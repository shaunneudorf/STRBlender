using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Domain.Services;

namespace STRBlender.Core.Application
{
    public enum BatchOutputMode
    {
        /// Single STRmix-style sample .txt (many rows per sample).
        StrmixTxt,

        /// Paired ML CSVs: mixtures (features) + truth (genotypes / labels), one row per sample.
        MlCsv
    }

    /// Batch of independent mixtures that share the same kit, template, and
    /// per-contributor settings (same shape as a single EPG simulation).
    public class BatchSimulationSpec
    {
        public string Kit { get; set; } = "GLOBALFILER";
        public double Template { get; set; } = 2000;
        public int ProfileCount { get; set; } = 100;
        public List<ContributorParams> ContributorParams { get; set; } = new();
        public int StartingSeries { get; set; } = 1;
        public BatchOutputMode OutputMode { get; set; } = BatchOutputMode.StrmixTxt;
    }

    public class BatchSimulationResult
    {
        public int Requested { get; set; }
        public int Written { get; set; }
        public string OutputPath { get; set; } = "";
        /// For ML mode, path to the companion truth CSV (if written).
        public string? TruthPath { get; set; }
        public List<string> SampleNames { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public static class BatchSimulationService
    {
        private const int LettersPerSeries = 26 * 26;
        private const int MaxPeaksPerLocus = 20;

        public static BatchSimulationResult Run(
            BatchSimulationSpec spec,
            string outputPath,
            IProgress<(int done, int total)>? progress = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var result = new BatchSimulationResult
            {
                Requested = spec.ProfileCount,
                OutputPath = outputPath
            };

            if (spec.ProfileCount < 1)
            {
                result.ErrorMessage = "Profile count must be at least 1.";
                return result;
            }

            if (spec.ContributorParams == null || spec.ContributorParams.Count < 1)
            {
                result.ErrorMessage = "At least one contributor is required.";
                return result;
            }

            if (!KitConfig.Kits.ContainsKey(spec.Kit))
            {
                result.ErrorMessage = $"Unknown kit '{spec.Kit}'.";
                return result;
            }

            var lociToUse = KitConfig.Kits[spec.Kit];
            LocusDefinitions.SetKit(spec.Kit);

            var channels = spec.Kit == "GLOBALFILER"
                ? KitConfig.GlobalfilerChannels
                : KitConfig.IdplusChannels;

            var locusThresholds = channels.Values
                .SelectMany(c => c.Loci.Select(locus => new { locus, c.Threshold }))
                .ToDictionary(x => x.locus, x => x.Threshold);

            var contributors = spec.ContributorParams
                .Select(c => c with { Locked = false })
                .ToList();

            int noc = contributors.Count;
            var sampleNames = new List<string>(spec.ProfileCount);

            if (spec.OutputMode == BatchOutputMode.StrmixTxt)
            {
                return RunStrmixTxt(spec, outputPath, contributors, lociToUse, locusThresholds,
                    sampleNames, progress, cancellationToken);
            }

            return RunMlCsv(spec, outputPath, contributors, lociToUse, locusThresholds,
                sampleNames, progress, cancellationToken);
        }

        private static BatchSimulationResult RunStrmixTxt(
            BatchSimulationSpec spec,
            string outputPath,
            List<ContributorParams> contributors,
            List<string> lociToUse,
            Dictionary<string, int> locusThresholds,
            List<string> sampleNames,
            IProgress<(int done, int total)>? progress,
            System.Threading.CancellationToken cancellationToken)
        {
            var result = new BatchSimulationResult
            {
                Requested = spec.ProfileCount,
                OutputPath = outputPath
            };

            var allLines = new List<string>();
            var header = new List<string> { "Sample Name", "Marker" };
            for (int i = 1; i <= MaxPeaksPerLocus; i++) header.Add($"Allele {i}");
            for (int i = 1; i <= MaxPeaksPerLocus; i++) header.Add($"Size {i}");
            for (int i = 1; i <= MaxPeaksPerLocus; i++) header.Add($"Height {i}");
            allLines.Add(string.Join("\t", header));

            for (int i = 0; i < spec.ProfileCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var simResult = SimulateOnce(spec, contributors, lociToUse);
                string sampleName = BuildSampleName(spec.StartingSeries, i);
                sampleNames.Add(sampleName);
                AppendStrmixSampleRows(allLines, sampleName, simResult.FinalPeaks, lociToUse, locusThresholds);
                progress?.Report((i + 1, spec.ProfileCount));
            }

            EnsureDirectory(outputPath);
            File.WriteAllLines(outputPath, allLines);

            result.Written = sampleNames.Count;
            result.SampleNames = sampleNames;
            return result;
        }

        private static BatchSimulationResult RunMlCsv(
            BatchSimulationSpec spec,
            string outputPath,
            List<ContributorParams> contributors,
            List<string> lociToUse,
            Dictionary<string, int> locusThresholds,
            List<string> sampleNames,
            IProgress<(int done, int total)>? progress,
            System.Threading.CancellationToken cancellationToken)
        {
            // outputPath is the mixtures CSV; truth is written alongside.
            string mixturesPath = outputPath;
            if (!mixturesPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                mixturesPath += ".csv";

            string directory = Path.GetDirectoryName(mixturesPath) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(mixturesPath);
            // If user picked "foo.csv", write foo_mixtures.csv + foo_truth.csv unless name already ends with _mixtures
            string mixturesFile;
            string truthFile;
            if (baseName.EndsWith("_mixtures", StringComparison.OrdinalIgnoreCase))
            {
                mixturesFile = mixturesPath;
                truthFile = Path.Combine(directory, baseName[..^"_mixtures".Length] + "_truth.csv");
            }
            else
            {
                mixturesFile = Path.Combine(directory, baseName + "_mixtures.csv");
                truthFile = Path.Combine(directory, baseName + "_truth.csv");
            }

            var result = new BatchSimulationResult
            {
                Requested = spec.ProfileCount,
                OutputPath = mixturesFile,
                TruthPath = truthFile
            };

            int noc = contributors.Count;
            var mixtureHeader = BuildMixtureHeader(noc, lociToUse);
            var truthHeader = BuildTruthHeader(noc, lociToUse);

            var mixtureLines = new List<string> { string.Join(",", mixtureHeader.Select(CsvEscape)) };
            var truthLines = new List<string> { string.Join(",", truthHeader.Select(CsvEscape)) };

            for (int i = 0; i < spec.ProfileCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var simResult = SimulateOnce(spec, contributors, lociToUse);
                string sampleName = BuildSampleName(spec.StartingSeries, i);
                sampleNames.Add(sampleName);

                mixtureLines.Add(string.Join(",",
                    BuildMixtureRow(sampleName, spec, contributors, simResult, lociToUse, locusThresholds)
                        .Select(CsvEscape)));

                truthLines.Add(string.Join(",",
                    BuildTruthRow(sampleName, contributors, simResult, lociToUse)
                        .Select(CsvEscape)));

                progress?.Report((i + 1, spec.ProfileCount));
            }

            EnsureDirectory(mixturesFile);
            File.WriteAllLines(mixturesFile, mixtureLines, Encoding.UTF8);
            File.WriteAllLines(truthFile, truthLines, Encoding.UTF8);

            result.Written = sampleNames.Count;
            result.SampleNames = sampleNames;
            return result;
        }

        private static SimulationResult SimulateOnce(
            BatchSimulationSpec spec,
            List<ContributorParams> contributors,
            List<string> lociToUse)
        {
            var simParams = new SimulationParams
            {
                Kit = spec.Kit,
                Template = spec.Template,
                LociToUse = lociToUse,
                ContributorParams = contributors,
                LastProfilesBackup = new List<Profile>(),
                CachedPedigree = null
            };
            return SimulationService.GenerateProfiles(simParams);
        }

        // ---------- ML CSV schema ----------

        private static List<string> BuildMixtureHeader(int noc, List<string> lociToUse)
        {
            var h = new List<string>
            {
                "sample_id", "kit", "template", "noc"
            };

            for (int c = 1; c <= noc; c++)
            {
                h.Add($"prop_{c}");
                h.Add($"deg_{c}");
                h.Add($"sex_{c}");
                h.Add($"db_{c}");
                h.Add($"rel_{c}");
            }

            foreach (var locus in lociToUse)
            {
                for (int k = 1; k <= MaxPeaksPerLocus; k++)
                {
                    h.Add($"{locus}_allele_{k}");
                    h.Add($"{locus}_height_{k}");
                }
            }

            return h;
        }

        private static List<string> BuildTruthHeader(int noc, List<string> lociToUse)
        {
            var h = new List<string> { "sample_id", "noc" };

            for (int c = 1; c <= noc; c++)
            {
                h.Add($"prop_{c}");
                h.Add($"deg_{c}");
                h.Add($"sex_{c}");
                h.Add($"db_{c}");
                h.Add($"rel_{c}");

                foreach (var locus in lociToUse)
                {
                    h.Add($"C{c}_{locus}_a1");
                    h.Add($"C{c}_{locus}_a2");
                }
            }

            return h;
        }

        private static List<string> BuildMixtureRow(
            string sampleId,
            BatchSimulationSpec spec,
            List<ContributorParams> contributors,
            SimulationResult simResult,
            List<string> lociToUse,
            Dictionary<string, int> locusThresholds)
        {
            int noc = contributors.Count;
            var row = new List<string>
            {
                sampleId,
                spec.Kit,
                spec.Template.ToString("G", CultureInfo.InvariantCulture),
                noc.ToString(CultureInfo.InvariantCulture)
            };

            for (int c = 0; c < noc; c++)
            {
                var cp = contributors[c];
                row.Add(cp.Proportion.ToString("G", CultureInfo.InvariantCulture));
                row.Add(cp.Degradation.ToString("G", CultureInfo.InvariantCulture));
                row.Add(cp.Sex.ToString());
                row.Add(cp.Database);
                row.Add(cp.Relationship);
            }

            // Peaks by locus, allele ascending, post-threshold (same as STRmix export)
            var peaksByLocus = simResult.FinalPeaks
                .Where(p =>
                {
                    int threshold = locusThresholds.TryGetValue(p.Locus, out int t) ? t : 100;
                    return p.Height >= threshold;
                })
                .GroupBy(p => p.Locus)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var locus in lociToUse)
            {
                var ordered = new List<Peak>();
                if (peaksByLocus.TryGetValue(locus, out var list))
                {
                    ordered = list
                        .OrderBy(p => AlleleSortKey(p.Allele))
                        .ThenBy(p => p.Allele, StringComparer.Ordinal)
                        .Take(MaxPeaksPerLocus)
                        .ToList();
                }

                for (int k = 0; k < MaxPeaksPerLocus; k++)
                {
                    if (k < ordered.Count)
                    {
                        row.Add(ordered[k].Allele);
                        row.Add(Math.Round(ordered[k].Height).ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        row.Add("");
                        row.Add("0");
                    }
                }
            }

            return row;
        }

        private static List<string> BuildTruthRow(
            string sampleId,
            List<ContributorParams> contributors,
            SimulationResult simResult,
            List<string> lociToUse)
        {
            int noc = contributors.Count;
            var row = new List<string>
            {
                sampleId,
                noc.ToString(CultureInfo.InvariantCulture)
            };

            for (int c = 0; c < noc; c++)
            {
                var cp = contributors[c];
                row.Add(cp.Proportion.ToString("G", CultureInfo.InvariantCulture));
                row.Add(cp.Degradation.ToString("G", CultureInfo.InvariantCulture));
                row.Add(cp.Sex.ToString());
                row.Add(cp.Database);
                row.Add(cp.Relationship);

                Profile? profile = c < simResult.Profiles.Count ? simResult.Profiles[c] : null;

                foreach (var locus in lociToUse)
                {
                    if (profile != null && profile.Loci.TryGetValue(locus, out var alleles))
                    {
                        row.Add(alleles.A1);
                        row.Add(alleles.A2);
                    }
                    else
                    {
                        row.Add("");
                        row.Add("");
                    }
                }
            }

            return row;
        }

        private static double AlleleSortKey(string allele)
        {
            if (double.TryParse(allele, NumberStyles.Float, CultureInfo.InvariantCulture, out double n))
                return n;
            // Non-numeric (AMEL X/Y, etc.): keep stable order after numerics
            if (string.Equals(allele, "X", StringComparison.OrdinalIgnoreCase)) return 1e9;
            if (string.Equals(allele, "Y", StringComparison.OrdinalIgnoreCase)) return 1e9 + 1;
            return 1e9 + 2;
        }

        // ---------- STRmix txt helpers ----------

        private static void AppendStrmixSampleRows(
            List<string> lines,
            string sampleName,
            List<Peak> peaks,
            List<string> lociToUse,
            Dictionary<string, int> locusThresholds)
        {
            var filtered = peaks.Where(p =>
            {
                int threshold = locusThresholds.TryGetValue(p.Locus, out int t) ? t : 100;
                return p.Height >= threshold;
            });

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
                          .OrderBy(p => AlleleSortKey(p.Allele))
                          .ThenBy(p => p.Allele, StringComparer.Ordinal)
                          .ToList());

            foreach (var locus in lociToUse)
            {
                if (!mergedByLocus.TryGetValue(locus, out var locusPeaks)) continue;

                var row = new List<string> { sampleName, locus };

                for (int i = 0; i < MaxPeaksPerLocus; i++)
                    row.Add(i < locusPeaks.Count ? locusPeaks[i].Allele : "");

                for (int i = 0; i < MaxPeaksPerLocus; i++)
                    row.Add(i < locusPeaks.Count && locusPeaks[i].Mw.HasValue
                        ? locusPeaks[i].Mw!.Value.ToString("0.00", CultureInfo.InvariantCulture)
                        : "");

                for (int i = 0; i < MaxPeaksPerLocus; i++)
                    row.Add(i < locusPeaks.Count
                        ? Math.Round(locusPeaks[i].Height).ToString(CultureInfo.InvariantCulture)
                        : "");

                lines.Add(string.Join("\t", row));
            }
        }

        // ---------- shared ----------

        public static string BuildSampleName(int startingSeries, int zeroBasedIndex)
        {
            int series = startingSeries + zeroBasedIndex / LettersPerSeries;
            int letterIndex = zeroBasedIndex % LettersPerSeries;
            int first = letterIndex / 26;
            int second = letterIndex % 26;
            return $"{series:D4}-{(char)('A' + first)}{(char)('A' + second)}";
        }

        private static void EnsureDirectory(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }

        private static string CsvEscape(string value)
        {
            if (value == null) return "";
            bool needQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!needQuotes) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
