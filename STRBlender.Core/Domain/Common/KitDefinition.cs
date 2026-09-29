using System.Collections.Generic;
using System.Linq;

namespace STRBlender.Core.Domain.Common
{
    /// Per-locus MW regression parameters (moved here from the old
    /// LocusDefinitions.cs, unchanged) — k/b/a feed the same linear
    /// allele→MW conversion as before; efficiency is used by peak height
    /// modeling. No stored min/max MW here — the locus bar's range is
    /// derived live from the ladder (see KitDefinition.GetLocusBounds), so
    /// there's no separate min/max value that could ever drift out of sync
    /// with the ladder it's supposed to describe.
    public class LocusParams
    {
        public double Intercept { get; }  // Was B
        public double Slope { get; }      // Was A
        public double Efficiency { get; }

        public LocusParams(double intercept, double slope, double efficiency)
        {
            Intercept = intercept;
            Slope = slope;
            Efficiency = efficiency;
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

            return AlleleToMw(alleleNum, p.Intercept, p.Slope);
        }

        public double GetEfficiency(string locus) =>
            LocusParamsByLocus.TryGetValue(locus, out var p) ? p.Efficiency : 1.0;

        /// The locus bar's range: the lowest and highest MW across that
        /// locus's own ladder alleles, computed live from the ladder (not a
        /// separately stored value) — so the bar always exactly matches
        /// where the ladder's own peaks/bins actually sit, tight to the
        /// alleles you've entered rather than an official spec's Min/Max
        /// Size, which intentionally pads beyond the printed ladder to
        /// accommodate off-ladder calls and isn't recoverable from k/b/a.
        public (double? MinMw, double? MaxMw) GetLocusBounds(string locus)
        {
            if (!Ladder.TryGetValue(locus, out var alleles) || alleles.Count == 0)
                return (null, null);

            var mws = alleles.Select(a => CalculateMw(locus, a)).Where(mw => mw.HasValue).Select(mw => mw!.Value).ToList();
            if (mws.Count == 0)
                return (null, null);

            return (mws.Min(), mws.Max());
        }

        private static double AlleleToMw(double allele, double intercept, double slope)
        {
            int integerPart = (int)allele;
            double fractionalPart = allele - integerPart;
            return integerPart * slope + (fractionalPart * 10) + intercept;
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
