using System.Collections.Generic;
using System.Linq;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Services
{
    public static class PeakMerger
    {
        public static List<Peak> MergePeaks(List<Peak> allPeaks, List<string> lociToUse)
        {
            var merged = new Dictionary<(string Locus, string Allele), double>();

            foreach (var peak in allPeaks)
            {
                var key = (peak.Locus, peak.Allele);
                if (merged.ContainsKey(key))
                    merged[key] += peak.Height;
                else
                    merged[key] = peak.Height;
            }

            var locusOrder = lociToUse
                .Select((l, i) => (l, i))
                .ToDictionary(x => x.l, x => x.i);

            return merged.Select(kvp =>
            {
                var (locus, allele) = kvp.Key;
                double? mw = LocusDefinitions.CalculateMw(locus, allele);

                return new Peak
                {
                    Locus = locus,
                    Allele = allele,
                    Mw = mw.HasValue ? Math.Round(mw.Value, 2) : null,
                    Height = Math.Round(kvp.Value, 2)
                };
            })
            .OrderBy(p => locusOrder.TryGetValue(p.Locus, out int o) ? o : 999)
            .ThenBy(p => double.TryParse(p.Allele, out double a) ? a : double.MaxValue)
            .ToList();
        }
    }
}