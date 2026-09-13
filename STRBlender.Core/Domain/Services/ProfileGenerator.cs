using System;
using System.Collections.Generic;
using System.Linq;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Services
{
    public static class ProfileGenerator
    {
        private static readonly Random Rng = Random.Shared;

        public static string WeightedChoice(Dictionary<string, double> freqs)
        {
            double total = freqs.Values.Sum();
            double r = Rng.NextDouble() * total;
            double cumulative = 0;

            foreach (var kvp in freqs)
            {
                cumulative += kvp.Value;
                if (r <= cumulative)
                    return kvp.Key;
            }
            return freqs.Keys.Last();
        }

        public static readonly Dictionary<string, double> DysStutterFreqs = new()
        {
            ["8"] = 0.02,
            ["9"] = 0.08,
            ["10"] = 0.40,
            ["11"] = 0.30,
            ["12"] = 0.15,
            ["13"] = 0.05
        };

        /// Sets AMEL and any Y-linked loci (YINDEL, DYS391) on a profile for a fixed,
        /// already-known sex. Used both for ordinary founder generation and for pedigree
        /// roles whose sex is fixed by definition (e.g. Mother, Father, grandparents).
        public static void SetSexLinkedLoci(Profile profile, string resolvedSex, List<string> lociToUse)
        {
            if (lociToUse.Contains("AMEL"))
            {
                string second = resolvedSex == "Male" ? "Y" : "X";
                profile.Loci["AMEL"] = ("X", second);
            }

            if (resolvedSex != "Male") return;

            if (lociToUse.Contains("YINDEL"))
            {
                string allele = Rng.NextDouble() < 0.5 ? "1" : "2";
                profile.Loci["YINDEL"] = (allele, allele);
            }

            if (lociToUse.Contains("DYS391"))
            {
                string a = WeightedChoice(DysStutterFreqs);
                profile.Loci["DYS391"] = (a, a);
            }
        }

        public static Profile SampleProfile(
            Dictionary<string, Dictionary<string, double>> freqs,
            List<string> lociToUse,
            SexType sex = SexType.Random)
        {
            var profile = new Profile();

            // Determine sex
            string resolvedSex = sex switch
            {
                SexType.Male => "Male",
                SexType.Female => "Female",
                _ => Rng.NextDouble() < 0.5 ? "Male" : "Female"
            };

            SetSexLinkedLoci(profile, resolvedSex, lociToUse);

            // Other loci
            foreach (var locus in lociToUse)
            {
                if (locus == "AMEL" || locus == "YINDEL" || locus == "DYS391") continue;

                if (!freqs.ContainsKey(locus) || freqs[locus].Count == 0)
                    continue;

                string a1 = WeightedChoice(freqs[locus]);
                string a2 = WeightedChoice(freqs[locus]);
                profile.Loci[locus] = SortAlleles(a1, a2);
            }

            return profile;
        }

        public static (string, string) SortAlleles(string a1, string a2)
        {
            bool a1IsNum = double.TryParse(a1, out double n1);
            bool a2IsNum = double.TryParse(a2, out double n2);

            if (a1IsNum && a2IsNum)
                return n1 <= n2 ? (a1, a2) : (a2, a1);

            return string.Compare(a1, a2, StringComparison.Ordinal) <= 0 ? (a1, a2) : (a2, a1);
        }
    }
}