using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Presentation.ViewModels;
using STRBlender.Presentation.Views.Controls;
using STRBlender.Presentation.Plotting;

namespace STRBlender.Presentation.Views
{
    public partial class EpgView : UserControl
    {
        private readonly MainViewModel _viewModel;
        private readonly List<ContributorControl> _contributorControls = new();

        public EpgView()
        {
            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;

            InitializeControls();
        }

        private void InitializeControls()
        {
            // Kit selector
            comboBox_Kit.ItemsSource = KitConfig.Kits.Keys.ToList();
            comboBox_Kit.SelectedIndex = 0;

            UpdateContributors(1);   // Start with 1 contributor
        }

        private void UpdateContributors(int count)
        {
            // Save current state before clearing
            var savedParams = _contributorControls.Select(c => c.GetParams()).ToList();

            panel_Contributors.Children.Clear();
            _contributorControls.Clear();

            var databaseOptions = KitConfig.DatabaseOptions.Keys.ToList();

            for (int i = 0; i < count; i++)
            {
                var saved = i < savedParams.Count ? savedParams[i] : null;
                var control = new ContributorControl(i + 1, saved, databaseOptions);

                panel_Contributors.Children.Add(control);
                _contributorControls.Add(control);
            }
        }

        private void btn_Run_Click(object sender, RoutedEventArgs e)
        {
            var contributorParams = _contributorControls.Select(c => c.GetParams()).ToList();
            _viewModel.SetContributorParams(contributorParams);
            _viewModel.RunCommand.Execute(null);

            MergeAndDisplayPlots();
        }

        private void btn_Resample_Click(object sender, RoutedEventArgs e)
        {
            var contributorParams = _contributorControls.Select(c => c.GetParams()).ToList();
            _viewModel.SetContributorParams(contributorParams);
            _viewModel.ResampleCommand.Execute(null);

            MergeAndDisplayPlots();
        }

        private void MergeAndDisplayPlots()
        {
            epgHost.DrawMixture(_viewModel.FinalPeaks, _viewModel.SelectedKit);
        }

        // In ToggleOverlay / other places use epgHost.XXXCanvas if needed
        // Event Handlers
        private void Slider_Contributors_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (panel_Contributors == null) return;
            if (e.NewValue > 0)
                UpdateContributors((int)e.NewValue);
        }

        private void ComboBox_Kit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_Kit.SelectedItem is string kit)
            {
                _viewModel.SelectedKit = kit;

                // Refresh plots if we already have data
                if (_viewModel.FinalPeaks.Count > 0)
                    MergeAndDisplayPlots();
            }
        }

        private void btn_ViewContributors_Click(object sender, RoutedEventArgs e) => _viewModel.ViewContributorsCommand.Execute(null);
        private void btn_ExportSample_Click(object sender, RoutedEventArgs e) => _viewModel.ExportSampleCommand.Execute(null);
        private void btn_ExportReference_Click(object sender, RoutedEventArgs e) => _viewModel.ExportReferenceCommand.Execute(null);
    }
}