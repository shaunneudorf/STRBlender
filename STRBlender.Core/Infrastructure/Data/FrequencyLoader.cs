using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using STRBlender.Core.Domain.Common;

namespace STRBlender.Core.Infrastructure.Data
{
    public static class FrequencyLoader
    {
        public static Dictionary<string, Dictionary<string, double>> LoadAlleleFrequencies(
            string csvPath, List<string> lociToUse)
        {
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Frequency file not found: {csvPath}");

            return ParseLines(File.ReadAllLines(csvPath), lociToUse);
        }

        /// Same parsing as LoadAlleleFrequencies, but from CSV text already in memory
        /// rather than a local file path — for callers with no local file system
        /// access (e.g. a Blazor WebAssembly app that fetched the CSV via HttpClient).
        public static Dictionary<string, Dictionary<string, double>> LoadAlleleFrequenciesFromContent(
            string csvContent, List<string> lociToUse)
        {
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return ParseLines(lines, lociToUse);
        }

        private static Dictionary<string, Dictionary<string, double>> ParseLines(
            string[] lines, List<string> lociToUse)
        {
            var freqs = new Dictionary<string, Dictionary<string, double>>();
            if (lines.Length < 2) return freqs;

            var headers = lines[0].Split(',');
            for (int i = 0; i < headers.Length; i++)
                headers[i] = headers[i].Trim().Trim('\uFEFF');

            // Case-insensitive (e.g. CSV "Yindel" vs kit "YINDEL")
            var locusColIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var locus in lociToUse)
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    if (string.Equals(headers[i], locus, StringComparison.OrdinalIgnoreCase))
                    {
                        locusColIndex[locus] = i;
                        break;
                    }
                }
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 2) continue;

                string allele = cols[0].Trim();
                if (allele.ToUpper() == "N" || string.IsNullOrWhiteSpace(allele)) continue;

                foreach (var locus in lociToUse)
                {
                    if (!locusColIndex.TryGetValue(locus, out int colIdx)) continue;
                    if (colIdx >= cols.Length) continue;

                    string valStr = cols[colIdx].Trim();
                    if (string.IsNullOrWhiteSpace(valStr)) continue;

                    if (double.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double p) && p > 0)
                    {
                        if (!freqs.ContainsKey(locus))
                            freqs[locus] = new Dictionary<string, double>();
                        freqs[locus][allele] = p;
                    }
                }
            }
            return freqs;
        }
    }
}