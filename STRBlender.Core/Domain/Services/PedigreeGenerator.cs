using System;
using System.Collections.Generic;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Services
{
    /// Builds a related family around an already-generated C1 profile.
    ///
    /// Two directions of generation are used:
    ///  - Backward (C1 -> parents -> grandparents): each non-sex-linked locus is
    ///    phased (which of C1's two alleles came from mother vs. father), the
    ///    transmitted allele is fixed on that parent, and the parent's other
    ///    allele is a fresh population-frequency draw. Valid under Hardy-Weinberg,
    ///    since a person's two alleles are independent, so conditioning on one
    ///    being known doesn't change the distribution of the other.
    ///  - Forward (parents -> any other relative): ordinary Mendelian transmission,
    ///    used once ancestors exist to generate siblings, children, aunts/uncles,
    ///    and cousins.
    ///
    /// All family members are drawn from the same frequency table as C1 (no
    /// mixed-population pedigrees), and C1's own generation is unaffected —
    /// this only ever builds outward from an already-sampled C1 profile.
    public static class PedigreeGenerator
    {
        private static readonly Random Rng = Random.Shared;

        private static readonly HashSet<string> SexLinkedLoci = new()
        {
            "AMEL", "YINDEL", "DYS391"
        };

        /// Builds C1's partner, parents, and all four grandparents from an
        /// already-generated C1 profile.
        public static Pedigree BuildCoreFamily(
            Profile c1,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse)
        {
            var pedigree = new Pedigree();
            pedigree.Set("C1", c1);

            string c1Sex = ResolveSex(c1);
            string partnerSex = c1Sex == "Male" ? "Female" : "Male";
            var partner = ProfileGenerator.SampleProfile(freqs, lociToUse,
                partnerSex == "Male" ? SexType.Male : SexType.Female);
            pedigree.Set("Partner", partner);

            var (mother, father) = GenerateParentsBackward(c1, freqs, lociToUse);
            pedigree.Set("Mother", mother);
            pedigree.Set("Father", father);

            var (mgm, mgf) = GenerateParentsBackward(mother, freqs, lociToUse);
            pedigree.Set("MaternalGrandmother", mgm);
            pedigree.Set("MaternalGrandfather", mgf);

            var (pgm, pgf) = GenerateParentsBackward(father, freqs, lociToUse);
            pedigree.Set("PaternalGrandmother", pgm);
            pedigree.Set("PaternalGrandfather", pgf);

            return pedigree;
        }

        /// Backward step: given a child's genotype, generate that child's mother
        /// and father. Mother and father's sex is fixed by role — sex-linked loci
        /// are set directly, not derived from the child.
        private static (Profile Mother, Profile Father) GenerateParentsBackward(
            Profile child,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse)
        {
            var mother = new Profile();
            var father = new Profile();

            foreach (var locus in lociToUse)
            {
                if (SexLinkedLoci.Contains(locus)) continue;
                if (!child.Loci.TryGetValue(locus, out var childAlleles)) continue;
                if (!freqs.ContainsKey(locus) || freqs[locus].Count == 0) continue;

                bool firstIsMaternal = Rng.NextDouble() < 0.5;
                string maternalAllele = firstIsMaternal ? childAlleles.A1 : childAlleles.A2;
                string paternalAllele = firstIsMaternal ? childAlleles.A2 : childAlleles.A1;

                string motherOther = ProfileGenerator.WeightedChoice(freqs[locus]);
                string fatherOther = ProfileGenerator.WeightedChoice(freqs[locus]);

                mother.Loci[locus] = ProfileGenerator.SortAlleles(maternalAllele, motherOther);
                father.Loci[locus] = ProfileGenerator.SortAlleles(paternalAllele, fatherOther);
            }

            ProfileGenerator.SetSexLinkedLoci(mother, "Female", lociToUse);
            ProfileGenerator.SetSexLinkedLoci(father, "Male", lociToUse);

            // The Y chromosome passes down the paternal line unchanged. If the child
            // is male, we know their Y-STR values exactly equal their father's — so
            // overwrite the fresh draw above with the child's actual values. If the
            // child is female, there's no information to propagate (she carries no
            // Y-STR), so the fresh draw above stands. This also correctly propagates
            // transitively: since Father is always male when this method is called
            // to build his own parents, PaternalGrandfather always inherits Father's
            // exact Y-STR values, matching C1's if C1 is male.
            if (ResolveSex(child) == "Male")
            {
                if (lociToUse.Contains("YINDEL") && child.Loci.TryGetValue("YINDEL", out var childYindel))
                    father.Loci["YINDEL"] = childYindel;

                if (lociToUse.Contains("DYS391") && child.Loci.TryGetValue("DYS391", out var childDys391))
                    father.Loci["DYS391"] = childDys391;
            }

            return (mother, father);
        }

        /// Forward step: generate a new child from a known mother and father.
        /// By default (requestedSex = Random), sex is derived from which of the
        /// father's AMEL alleles is transmitted (mother always contributes X) — a
        /// genuine random outcome. If a specific sex is requested instead, that
        /// outcome is forced directly rather than left to chance; this is still a
        /// valid conditioning on a known result, not a departure from the model.
        /// Y-linked loci (YINDEL, DYS391) are copied directly from the father for
        /// sons, since the Y chromosome passes down the paternal line without
        /// recombining — a fresh redraw would incorrectly break paternal-line
        /// Y-STR sharing.
        public static Profile GenerateChildForward(
            Profile mother,
            Profile father,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse,
            SexType requestedSex = SexType.Random)
        {
            var child = new Profile();

            foreach (var locus in lociToUse)
            {
                if (SexLinkedLoci.Contains(locus)) continue;
                if (!mother.Loci.TryGetValue(locus, out var momAlleles)) continue;
                if (!father.Loci.TryGetValue(locus, out var dadAlleles)) continue;

                string momAllele = Rng.NextDouble() < 0.5 ? momAlleles.A1 : momAlleles.A2;
                string dadAllele = Rng.NextDouble() < 0.5 ? dadAlleles.A1 : dadAlleles.A2;

                child.Loci[locus] = ProfileGenerator.SortAlleles(momAllele, dadAllele);
            }

            string childSex;
            if (requestedSex == SexType.Male)
            {
                childSex = "Male";
            }
            else if (requestedSex == SexType.Female)
            {
                childSex = "Female";
            }
            else
            {
                // Sex determined by father's transmitted AMEL allele (X or Y).
                childSex = "Female";
                if (father.Loci.TryGetValue("AMEL", out var fatherAmel))
                {
                    string transmitted = Rng.NextDouble() < 0.5 ? fatherAmel.A1 : fatherAmel.A2;
                    childSex = transmitted == "Y" ? "Male" : "Female";
                }
            }

            if (lociToUse.Contains("AMEL"))
            {
                string second = childSex == "Male" ? "Y" : "X";
                child.Loci["AMEL"] = ("X", second);
            }

            if (childSex == "Male")
            {
                if (lociToUse.Contains("YINDEL") && father.Loci.TryGetValue("YINDEL", out var yindel))
                    child.Loci["YINDEL"] = yindel;

                if (lociToUse.Contains("DYS391") && father.Loci.TryGetValue("DYS391", out var dys391))
                    child.Loci["DYS391"] = dys391;
            }

            return child;
        }

        private static string ResolveSex(Profile profile)
        {
            if (profile.Loci.TryGetValue("AMEL", out var amel) && amel.A2 == "Y")
                return "Male";
            return "Female";
        }

        private static (Profile Mother, Profile Father) AssignByGeneratedSex(Profile a, Profile b)
        {
            return ResolveSex(a) == "Female" ? (a, b) : (b, a);
        }

        /// A full sibling of C1 — a fresh, independent meiosis from the same
        /// cached parents. Each call produces a distinct individual, so multiple
        /// "Sibling" contributor slots correctly become different siblings.
        public static Profile GenerateSibling(
            Pedigree pedigree,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse,
            SexType requestedSex = SexType.Random)
        {
            return GenerateChildForward(pedigree.Get("Mother"), pedigree.Get("Father"), freqs, lociToUse, requestedSex);
        }

        /// A child of C1 and C1's cached partner.
        public static Profile GenerateChildOfC1(
            Pedigree pedigree,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse,
            SexType requestedSex = SexType.Random)
        {
            var (mother, father) = AssignByGeneratedSex(pedigree.Get("C1"), pedigree.Get("Partner"));
            return GenerateChildForward(mother, father, freqs, lociToUse, requestedSex);
        }

        /// A sibling of C1's mother (side = "Maternal") or father (side = "Paternal") —
        /// i.e. an aunt or uncle of C1. Each call produces a distinct individual.
        public static Profile GenerateAuntOrUncle(
            Pedigree pedigree,
            string side,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse,
            SexType requestedSex = SexType.Random)
        {
            string gmKey = side == "Maternal" ? "MaternalGrandmother" : "PaternalGrandmother";
            string gfKey = side == "Maternal" ? "MaternalGrandfather" : "PaternalGrandfather";
            return GenerateChildForward(pedigree.Get(gmKey), pedigree.Get(gfKey), freqs, lociToUse, requestedSex);
        }

        /// A child of a freshly-generated aunt/uncle (side = "Maternal" or "Paternal")
        /// and that aunt/uncle's own unrelated partner — i.e. a cousin of C1. The
        /// intermediate aunt/uncle and their partner are scaffolding only: each call
        /// builds its own, so this doesn't reuse an aunt/uncle from a separate
        /// contributor slot even if one was also requested in the same simulation.
        /// requestedSex applies to the cousin only — the intermediate aunt/uncle's
        /// sex is always left to chance.
        public static Profile GenerateCousin(
            Pedigree pedigree,
            string side,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse,
            SexType requestedSex = SexType.Random)
        {
            var auntOrUncle = GenerateAuntOrUncle(pedigree, side, freqs, lociToUse);
            var auntOrUnclePartnerSex = ResolveSex(auntOrUncle) == "Male" ? SexType.Female : SexType.Male;
            var partner = ProfileGenerator.SampleProfile(freqs, lociToUse, auntOrUnclePartnerSex);

            var (mother, father) = AssignByGeneratedSex(auntOrUncle, partner);
            return GenerateChildForward(mother, father, freqs, lociToUse, requestedSex);
        }

        /// Resolves a contributor's requested relationship to a profile. Singleton
        /// roles (Mother, Father, grandparents, Partner) are looked up directly from
        /// the cached pedigree, with sex fixed by role — every contributor requesting
        /// the same singleton role gets the same individual, and requestedSex has no
        /// effect. Multi-instance roles (Sibling, Child, Aunt/Uncle, Cousin) generate
        /// a fresh individual on every call and honor requestedSex if not Random.
        /// Returns null if the role isn't recognized, so the caller can fall back to
        /// its own default handling.
        public static Profile? ResolveRole(
            Pedigree pedigree,
            string role,
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse,
            SexType requestedSex = SexType.Random)
        {
            switch (role)
            {
                case "Sibling":
                    return GenerateSibling(pedigree, freqs, lociToUse, requestedSex);
                case "Child":
                    return GenerateChildOfC1(pedigree, freqs, lociToUse, requestedSex);
                case "MaternalAuntUncle":
                    return GenerateAuntOrUncle(pedigree, "Maternal", freqs, lociToUse, requestedSex);
                case "PaternalAuntUncle":
                    return GenerateAuntOrUncle(pedigree, "Paternal", freqs, lociToUse, requestedSex);
                case "MaternalCousin":
                    return GenerateCousin(pedigree, "Maternal", freqs, lociToUse, requestedSex);
                case "PaternalCousin":
                    return GenerateCousin(pedigree, "Paternal", freqs, lociToUse, requestedSex);
                default:
                    return pedigree.TryGet(role, out var profile) ? profile : null;
            }
        }
    }
}
