using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Domain.Services;
using STRBlender.Core.Infrastructure.Data;

const int ProfileCount = 1000;
const string Kit = "GLOBALFILER";
const string Database = "GF NIST Asian"; // full GlobalFiler locus coverage
const double AnalyticalThreshold = 50.0; // RFU; peaks below this aren't "detected" in real practice

// Prefer the data folder next to the running binary / project, not a hardcoded Windows path.
// Falls back to AppPaths which already searches BaseDirectory and CurrentDirectory.
try
{
    // If we're running from the PeakModelTest bin folder, walk up to the solution root
    // that contains the shared data/ directory (STRBlender/data or sibling).
    string? dir = AppDomain.CurrentDomain.BaseDirectory;
    for (int i = 0; i < 6 && dir != null; i++)
    {
        string candidate = Path.Combine(dir, "data");
        if (Directory.Exists(candidate) && Directory.Exists(Path.Combine(candidate, "frequencies")))
        {
            Directory.SetCurrentDirectory(dir);
            break;
        }
        dir = Directory.GetParent(dir)?.FullName;
    }
}
catch { /* AppPaths will still try its own candidates */ }

var lociToUse = KitConfig.Kits[Kit];
LocusDefinitions.SetKit(Kit); // switches LocusDefinitions' active locus table to GlobalFiler's MW/efficiency params
string freqPath = AppPaths.GetFrequencyFilePath(KitConfig.DatabaseOptions[Database]);
var freqs = FrequencyLoader.LoadAlleleFrequencies(freqPath, lociToUse);

var reverseRules = StutterCalculator.Load(AppPaths.ReverseStutterPath);
var forwardRules = StutterCalculator.Load(AppPaths.ForwardStutterPath);

var rng = Random.Shared;

// Generic per-locus stat accumulator, reused for PHR, reverse stutter, and forward stutter
var phrStats = new Dictionary<string, (double Min, double Max, double Sum, int N)>();
var revStats = new Dictionary<string, (double Min, double Max, double Sum, int N)>();
var fwdStats = new Dictionary<string, (double Min, double Max, double Sum, int N)>();

static void Record(Dictionary<string, (double Min, double Max, double Sum, int N)> stats, string locus, double value)
{
    if (stats.TryGetValue(locus, out var s))
        stats[locus] = (Math.Min(s.Min, value), Math.Max(s.Max, value), s.Sum + value, s.N + 1);
    else
        stats[locus] = (value, value, value, 1);
}

for (int i = 0; i < ProfileCount; i++)
{
    var profile = ProfileGenerator.SampleProfile(freqs, lociToUse, SexType.Random);

    // Single-source profile. Degradation randomized across a realistic range
    // matching the NOC game / contributor controls (~ -0.003 to 0).
    double template = 2000;
    double proportion = 1.0;
    double degradation = -(rng.NextDouble() * 0.003);

    // BuildPeakTable still emits illustrative stutter entries for overlay use.
    // The production pipeline (and this test) only feeds true-allele ("parent")
    // peaks into BuildFinalPeaks, which:
    //   Stage 1 – stacks parents and applies allelic stochastic variation → Oa
    //   Stage 2 – derives reverse/forward stutter from each finalized Oa
    //   then merges additively.
    var peakList = PeakCalculator.BuildPeakTable(
        profile, template, proportion, degradation, reverseRules, forwardRules, lociToUse);

    var alleleOnlyPeaks = peakList.Where(p => p.PeakType == "parent").ToList();

    // Replicate Stage 1 so we can inspect pure parent heights for PHR
    // (before any stutter is added onto those positions).
    var parentHeights = new Dictionary<(string Locus, string Allele), double>();
    var parentBase = new Dictionary<(string Locus, string Allele), double>();

    foreach (var peak in alleleOnlyPeaks)
    {
        var key = (peak.Locus, peak.Allele);
        parentBase[key] = parentBase.GetValueOrDefault(key) + peak.BaseHeight;
    }

    foreach (var kvp in parentBase)
    {
        parentHeights[kvp.Key] = PeakCalculator.ApplyStochasticVariation(kvp.Value, "parent");
    }

    // PHR: heterozygous STR loci only (two distinct parent alleles after stacking).
    // AMEL / Y markers excluded.
    foreach (var locusGroup in parentHeights.Keys.GroupBy(k => k.Locus))
    {
        string locus = locusGroup.Key;
        if (locus is "AMEL" or "YINDEL" or "DYS391") continue;

        var alleles = locusGroup.ToList();
        if (alleles.Count != 2) continue;

        double h1 = parentHeights[alleles[0]];
        double h2 = parentHeights[alleles[1]];

        if (h1 >= AnalyticalThreshold && h2 >= AnalyticalThreshold)
        {
            double larger = Math.Max(h1, h2);
            double smaller = Math.Min(h1, h2);
            Record(phrStats, locus, smaller / larger);
        }
    }

    // Full final peaks (parents + stutter generated from realized Oa)
    var finalPeaks = PeakCalculator.BuildFinalPeaks(
        alleleOnlyPeaks, reverseRules, forwardRules, lociToUse);

    // For stutter ratios we need the true parent Oa and the stutter contribution
    // that was generated from it. BuildFinalPeaks merges everything, so we
    // recompute Stage-2 stutter heights the same way GenerateStutterContributions does.
    foreach (var parentKvp in parentHeights)
    {
        var (locus, alleleStr) = parentKvp.Key;
        if (!double.TryParse(alleleStr, out double alleleNum)) continue;

        double oa = parentKvp.Value;
        if (oa < AnalyticalThreshold) continue;

        double srRev = reverseRules?.GetStutterRatio(locus, alleleStr) ?? 0.0;
        double srFwd = forwardRules?.GetStutterRatio(locus, alleleStr) ?? 0.0;

        if (srRev > 0.001)
        {
            // Expected stutter = Oa * ratio; then the same log-normal draw used in BuildStutterPeak
            // We cannot recover the exact draw that BuildFinalPeaks made, so for ratio statistics
            // we sample a fresh independent draw with the identical model. Over many profiles
            // the distribution of observed ratios converges to the theoretical one.
            double stutterHeight = SampleStutterHeight(oa, srRev, "reverse_stutter");
            Record(revStats, locus, stutterHeight / oa);
        }

        if (srFwd > 0.001)
        {
            double stutterHeight = SampleStutterHeight(oa, srFwd, "forward_stutter");
            Record(fwdStats, locus, stutterHeight / oa);
        }
    }
}

// Local helper that mirrors PeakCalculator.BuildStutterPeak variance model
// (constants are private inside PeakCalculator, so we duplicate the small formula).
static double SampleStutterHeight(double oa, double ratio, string peakType)
{
    // Same constants as PeakCalculator
    const double B = 1000.0;
    double k = peakType == "forward_stutter" ? 11.50 : 5.79;
    double sigma = Math.Sqrt(k / ((B / oa) + oa));
    // Box-Muller
    double u1 = 1.0 - Random.Shared.NextDouble();
    double u2 = 1.0 - Random.Shared.NextDouble();
    double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    double noise = sigma * z;
    double expected = oa * ratio;
    return expected * Math.Pow(10, noise);
}

void PrintTable(string title, Dictionary<string, (double Min, double Max, double Sum, int N)> stats)
{
    Console.WriteLine(title);
    Console.WriteLine(new string('-', 50));
    Console.WriteLine($"{"Locus",-12}{"Min",8}{"Max",8}{"Avg",8}{"N",8}");
    foreach (var locus in lociToUse)
    {
        if (!stats.TryGetValue(locus, out var s)) continue; // no qualifying observations for this locus
        Console.WriteLine($"{locus,-12}{s.Min,8:F3}{s.Max,8:F3}{s.Sum / s.N,8:F3}{s.N,8}");
    }
    Console.WriteLine();
}

PrintTable($"GlobalFiler Peak Height Ratio by Locus  ({ProfileCount} single-source profiles)", phrStats);
PrintTable("Reverse Stutter Ratio by Locus", revStats);
PrintTable("Forward Stutter Ratio by Locus", fwdStats);