using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace STRBlender.Core.Infrastructure.Data
{
    public class StutterCalculator
    {
        private readonly Dictionary<string, Dictionary<double, double>> _specific = new();
        private readonly Dictionary<string, (double Slope, double Intercept)> _linear = new();

        public static StutterCalculator Load(string csvPath)
        {
            if (!File.Exists(csvPath)) return new StutterCalculator();
            return ParseLines(File.ReadAllLines(csvPath));
        }

        /// Same parsing as Load, but from CSV text already in memory rather than a
        /// local file path — for callers with no local file system access (e.g. a
        /// Blazor WebAssembly app that fetched the CSV via HttpClient).
        public static StutterCalculator LoadFromContent(string csvContent)
        {
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return ParseLines(lines);
        }

        private static StutterCalculator ParseLines(string[] lines)
        {
            var rules = new StutterCalculator();
            if (lines.Length < 2) return rules;

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 3) continue;

                string locus = cols[0].Trim();
                string alleleStr = cols[1].Trim();
                string ratioStr = cols[2].Trim();

                if (string.IsNullOrWhiteSpace(locus)) continue;

                // Specific allele stutter
                if (!string.IsNullOrWhiteSpace(alleleStr) &&
                    double.TryParse(alleleStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double allele) &&
                    double.TryParse(ratioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ratio))
                {
                    if (!rules._specific.ContainsKey(locus))
                        rules._specific[locus] = new Dictionary<double, double>();
                    rules._specific[locus][allele] = ratio;
                }
                // Linear model fallback
                else if (string.IsNullOrWhiteSpace(alleleStr) && cols.Length > 4)
                {
                    string slopeStr = cols[3].Trim();
                    string interceptStr = cols[4].Trim();
                    if (double.TryParse(slopeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double slope) &&
                        double.TryParse(interceptStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double intercept))
                    {
                        rules._linear[locus] = (slope, intercept);
                    }
                }
            }
            return rules;
        }

        public double GetStutterRatio(string locus, string alleleStr)
        {
            if (!double.TryParse(alleleStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double allele))
                return 0.0;

            // Check specific allele first
            if (_specific.TryGetValue(locus, out var alleleDict))
            {
                double bestDist = double.MaxValue;
                double bestRatio = 0.0;
                foreach (var kvp in alleleDict)
                {
                    double dist = Math.Abs(kvp.Key - allele);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestRatio = kvp.Value;
                    }
                }
                if (bestDist < 0.01) return bestRatio;
            }

            // Fall back to linear model
            if (_linear.TryGetValue(locus, out var linear))
            {
                double ratio = linear.Slope * allele + linear.Intercept;
                return Math.Max(0.0, ratio);
            }

            return 0.0;
        }
    }
}