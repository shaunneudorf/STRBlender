using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Domain.Services;
using STRBlender.Presentation.Plotting;
using STRBlender.Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace STRBlender.Presentation.Views
{
    public partial class NocGameView : UserControl
    {
        private readonly NocGameViewModel _viewModel;
        private readonly Random _rng = new();
        private int _actualNoc = 0;
        private List<ContributorParams> _actualContributors = new();

        public NocGameView()
        {
            InitializeComponent();
            _viewModel = (NocGameViewModel)DataContext;

            comboBox_Kit.ItemsSource = KitConfig.Kits.Keys.ToList();
            comboBox_Kit.SelectedIndex = 0;
        }

        private void ComboBox_Kit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_Kit.SelectedItem is string kit)
                _viewModel.SelectedKit = kit;
        }

        private void btn_Generate_Click(object sender, RoutedEventArgs e)
        {
            _actualNoc = _rng.Next(_viewModel.MinContributors, _viewModel.MaxContributors + 1);
            _actualContributors = GenerateRandomContributors(_actualNoc);

            double template = _rng.NextDouble() *
                (_viewModel.MaxTemplate - _viewModel.MinTemplate)
                + _viewModel.MinTemplate;

            _viewModel.RunSimulation(_actualContributors, _viewModel.SelectedKit, template);

            DrawPlots();
            ClearGuess();

            _viewModel.MixtureGenerated = true;
            _viewModel.ResultVisible = Visibility.Collapsed;

            // Hide overlay panel when new mixture generated
            panel_ContributorOverlays.Visibility = Visibility.Collapsed;
            list_ContributorChecks.Items.Clear();
        }

        private List<ContributorParams> GenerateRandomContributors(int count)
        {
            var contributors = new List<ContributorParams>();
            var proportions = GenerateRandomProportions(count);

            for (int i = 0; i < count; i++)
            {
                double degradation = _rng.NextDouble() *
                    (_viewModel.MaxDegradation - _viewModel.MinDegradation)
                    + _viewModel.MinDegradation;

                contributors.Add(new ContributorParams(
                    Proportion: proportions[i],
                    Degradation: degradation,
                    Database: _viewModel.SelectedDatabase,
                    Relationship: "Unrelated",
                    Locked: false,
                    Sex: SexType.Random
                ));
            }
            return contributors;
        }

        private List<double> GenerateRandomProportions(int count)
        {
            if (count == 1)
                return new List<double> { 1.0 };

            var proportions = new List<double>();
            double remaining = 1.0;
            double min = _viewModel.MinProportion;
            double max = _viewModel.MaxProportion;

            for (int i = 0; i < count - 1; i++)
            {
                // Calculate safe upper bound for this draw
                double safeMax = Math.Min(max, remaining - min * (count - i - 1));
                safeMax = Math.Max(min, safeMax); // ensure safeMax >= min

                double value = min + _rng.NextDouble() * (safeMax - min);
                proportions.Add(value);
                remaining -= value;
            }

            // Last contributor gets whatever is left
            proportions.Add(Math.Max(min, remaining));

            // Optional: shuffle to avoid last one always being largest/smallest
            proportions = proportions.OrderBy(x => _rng.Next()).ToList();

            return proportions;
        }

        private void DrawPlots()
        {
            epgHost.DrawMixture(_viewModel.FinalPeaks, _viewModel.SelectedKit);
        }

        private void btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            int guess = GetGuess();
            if (guess == 0)
            {
                MessageBox.Show("Please select a NOC guess first.", "No Guess Selected");
                return;
            }

            bool correct = guess == _actualNoc;

            if (correct)
            {
                _viewModel.ScoreCorrect++;
                _viewModel.Streak++;
                _viewModel.ResultVerdict = "✓ Correct!";
                _viewModel.ResultColor = new SolidColorBrush(Colors.Green);
            }
            else
            {
                _viewModel.ScoreWrong++;
                _viewModel.Streak = 0;
                _viewModel.ResultVerdict = "✗ Incorrect";
                _viewModel.ResultColor = new SolidColorBrush(Colors.Red);
            }

            var detail = $"Actual NOC: {_actualNoc}";
            _viewModel.ResultDetail = detail;

            _viewModel.ResultVisible = Visibility.Visible;
            _viewModel.MixtureGenerated = false;

            // Inside btn_Submit_Click, after setting ResultDetail:
            panel_ContributorOverlays.Visibility = Visibility.Visible;
            list_ContributorChecks.Items.Clear();

            for (int i = 0; i < _actualContributors.Count; i++)
            {
                var c = _actualContributors[i];
                int contributorIndex = i;

                var cb = new CheckBox
                {
                    Content = $"C{i + 1}   Prop={c.Proportion:F2}   Deg={c.Degradation:F4}",
                    FontSize = 10,
                    Margin = new Thickness(0, 4, 0, 4),
                    Tag = contributorIndex,
                    IsChecked = false
                };
                cb.Checked += (s, ev) => ToggleOverlay(contributorIndex, true);
                cb.Unchecked += (s, ev) => ToggleOverlay(contributorIndex, false);
                list_ContributorChecks.Items.Add(cb);
            }
        }

        private void ToggleOverlay(int contributorIndex, bool show)
        {
            var channels = _viewModel.SelectedKit == "GLOBALFILER"
                ? KitConfig.GlobalfilerChannels
                : KitConfig.IdplusChannels;

            // Clear ONLY overlays (do NOT clear main mixture)
            EpgPlotter.ClearOverlays(epgHost.BlueCanvas);
            EpgPlotter.ClearOverlays(epgHost.GreenCanvas);
            EpgPlotter.ClearOverlays(epgHost.YellowCanvas);
            EpgPlotter.ClearOverlays(epgHost.RedCanvas);
            EpgPlotter.ClearOverlays(epgHost.PurpleCanvas);

            var checkedIndices = list_ContributorChecks.Items
                .OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => (int)cb.Tag)
                .ToList();

            bool anyChecked = checkedIndices.Any();
            double opacity = anyChecked ? 0.25 : 1.0;

            // Set mixture opacity
            EpgPlotter.SetMixtureOpacity(epgHost.BlueCanvas, opacity);
            EpgPlotter.SetMixtureOpacity(epgHost.GreenCanvas, opacity);
            EpgPlotter.SetMixtureOpacity(epgHost.YellowCanvas, opacity);
            EpgPlotter.SetMixtureOpacity(epgHost.RedCanvas, opacity);
            EpgPlotter.SetMixtureOpacity(epgHost.PurpleCanvas, opacity);

            if (!anyChecked) return;

            var overlayPeaks = PeakCalculator.ComputeOverlayAttribution(
                _viewModel.BaseContributorPeaks, checkedIndices, _viewModel.FinalPeaks);

            // 3. Draw the attributed peaks as the overlay
            foreach (var (channelName, channel) in channels)
            {
                Canvas? canvas = channelName switch
                {
                    "Blue" => epgHost.BlueCanvas,
                    "Green" => epgHost.GreenCanvas,
                    "Yellow" => epgHost.YellowCanvas,
                    "Red" => epgHost.RedCanvas,
                    "Purple" => epgHost.PurpleCanvas,
                    _ => null
                };

                if (canvas == null || canvas.Visibility != Visibility.Visible) continue;

                EpgPlotter.DrawOverlay(canvas, overlayPeaks, channel.Loci,
                    channel.Color, channel.Threshold, alpha: 190);
            }
        }
        private int GetGuess()
        {
            if (rb_Noc1.IsChecked == true) return 1;
            if (rb_Noc2.IsChecked == true) return 2;
            if (rb_Noc3.IsChecked == true) return 3;
            if (rb_Noc4.IsChecked == true) return 4;
            if (rb_Noc5.IsChecked == true) return 5;
            return 0;
        }

        private void ClearGuess()
        {
            rb_Noc1.IsChecked = false;
            rb_Noc2.IsChecked = false;
            rb_Noc3.IsChecked = false;
            rb_Noc4.IsChecked = false;
            rb_Noc5.IsChecked = false;
        }

        private void btn_ResetScore_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ScoreCorrect = 0;
            _viewModel.ScoreWrong = 0;
            _viewModel.Streak = 0;
        }
    }
}