using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Domain.Services;

namespace STRBlender.Presentation.Plotting
{
    public static class EpgPlotter
    {
        public static void DrawChannel(
            Canvas canvas,
            List<Peak> finalPeaks,
            List<string> loci,
            RgbColor color,
            int threshold)
        {
            canvas.Children.Clear();

            double W = canvas.ActualWidth;
            double H = canvas.ActualHeight;

            if (W < 10 || H < 10)
            {
                canvas.Loaded += (s, e) => DrawChannel(canvas, finalPeaks, loci, color, threshold);
                return;
            }

            var model = EpgChartBuilder.BuildChannelChart(finalPeaks, loci, color, threshold, W, H);

            // Store yAxisMax on canvas Tag so DrawOverlay can use the same scale
            canvas.Tag = model.YAxisMax;

            var wpfColor = System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
            var brush = new SolidColorBrush(wpfColor);
            var lightBrush = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(60, color.R, color.G, color.B));

            // === THRESHOLD LINE ===
            canvas.Children.Add(new Line
            {
                X1 = model.ThresholdLine.X1,
                Y1 = model.ThresholdLine.Y1,
                X2 = model.ThresholdLine.X2,
                Y2 = model.ThresholdLine.Y2,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 255, 120, 120)),
                StrokeThickness = 0.8,
                StrokeDashArray = new DoubleCollection { 4, 3 },
            });

            // === FILLED CURVE ===
            var fillPoints = new PointCollection();
            foreach (var p in model.FillPolygonPoints)
                fillPoints.Add(new Point(p.X, p.Y));

            canvas.Children.Add(new Polygon
            {
                Points = fillPoints,
                Fill = lightBrush,
                Stroke = Brushes.Transparent,
            });

            // === CURVE LINE ===
            var linePoints = new PointCollection();
            foreach (var p in model.CurveLinePoints)
                linePoints.Add(new Point(p.X, p.Y));

            canvas.Children.Add(new Polyline
            {
                Points = linePoints,
                Stroke = brush,
                StrokeThickness = 0.8,
                StrokeLineJoin = PenLineJoin.Round,
            });

            // === AXES ===
            canvas.Children.Add(new Line
            {
                X1 = model.YAxisLine.X1,
                Y1 = model.YAxisLine.Y1,
                X2 = model.YAxisLine.X2,
                Y2 = model.YAxisLine.Y2,
                Stroke = Brushes.Black,
                StrokeThickness = 0.8
            });

            canvas.Children.Add(new Line
            {
                X1 = model.XAxisLine.X1,
                Y1 = model.XAxisLine.Y1,
                X2 = model.XAxisLine.X2,
                Y2 = model.XAxisLine.Y2,
                Stroke = Brushes.Black,
                StrokeThickness = 0.8
            });

            // === Y AXIS TICKS ===
            foreach (var tick in model.YTicks)
            {
                canvas.Children.Add(new Line
                {
                    X1 = tick.TickMark.X1,
                    Y1 = tick.TickMark.Y1,
                    X2 = tick.TickMark.X2,
                    Y2 = tick.TickMark.Y2,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.7
                });
                var tickLabel = new TextBlock
                {
                    Text = tick.LabelText,
                    FontSize = 9,
                    Foreground = Brushes.Black,
                };
                Canvas.SetLeft(tickLabel, tick.LabelCenter.X);
                Canvas.SetTop(tickLabel, tick.LabelCenter.Y);
                canvas.Children.Add(tickLabel);
            }

            // === X AXIS TICKS ===
            foreach (var tick in model.XTicks)
            {
                canvas.Children.Add(new Line
                {
                    X1 = tick.TickMark.X1,
                    Y1 = tick.TickMark.Y1,
                    X2 = tick.TickMark.X2,
                    Y2 = tick.TickMark.Y2,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.7
                });
                var bpLabel = new TextBlock
                {
                    Text = tick.LabelText,
                    FontSize = 9,
                    Foreground = Brushes.Black,
                };
                Canvas.SetLeft(bpLabel, tick.LabelCenter.X - 10);
                Canvas.SetTop(bpLabel, tick.LabelCenter.Y - 5);
                canvas.Children.Add(bpLabel);
            }

            // === LOCUS BARS ===
            foreach (var bar in model.LocusBars)
            {
                var locusRect = new Rectangle
                {
                    Width = bar.Width,
                    Height = bar.Height,
                    Fill = new SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(110, color.R, color.G, color.B)),
                    RadiusX = 3,
                    RadiusY = 3,
                };
                Canvas.SetLeft(locusRect, bar.X);
                Canvas.SetTop(locusRect, bar.Y);
                canvas.Children.Add(locusRect);

                var locusLabel = new TextBlock
                {
                    Text = bar.LocusName,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                };
                locusLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(locusLabel, bar.LabelCenter.X - locusLabel.DesiredSize.Width / 2);
                Canvas.SetTop(locusLabel, bar.LabelCenter.Y - locusLabel.DesiredSize.Height / 2);
                canvas.Children.Add(locusLabel);
            }

            // === PEAK LABELS ===
            var boxFillColor = System.Windows.Media.Color.FromArgb(20, color.R, color.G, color.B);
            var boxBorderColor = System.Windows.Media.Color.FromArgb(180, color.R, color.G, color.B);
            var textColor = Brushes.Black;

            foreach (var pl in model.PeakLabels)
            {
                canvas.Children.Add(new Line
                {
                    X1 = pl.StemLine.X1,
                    Y1 = pl.StemLine.Y1,
                    X2 = pl.StemLine.X2,
                    Y2 = pl.StemLine.Y2,
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
                    StrokeThickness = 0.8,
                });

                var box = new Border
                {
                    Width = pl.BoxWidth,
                    Height = pl.BoxHeight,
                    Background = new SolidColorBrush(boxFillColor),
                    BorderBrush = new SolidColorBrush(boxBorderColor),
                    BorderThickness = new Thickness(0.8),
                    CornerRadius = new CornerRadius(4),
                    Child = new TextBlock
                    {
                        Text = $"{pl.AlleleText}\n{pl.HeightText}",
                        FontSize = 8,
                        Foreground = textColor,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        LineHeight = 11,
                    }
                };

                Canvas.SetLeft(box, pl.BoxX);
                Canvas.SetTop(box, pl.BoxY);
                canvas.Children.Add(box);
            }
        }

        // === OVERLAY — draws a single contributor's peaks over an existing canvas ===
        public static void DrawOverlay(
            Canvas canvas,
            List<Peak> peaks,
            List<string> loci,
            RgbColor color,
            int threshold,
            byte alpha = 130)
        {
            double W = canvas.ActualWidth;
            double H = canvas.ActualHeight;
            if (W < 10 || H < 10) return;

            // Use the yAxisMax stored by DrawChannel so scales match exactly
            double yAxisMax = canvas.Tag is double storedMax ? storedMax : 5000;

            var model = EpgChartBuilder.BuildOverlay(peaks, loci, W, H, yAxisMax);
            if (model.LinePoints.Count == 0) return;

            var fillColor = System.Windows.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
            var lineColor = System.Windows.Media.Color.FromArgb(220, color.R, color.G, color.B);

            var fillPoints = new PointCollection();
            foreach (var p in model.FillPolygonPoints)
                fillPoints.Add(new Point(p.X, p.Y));

            canvas.Children.Add(new Polygon
            {
                Points = fillPoints,
                Fill = new SolidColorBrush(fillColor),
                Stroke = Brushes.Transparent,
                Tag = "overlay",
            });

            var linePoints = new PointCollection();
            foreach (var p in model.LinePoints)
                linePoints.Add(new Point(p.X, p.Y));

            canvas.Children.Add(new Polyline
            {
                Points = linePoints,
                Stroke = new SolidColorBrush(lineColor),
                StrokeThickness = 1.2,
                StrokeLineJoin = PenLineJoin.Round,
                Tag = "overlay",
            });
        }

        // === CLEAR OVERLAYS — removes only overlay elements, leaves base plot intact ===
        public static void ClearOverlays(Canvas canvas)
        {
            var toRemove = canvas.Children
                .OfType<FrameworkElement>()
                .Where(e => e.Tag is string s && s == "overlay")
                .ToList();
            foreach (var el in toRemove)
                canvas.Children.Remove(el);
        }

        public static void SetMixtureOpacity(Canvas canvas, double opacity)
        {
            foreach (FrameworkElement el in canvas.Children)
            {
                // Only dim base elements, not overlays
                if (el.Tag is string s && s == "overlay") continue;
                el.Opacity = opacity;
            }
        }
    }
}