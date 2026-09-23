using System.Collections.Generic;
using System.Linq;

namespace STRBlender.Core.Domain.Common
{
    /// Per-locus MW regression parameters (moved here from the old
    /// LocusDefinitions.cs, unchanged) — k/b/a feed the same linear
    /// allele→MW conversion as before; efficiency and min/max MW are used
    /// by peak height modeling and locus bounds checks respectively.
    public class LocusParams
    {
        public double K { get; }
        public double B { get; }
        public double A { get; }
        public double Efficiency { get; }
        public double? MinMW { get; }
        public double? MaxMW { get; }

        public LocusParams(double k, double b, double a, double efficiency, double? minMW, double? maxMW)
        {
            K = k;
            B = b;
            A = a;
            Efficiency = efficiency;
            MinMW = minMW;
            MaxMW = maxMW;
        }
    }

    /// One allelic-ladder bin for a single locus/allele, in MW/bp data space
    /// (not yet converted to screen pixels — EpgChartBuilder does that, the
    /// same way it converts peak MW values to screen X).
    public record LadderBin(string Locus, string Allele, double MwMin, double MwMax);

    /// Everything that describes one STR kit: its loci, dye/channel layout,
    /// per-locus MW regression parameters, and allelic ladder alleles.
    /// Adding a new kit (PowerPlex, SGM Plus, etc.) means writing one new
    /// factory method that builds one of these — nothing else in the app
    /// needs to change.
    public class KitDefinition
    {
        public string Name { get; }
        public List<string> Loci { get; }
        public Dictionary<string, ChannelConfig> Channels { get; }
        public Dictionary<string, LocusParams> LocusParamsByLocus { get; }

        /// Ladder alleles per locus, e.g. ["8","9","10",...]. Order doesn't
        /// matter — BuildLadderBins sorts by resolved MW.
        public Dictionary<string, List<string>> Ladder { get; }

        public KitDefinition(
            string name,
            List<string> loci,
            Dictionary<string, ChannelConfig> channels,
            Dictionary<string, LocusParams> locusParams,
            Dictionary<string, List<string>> ladder)
        {
            Name = name;
            Loci = loci;
            Channels = channels;
            LocusParamsByLocus = locusParams;
            Ladder = ladder;
        }

        /// Same allele→MW logic previously in LocusDefinitions.CalculateMw,
        /// now scoped to this specific kit's own parameters rather than
        /// whichever kit happened to be the last one set as "active".
        public double? CalculateMw(string locus, string allele)
        {
            if (locus == "AMEL")
                return allele == "X" ? 106.0 : allele == "Y" ? 110.0 : null;
            if (locus == "YINDEL")
                return allele == "1" ? 81.075 : allele == "2" ? 86.345 : null;

            if (!LocusParamsByLocus.TryGetValue(locus, out var p))
                return null;
            if (!double.TryParse(allele, out double alleleNum))
                return null;

            return AlleleToMw(alleleNum, p.K, p.B, p.A);
        }

        public double GetEfficiency(string locus) =>
            LocusParamsByLocus.TryGetValue(locus, out var p) ? p.Efficiency : 1.0;

        public (double? MinMw, double? MaxMw) GetLocusBounds(string locus) =>
            LocusParamsByLocus.TryGetValue(locus, out var p) ? (p.MinMW, p.MaxMW) : (null, null);

        private static double AlleleToMw(double allele, double k, double b, double a)
        {
            int integerPart = (int)allele;
            double fractionalPart = allele - integerPart;
            return (integerPart - k) * a + (fractionalPart * 10) + b;
        }

        /// Fixed bin half-width, in MW/bp data-space units (the same units
        /// as LocusParams.A — roughly "one repeat unit" per allele step).
        /// Adjust this single value to make every kit's bins narrower or
        /// wider; it applies uniformly to every locus/allele.
        public const double LadderBinHalfWidth = 0.5;

        /// Computes every locus's ladder bins in MW/bp data space, one
        /// fixed-width bin per ladder allele, centered on that allele's
        /// resolved MW position. Width is controlled by LadderBinHalfWidth
        /// above.
        public List<LadderBin> BuildLadderBins()
        {
            var bins = new List<LadderBin>();

            foreach (var locus in Loci)
            {
                if (!Ladder.TryGetValue(locus, out var alleles) || alleles.Count == 0)
                    continue;

                foreach (var allele in alleles)
                {
                    double? mw = CalculateMw(locus, allele);
                    if (!mw.HasValue) continue;

                    bins.Add(new LadderBin(locus, allele, mw.Value - LadderBinHalfWidth, mw.Value + LadderBinHalfWidth));
                }
            }

            return bins;
        }
    }
}
