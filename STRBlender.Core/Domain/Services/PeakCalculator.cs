using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STRBlender.Core.Domain.Services
{
    public static class PeakCalculator
    {
        private static readonly Random Rng = Random.Shared;
        private const double Offset = 80.0;

        // Shared kit-level constant (b) used by both the allelic and stutter
        // variance denominators: f(x) = b/x + x. Kept as one global value
        // rather than per-locus, per current scope.
        private const double B = 1000.0;

        // Numerator constants: "parent" is c^2 for true alleles (used with
        // f(Ea)); reverse/forward are k^2 for stutter (used with f(Oa)).
        private static readonly Dictionary<string, double> StochasticConstants = new()
        {
            ["parent"] = 5.71,
            ["reverse_stutter"] = 5.79,
            ["forward_stutter"] = 11.50,
        };

        // f(x) = b/x + x, shared by both the allelic model (x = Ea, the
        // peak's own expected/stacked height) and the stutter model
        // (x = Oa, the parent allele's already-finalized observed height).
        private static double VarianceDenominator(double x) => (B / x) + x;

        /// Applies the allelic stochastic variation model: log10(noisy/expected)
        /// ~ N(0, c^2 / f(Ea)). Used for true-allele (Stage 1) peaks only.
        public static double ApplyStochasticVariation(double expectedHeight, string peakType)
        {
            if (!StochasticConstants.TryGetValue(peakType, out double c))
                c = StochasticConstants["parent"];

            double sigma = Math.Sqrt(c / VarianceDenominator(expectedHeight));
            double noise = SampleNormal(0, sigma);
            return expectedHeight * Math.Pow(10, noise);
        }

        private static double SampleNormal(double mean, double stdDev)
        {
            double u1 = 1.0 - Rng.NextDouble();
            double u2 = 1.0 - Rng.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return mean + stdDev * z;
        }

        private static readonly HashSet<string> HemizygousLoci = new() { "YINDEL", "DYS391" };

        private static List<(string Allele, int X)> CalculateDose(string locus, string a1, string a2)
        {
            if (HemizygousLoci.Contains(locus))
                return new List<(string, int)> { (a1, 1) };

            if (a1 == a2)
                return new List<(string, int)> { (a1, 2) };

            return new List<(string, int)> { (a1, 1), (a2, 1) };
        }

        private static double? CalculatePeakHeight(string locus, string allele, int x,
            double template, double proportion, double degradation)
        {
            double a = LocusDefinitions.GetEfficiency(locus);
            double? mw = LocusDefinitions.CalculateMw(locus, allele);
            if (mw == null) return null;

            double exponent = degradation * (mw.Value - Offset);
            return a * template * proportion * x * Math.Exp(exponent);
        }

        /// Builds one contributor's raw peak contributions. True-allele entries
        /// (PeakType "parent") carry their full, undivided expected height — no
        /// conservation split with stutter, since stutter is no longer carved out
        /// of the parent's own height. Stutter entries here are illustrative only
        /// (a per-contributor approximation for overlay/display purposes): the
        /// real stutter calculation happens later, in GenerateStutterContributions,
        /// derived from each finalized allele position's realized Oa — which
        /// doesn't exist yet at this per-contributor stage. Callers building the
        /// real calculation must filter this list to PeakType == "parent"; the
        /// full list (parent + illustrative stutter) is what should populate
        /// per-contributor overlay data.
        public static List<Peak> BuildPeakTable(
            Profile profile,
            double template,
            double proportion,
            double degradation,
            StutterCalculator? reverseRules,
            StutterCalculator? forwardRules,
            List<string> lociToUse)
        {
            var peakList = new List<Peak>();

            foreach (var locus in profile.Loci.Keys)
            {
                var (a1, a2) = profile.Loci[locus];
                var doses = CalculateDose(locus, a1, a2);

                foreach (var (alleleStr, x) in doses)
                {
                    double? t = CalculatePeakHeight(locus, alleleStr, x, template, proportion, degradation);
                    double? mw = LocusDefinitions.CalculateMw(locus, alleleStr);
                    if (mw == null || t == null) continue;

                    peakList.Add(new Peak
                    {
                        Locus = locus,
                        Allele = alleleStr,
                        Mw = mw,
                        BaseHeight = t.Value,
                        PeakType = "parent"
                    });

                    if (double.TryParse(alleleStr, out double alleleNum))
                    {
                        double srRev = reverseRules?.GetStutterRatio(locus, alleleStr) ?? 0.0;
                        double srFwd = forwardRules?.GetStutterRatio(locus, alleleStr) ?? 0.0;

                        if (srRev > 0.001)
                        {
                            string revAllele = FormatAllele(alleleNum - 1);
                            peakList.Add(new Peak
                            {
                                Locus = locus,
                                Allele = revAllele,
                                Mw = LocusDefinitions.CalculateMw(locus, revAllele),
                                BaseHeight = t.Value * srRev,   // illustrative only
                                PeakType = "reverse_stutter"
                            });
                        }

                        if (srFwd > 0.001)
                        {
                            string fwdAllele = FormatAllele(alleleNum + 1);
                            peakList.Add(new Peak
                            {
                                Locus = locus,
                                Allele = fwdAllele,
                                Mw = LocusDefinitions.CalculateMw(locus, fwdAllele),
                                BaseHeight = t.Value * srFwd,   // illustrative only
                                PeakType = "forward_stutter"
                            });
                        }
                    }
                }
            }
            return peakList;
        }

        /// Stage 1: stacks true-allele-only contributions by (locus, allele) and
        /// applies the allelic stochastic variation model to each stacked total,
        /// producing every position's finalized, realized height (Ea).
        private static List<Peak> StackAlleles(List<Peak> alleleOnlyPeaks)
        {
            var baseHeightDict = new Dictionary<(string Locus, string Allele), double>();

            foreach (var peak in alleleOnlyPeaks)
            {
                var key = (peak.Locus, peak.Allele);
                baseHeightDict[key] = baseHeightDict.GetValueOrDefault(key) + peak.BaseHeight;
            }

            var finalized = new List<Peak>();
            foreach (var kvp in baseHeightDict)
            {
                var (locus, allele) = kvp.Key;
                double totalBaseHeight = kvp.Value;
                double finalHeight = ApplyStochasticVariation(totalBaseHeight, "parent");
                double? mw = LocusDefinitions.CalculateMw(locus, allele);

                finalized.Add(new Peak
                {
                    Locus = locus,
                    Allele = allele,
                    Mw = mw,
                    Height = finalHeight,
                    BaseHeight = totalBaseHeight,
                    PeakType = "parent"
                });
            }

            return finalized;
        }

        /// Stage 2: for every finalized allele position (Ea), computes one shared
        /// stutter draw per direction using that position's own realized Oa —
        /// log10(stutterHeight / Oa) ~ N(0, k^2 / f(Oa)). One draw per finalized
        /// position, not per contributor, since multiple contributors sharing a
        /// true-allele position have already been merged into a single Oa by the
        /// time stutter is calculated — matching how the position is physically
        /// a single co-migrating peak on the instrument.
        private static List<Peak> GenerateStutterContributions(
            List<Peak> finalizedAllelePeaks,
            StutterCalculator? reverseRules,
            StutterCalculator? forwardRules,
            List<string> lociToUse)
        {
            var stutterPeaks = new List<Peak>();

            foreach (var allelePeak in finalizedAllelePeaks)
            {
                if (!double.TryParse(allelePeak.Allele, out double alleleNum)) continue;

                double oa = allelePeak.Height;
                if (oa <= 0) continue;

                double srRev = reverseRules?.GetStutterRatio(allelePeak.Locus, allelePeak.Allele) ?? 0.0;
                double srFwd = forwardRules?.GetStutterRatio(allelePeak.Locus, allelePeak.Allele) ?? 0.0;

                if (srRev > 0.001)
                {
                    stutterPeaks.Add(BuildStutterPeak(
                        allelePeak.Locus, FormatAllele(alleleNum - 1),
                        oa, srRev, "reverse_stutter"));
                }

                if (srFwd > 0.001)
                {
                    stutterPeaks.Add(BuildStutterPeak(
                        allelePeak.Locus, FormatAllele(alleleNum + 1),
                        oa, srFwd, "forward_stutter"));
                }
            }

            return stutterPeaks;
        }

        private static Peak BuildStutterPeak(string locus, string allele, double oa, double ratio, string peakType)
        {
            double k = StochasticConstants[peakType];
            double sigma = Math.Sqrt(k / VarianceDenominator(oa));
            double noise = SampleNormal(0, sigma);

            double expectedStutterHeight = oa * ratio;
            double finalHeight = expectedStutterHeight * Math.Pow(10, noise);

            return new Peak
            {
                Locus = locus,
                Allele = allele,
                Mw = LocusDefinitions.CalculateMw(locus, allele),
                Height = finalHeight,
                BaseHeight = expectedStutterHeight,
                PeakType = peakType
            };
        }

        /// Orchestrates the full final-peak calculation: Stage 1 (stack + vary
        /// true alleles), Stage 2 (derive stutter from each finalized Oa), then
        /// an additive merge — stutter landing on an existing allele position, or
        /// multiple stutter contributions sharing a position, are simply summed,
        /// matching how co-migrating signal physically combines at the detector.
        public static List<Peak> BuildFinalPeaks(
            List<Peak> alleleOnlyPeaks,
            StutterCalculator? reverseRules,
            StutterCalculator? forwardRules,
            List<string> lociToUse)
        {
            var finalizedAlleles = StackAlleles(alleleOnlyPeaks);
            var stutterContributions = GenerateStutterContributions(
                finalizedAlleles, reverseRules, forwardRules, lociToUse);

            var heightDict = new Dictionary<(string Locus, string Allele), double>();
            var baseDict = new Dictionary<(string Locus, string Allele), double>();
            var mwDict = new Dictionary<(string Locus, string Allele), double?>();

            foreach (var peak in finalizedAlleles.Concat(stutterContributions))
            {
                // Normalise allele string so parent and stutter keys always match
                string normalisedAllele = NormalizeAlleleKey(peak.Allele);
                var key = (peak.Locus, normalisedAllele);

                heightDict[key] = heightDict.GetValueOrDefault(key) + peak.Height;
                baseDict[key] = baseDict.GetValueOrDefault(key) + peak.BaseHeight;
                mwDict[key] = peak.Mw;
            }

            var locusOrder = lociToUse
                .Select((l, i) => (l, i))
                .ToDictionary(x => x.l, x => x.i);

            var merged = heightDict.Select(kvp => new Peak
            {
                Locus = kvp.Key.Locus,
                Allele = FormatAlleleForDisplay(kvp.Key.Allele),
                Mw = mwDict[kvp.Key].HasValue ? Math.Round(mwDict[kvp.Key]!.Value, 2) : null,
                Height = Math.Round(kvp.Value, 2),
                BaseHeight = Math.Round(baseDict[kvp.Key], 2)
            }).ToList();

            merged.Sort((a, b) =>
            {
                int locA = locusOrder.TryGetValue(a.Locus, out int la) ? la : 999;
                int locB = locusOrder.TryGetValue(b.Locus, out int lb) ? lb : 999;
                if (locA != locB) return locA.CompareTo(locB);

                bool aNum = double.TryParse(a.Allele, out double an);
                bool bNum = double.TryParse(b.Allele, out double bn);
                if (aNum && bNum) return an.CompareTo(bn);

                return string.Compare(a.Allele, b.Allele, StringComparison.Ordinal);
            });

            return merged;
        }

        private static string FormatAllele(double allele)
        {
            // Round to 1 decimal place first to kill floating-point noise
            // (STR alleles are never finer than 0.1)
            double rounded = Math.Round(allele, 1);

            return rounded == Math.Floor(rounded)
                ? ((int)rounded).ToString()
                : rounded.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string FormatAlleleForDisplay(string allele)
        {
            if (double.TryParse(allele, out double value))
            {
                // If it's a whole number, show without decimal
                if (value == Math.Floor(value))
                    return ((int)value).ToString();

                // Otherwise show with exactly one decimal place, no trailing zero
                return value.ToString("0.#");
            }

            return allele;
        }
        /// Ensures parent alleles and stutter alleles always share the same dictionary key.
        /// Parses the allele to double then re-formats with the canonical FormatAllele rules.
        private static string NormalizeAlleleKey(string allele)
        {
            if (double.TryParse(allele, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                return FormatAllele(value);
            }
            return allele; // non-numeric (AMEL X/Y, etc.)
        }
    }
}