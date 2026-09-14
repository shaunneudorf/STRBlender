using System;
using System.Collections.Generic;
using System.Linq;
using STRBlender.Core.Domain.Common;

namespace STRBlender.Core.Domain.Models
{
    public record ChartPoint(double X, double Y);
    public record ChartLineSegment(double X1, double Y1, double X2, double Y2);

    /// A single axis tick: the small perpendicular tick mark, plus where its
    /// text label should be centered and what it should say.
    public record ChartTick(ChartLineSegment TickMark, ChartPoint LabelCenter, string LabelText);

    /// A locus name bar drawn along the top of the plot. LabelCenter is where
    /// the locus name should be centered — each platform uses its own native
    /// text-centering (SVG text-anchor="middle", WPF TextAlignment.Center)
    /// rather than Core pre-measuring text width, which isn't portable.
    public record ChartLocusBar(double X, double Y, double Width, double Height, ChartPoint LabelCenter, string LocusName);

    /// One peak's allele/height label box, plus the connecting stem line from
    /// the peak apex up to the box.
    public record ChartPeakLabel(
        ChartLineSegment StemLine,
        double BoxX, double BoxY, double BoxWidth, double BoxHeight,
        string AlleleText, string HeightText);

    /// Everything needed to draw one channel's EPG plot, already computed in
    /// screen-space pixel coordinates (top-left origin, Y increasing downward —
    /// the convention both WPF Canvas and SVG share). A platform's renderer
    /// just needs to iterate this and emit its own native shapes; no further
    /// math is needed.
    public class EpgChartModel
    {
        public double PlotWidth { get; set; }
        public double PlotHeight { get; set; }
        public double MarginLeft { get; set; }
        public double MarginTop { get; set; }
        public double YAxisMax { get; set; }

        public ChartLineSegment ThresholdLine { get; set; } = null!;
        public List<ChartPoint> FillPolygonPoints { get; set; } = new();
        public List<ChartPoint> CurveLinePoints { get; set; } = new();
        public ChartLineSegment YAxisLine { get; set; } = null!;
        public ChartLineSegment XAxisLine { get; set; } = null!;
        public List<ChartTick> YTicks { get; set; } = new();
        public List<ChartTick> XTicks { get; set; } = new();
        public List<ChartLocusBar> LocusBars { get; set; } = new();
        public List<ChartPeakLabel> PeakLabels { get; set; } = new();
    }

    /// A single contributor's overlay curve on top of an existing channel plot
    /// (used by the NOC game) — fill + line only, drawn at the same scale as
    /// the base chart it overlays.
    public class EpgOverlayModel
    {
        public List<ChartPoint> FillPolygonPoints { get; set; } = new();
        public List<ChartPoint> LinePoints { get; set; } = new();
    }
}

namespace STRBlender.Core.Domain.Services
{
    using STRBlender.Core.Domain.Models;

    /// Pure chart-layout math extracted from the WPF app's original EpgPlotter.
    /// Contains zero rendering calls and zero platform dependency — every
    /// value returned is a plain number or already-computed screen coordinate,
    /// so any front end (WPF, Blazor SVG, or otherwise) can consume the same
    /// model and just emit its own native shapes from it.
    public static class EpgChartBuilder
    {
        // === DATA SPACE ===
        public const double XMin = 50.0;
        public const double XMax = 450.0;
        private const int NPoints = 4000;
        private const double Sigma = 0.25;

        public static EpgChartModel BuildChannelChart(
            List<Peak> finalPeaks,
            List<string> loci,
            RgbColor color,
            int threshold,
            double canvasWidth,
            double canvasHeight,
            double marginLeft = 55.0,
            double marginRight = 20.0,
            double marginTop = 25.0,
            double marginBottom = 120.0,
            int samplePoints = 0)
        {
            // samplePoints <= 0 → desktop default NPoints; web should pass ~600–1000.
            int n = samplePoints > 10 ? samplePoints : NPoints;

            double plotW = canvasWidth - marginLeft - marginRight;
            double plotH = canvasHeight - marginTop - marginBottom;

            var visible = finalPeaks
                .Where(p => p.Mw.HasValue && loci.Contains(p.Locus))
                .ToList();

            double[] xData = Enumerable.Range(0, n)
                .Select(i => XMin + i * ((XMax - XMin) / (n - 1)))
                .ToArray();

            double[] yData = new double[n];
            foreach (var peak in visible)
            {
                double mw = peak.Mw!.Value;
                for (int i = 0; i < n; i++)
                {
                    double dx = xData[i] - mw;
                    yData[i] += peak.Height * Math.Exp(-(dx * dx) / (2 * Sigma * Sigma));
                }
            }

            double[] noise = GenerateNoise(color, n);
            double[] yDisplay = xData.Select((_, i) => Math.Max(yData[i], noise[i])).ToArray();

            double yMax = yDisplay.Max();
            double truePeakMax = visible.Any() ? visible.Max(p => p.Height) : 0;
            double yRange = Math.Max(Math.Max(yMax, truePeakMax), 150);
            double yAxisMax = RoundUpNice(yRange * 1.02);

            double ToScreenX(double bp) => marginLeft + (bp - XMin) / (XMax - XMin) * plotW;
            double ToScreenY(double rfu) => marginTop + plotH - (rfu / yAxisMax * plotH);
            double zeroY = ToScreenY(0);

            var model = new EpgChartModel
            {
                PlotWidth = plotW,
                PlotHeight = plotH,
                MarginLeft = marginLeft,
                MarginTop = marginTop,
                YAxisMax = yAxisMax,
            };

            // === THRESHOLD LINE ===
            double threshY = ToScreenY(threshold);
            model.ThresholdLine = new ChartLineSegment(marginLeft, threshY, marginLeft + plotW, threshY);

            // === FILLED CURVE + LINE ===
            model.FillPolygonPoints.Add(new ChartPoint(ToScreenX(xData[0]), zeroY));
            for (int i = 0; i < n; i++)
            {
                var pt = new ChartPoint(ToScreenX(xData[i]), ToScreenY(yDisplay[i]));
                model.FillPolygonPoints.Add(pt);
                model.CurveLinePoints.Add(pt);
            }
            model.FillPolygonPoints.Add(new ChartPoint(ToScreenX(xData[n - 1]), zeroY));

            // === AXES ===
            model.YAxisLine = new ChartLineSegment(marginLeft, marginTop, marginLeft, marginTop + plotH);
            model.XAxisLine = new ChartLineSegment(marginLeft, marginTop, marginLeft + plotW, marginTop);

            // === Y AXIS TICKS ===
            double yTickStep = RoundUpNice(yAxisMax / 6.0);
            for (double rfu = 0; rfu <= yAxisMax; rfu += yTickStep)
            {
                double sy = ToScreenY(rfu);
                model.YTicks.Add(new ChartTick(
                    new ChartLineSegment(marginLeft - 5, sy, marginLeft, sy),
                    new ChartPoint(marginLeft - 25, sy),
                    ((int)rfu).ToString()));
            }

            // === X AXIS TICKS ===
            for (double bp = 100; bp <= 450; bp += 50)
            {
                double sx = ToScreenX(bp);
                model.XTicks.Add(new ChartTick(
                    new ChartLineSegment(sx, marginTop, sx, marginTop - 5),
                    new ChartPoint(sx, marginTop - 13),
                    ((int)bp).ToString()));
            }

            // === LOCUS BARS ===
            double barTop = marginTop + 4;
            double barBottom = marginTop + 22;
            foreach (var locus in loci)
            {
                var (minMw, maxMw) = LocusDefinitions.GetLocusBounds(locus);
                if (minMw == null || maxMw == null) continue;

                double sx1 = ToScreenX(minMw.Value);
                double sx2 = ToScreenX(maxMw.Value);

                model.LocusBars.Add(new ChartLocusBar(
                    sx1, barTop, sx2 - sx1, barBottom - barTop,
                    new ChartPoint((sx1 + sx2) / 2, (barTop + barBottom) / 2),
                    locus));
            }

            // === PEAK LABELS ===
            var labelLevels = new Dictionary<double, int>();
            int maxLevels = 5;
            double levelStep = 24.5;
            double labelTopBase = zeroY + 3.0;
            double boxW = 25.0;
            double boxH = 24.0;
            double hThreshPx = boxW + 4.0;

            foreach (var peak in visible.OrderBy(p => p.Mw))
            {
                if (!peak.Mw.HasValue) continue;
                int idx = Array.IndexOf(xData, xData.MinBy(xi => Math.Abs(xi - peak.Mw.Value)));
                double sampledHeight = yDisplay[idx];
                double effectiveHeight = Math.Max(sampledHeight, peak.Height);
                if (effectiveHeight < threshold) continue;

                double xPos = peak.Mw.Value;

                int bestLevel = 0;
                while (true)
                {
                    bool conflict = labelLevels.Any(kv =>
                        kv.Value == bestLevel &&
                        Math.Abs(ToScreenX(kv.Key) - ToScreenX(xPos)) < hThreshPx);
                    if (!conflict) break;
                    bestLevel++;
                    if (bestLevel >= maxLevels) { bestLevel = 0; break; }
                }
                labelLevels[xPos] = bestLevel;

                double sx = ToScreenX(xPos);
                double sy = ToScreenY(effectiveHeight);
                double boxTop = labelTopBase + bestLevel * levelStep;
                double boxLeft = sx - boxW / 2;

                string alleleText = double.TryParse(peak.Allele, out double av)
                    ? (av == Math.Floor(av) ? ((int)av).ToString() : av.ToString("F1"))
                    : peak.Allele;
                string heightText = ((int)Math.Round(peak.Height)).ToString();

                model.PeakLabels.Add(new ChartPeakLabel(
                    new ChartLineSegment(sx, sy + 4, sx, boxTop),
                    boxLeft, boxTop, boxW, boxH,
                    alleleText, heightText));
            }

            return model;
        }

        /// Builds a single contributor's overlay curve (fill + line only) at
        /// the same scale as an already-built base chart — pass that chart's
        /// YAxisMax so the overlay lines up correctly.
        public static EpgOverlayModel BuildOverlay(
            List<Peak> peaks,
            List<string> loci,
            double canvasWidth,
            double canvasHeight,
            double yAxisMax,
            double marginLeft = 55.0,
            double marginRight = 20.0,
            double marginTop = 25.0,
            double marginBottom = 120.0,
            int nPoints = 2000)
        {
            double plotW = canvasWidth - marginLeft - marginRight;
            double plotH = canvasHeight - marginTop - marginBottom;

            var visible = peaks.Where(p => p.Mw.HasValue && loci.Contains(p.Locus)).ToList();
            var model = new EpgOverlayModel();
            if (!visible.Any()) return model;

            double ToScreenX(double bp) => marginLeft + (bp - XMin) / (XMax - XMin) * plotW;
            double ToScreenY(double rfu) => marginTop + plotH - (rfu / yAxisMax * plotH);
            double zeroY = ToScreenY(0);

            double[] xData = Enumerable.Range(0, nPoints)
                .Select(i => XMin + i * ((XMax - XMin) / (nPoints - 1)))
                .ToArray();

            double[] yData = new double[nPoints];
            foreach (var peak in visible)
            {
                double mw = peak.Mw!.Value;
                for (int i = 0; i < nPoints; i++)
                {
                    double dx = xData[i] - mw;
                    yData[i] += peak.Height * Math.Exp(-(dx * dx) / (2 * Sigma * Sigma));
                }
            }

            model.FillPolygonPoints.Add(new ChartPoint(ToScreenX(xData[0]), zeroY));
            for (int i = 0; i < nPoints; i++)
            {
                var pt = new ChartPoint(ToScreenX(xData[i]), ToScreenY(yData[i]));
                model.FillPolygonPoints.Add(pt);
                model.LinePoints.Add(pt);
            }
            model.FillPolygonPoints.Add(new ChartPoint(ToScreenX(xData[nPoints - 1]), zeroY));

            return model;
        }

        public static double[] GenerateNoise(RgbColor color, int nPoints)
        {
            double mu = color.Name switch
            {
                "Blue" => 1.5,
                "Green" => 2.0,
                "Goldenrod" => 2.5,
                "Red" => 2.3,
                _ => 2.0,
            };

            var rng = new Random();
            double[] raw = new double[nPoints];
            for (int i = 0; i < nPoints; i++)
            {
                double u1 = 1.0 - rng.NextDouble();
                double u2 = 1.0 - rng.NextDouble();
                double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                raw[i] = Math.Exp(mu + 0.6 * z);
            }

            double[] smoothed = new double[nPoints];
            int kernelSize = 4;
            for (int i = 0; i < nPoints; i++)
            {
                double sum = 0, weight = 0;
                for (int j = -kernelSize; j <= kernelSize; j++)
                {
                    int idx = i + j;
                    if (idx < 0 || idx >= nPoints) continue;
                    double w = Math.Exp(-j * j / 8.0);
                    sum += raw[idx] * w;
                    weight += w;
                }
                smoothed[i] = sum / weight;
            }
            return smoothed;
        }

        public static double RoundUpNice(double value)
        {
            if (value <= 0) return 500;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
            double normalized = value / magnitude;
            double nice = normalized <= 1 ? 1 :
                          normalized <= 1.5 ? 1.5 :
                          normalized <= 2 ? 2 :
                          normalized <= 2.5 ? 2.5 :
                          normalized <= 3 ? 3 :
                          normalized <= 4 ? 4 :
                          normalized <= 5 ? 5 :
                          normalized <= 7.5 ? 7.5 : 10;
            return nice * magnitude;
        }
    }
}
