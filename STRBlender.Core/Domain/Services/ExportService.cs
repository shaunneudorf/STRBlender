using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Services
{
    public static class ExportService
    {
        // ====================== SAMPLE EXPORT (STRmix format) ======================
        public static void ExportSample(string filePath, List<Peak> peaks,
    string sampleName, List<string> lociToUse, bool append = false,
    Dictionary<string, int>? locusThresholds = null)
        {
            // Check for duplicate sample name when appending
            if (append && File.Exists(filePath))
            {
                var existingLines = File.ReadAllLines(filePath);
                bool duplicate = existingLines.Any(line =>
                    line.StartsWith(sampleName + "\t"));
                if (duplicate)
                    throw new InvalidOperationException(
                        $"Sample name '{sampleName}' already exists in this file. " +
                        $"Please use a different Exhibit or Item number.");
            }

            var lines = new List<string>();
            bool fileExists = File.Exists(filePath);

            // Write header only if new file
            if (!append || !fileExists)
            {
                var header = new List<string> { "Sample Name", "Marker" };
                for (int i = 1; i <= 20; i++) header.Add($"Allele {i}");
                for (int i = 1; i <= 20; i++) header.Add($"Size {i}");
                for (int i = 1; i <= 20; i++) header.Add($"Height {i}");
                lines.Add(string.Join("\t", header));
            }

            // ---- Filter by analytical threshold (same logic as the genotype table) ----
            IEnumerable<Peak> filtered = peaks;
            if (locusThresholds != null)
            {
                filtered = peaks.Where(p =>
                {
                    int threshold = locusThresholds.TryGetValue(p.Locus, out int t) ? t : 100;
                    return p.Height >= threshold;
                });
            }

            // ---- Safety merge by (Locus, Allele) in case any duplicates remain ----
            var mergedByLocus = filtered
                .GroupBy(p => p.Locus)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(p => p.Allele)
                          .Select(ag => new Peak
                          {
                              Locus = g.Key,
                              Allele = ag.Key,
                              Mw = ag.First().Mw,
                              Height = ag.Sum(x => x.Height)
                          })
                          .OrderBy(p => double.TryParse(p.Allele, out double a) ? a : double.MaxValue)
                          .ToList());

            // Write in kit locus order
            foreach (var locus in lociToUse)
            {
                if (!mergedByLocus.ContainsKey(locus)) continue;
                var locusPeaks = mergedByLocus[locus];
                var row = new List<string> { sampleName, locus };

                // Alleles
                for (int i = 0; i < 20; i++)
                    row.Add(i < locusPeaks.Count ? locusPeaks[i].Allele : "");

                // Sizes (MW)
                for (int i = 0; i < 20; i++)
                    row.Add(i < locusPeaks.Count && locusPeaks[i].Mw.HasValue
                        ? locusPeaks[i].Mw.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        : "");

                // Heights (rounded to integer, matching the genotype table)
                for (int i = 0; i < 20; i++)
                    row.Add(i < locusPeaks.Count
                        ? Math.Round(locusPeaks[i].Height).ToString()
                        : "");

                lines.Add(string.Join("\t", row));
            }

            if (append && fileExists)
                File.AppendAllLines(filePath, lines);
            else
                File.WriteAllLines(filePath, lines);
        }

        // ====================== REFERENCE EXPORT (ground truth genotypes) ======================
        public static void ExportReference(string filePath, List<Profile> profiles,
            List<string> lociToUse, string sampleName)
        {
            // TODO: implement final format when spec is available
            var lines = new List<string>();
            lines.Add($"Sample: {sampleName}");

            for (int i = 0; i < profiles.Count; i++)
            {
                lines.Add($"Contributor {i + 1}");
                lines.Add("Locus\tAllele1\tAllele2");
                foreach (var locus in lociToUse)
                {
                    if (!profiles[i].Loci.ContainsKey(locus)) continue;
                    var (a1, a2) = profiles[i].Loci[locus];
                    lines.Add($"{locus}\t{a1}\t{a2}");
                }
            }
            File.WriteAllLines(filePath, lines);
        }

        // ====================== ML CSV EXPORT ======================
        public static void ExportMlCsv(string filePath, List<Peak> peaks,
            List<Profile> profiles, SimulationParams simParams, string sampleName)
        {
            // TODO: implement ML CSV format when column structure is defined
            File.WriteAllText(filePath, $"Sample: {sampleName} — ML export not yet implemented");
        }

        // ====================== SAMPLE NAME HELPERS ======================
        public static string? BuildSampleName(string fileNumber, string exhibitNumber, string itemNumber)
        {
            if (!ValidateFileNumber(fileNumber)) return null;
            if (!ValidateExhibitNumber(exhibitNumber)) return null;
            if (string.IsNullOrWhiteSpace(itemNumber)) return null;

            return $"{fileNumber}+{exhibitNumber}_{itemNumber}";
        }

        public static bool ValidateFileNumber(string s)
        {
            return Regex.IsMatch(s, @"^\d{4}[A-MO-Z]-\d{6}$");
        }

        public static bool ValidateExhibitNumber(string s)
        {
            return Regex.IsMatch(s, @"^\d{4}-[A-Z]{2}-\d{2}$");
        }

        public static string GetSamplePreview(string fileNumber, string exhibitNumber, string itemNumber)
        {
            string? name = BuildSampleName(fileNumber, exhibitNumber, itemNumber);
            return name != null ? $"Sample: {name}" : "Sample: invalid format";
        }
    }
}