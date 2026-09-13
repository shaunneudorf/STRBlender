using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using STRBlender.Core.Application;
using STRBlender.Core.Domain.Common;
using STRBlender.Presentation.ViewModels;
using STRBlender.Presentation.Views.Controls;

namespace STRBlender.Presentation.Views
{
    public partial class ProfileGenView : UserControl
    {
        private readonly SyntheticProfilesViewModel _viewModel;
        private readonly List<ContributorControl> _contributorControls = new();
        private CancellationTokenSource? _cts;

        public ProfileGenView()
        {
            InitializeComponent();
            _viewModel = (SyntheticProfilesViewModel)DataContext;
            InitializeControls();
        }

        private void InitializeControls()
        {
            comboBox_Kit.ItemsSource = KitConfig.Kits.Keys.ToList();
            comboBox_Kit.SelectedIndex = 0;
            UpdateContributors(1);
        }

        private void UpdateContributors(int count)
        {
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

        private void Slider_Contributors_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (panel_Contributors == null) return;
            if (e.NewValue > 0)
                UpdateContributors((int)e.NewValue);
        }

        private void ComboBox_Kit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_Kit.SelectedItem is string kit)
                _viewModel.SelectedKit = kit;
        }

        private async void Btn_Generate_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsRunning) return;

            if (_viewModel.ProfileCount < 1)
            {
                MessageBox.Show("Number of profiles must be at least 1.", "Synthetic Profiles",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var contributorParams = _contributorControls.Select(c => c.GetParams()).ToList();
            if (contributorParams.Count < 1)
            {
                MessageBox.Show("Add at least one contributor.", "Synthetic Profiles",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool ml = _viewModel.OutputMode == BatchOutputMode.MlCsv;

            var dialog = new SaveFileDialog
            {
                Filter = ml
                    ? "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                    : "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = ml
                    ? $"Synthetic_{_viewModel.StartingSeries:D4}"
                    : $"Synthetic_{_viewModel.StartingSeries:D4}_Q.txt",
                Title = ml ? "Save ML training CSV pair (base name)" : "Save synthetic sample batch",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
                return;

            var spec = new BatchSimulationSpec
            {
                Kit = _viewModel.SelectedKit,
                Template = _viewModel.Template,
                ProfileCount = _viewModel.ProfileCount,
                ContributorParams = contributorParams,
                StartingSeries = Math.Max(1, _viewModel.StartingSeries),
                OutputMode = _viewModel.OutputMode
            };

            _cts = new CancellationTokenSource();
            _viewModel.IsRunning = true;
            _viewModel.ProgressDone = 0;
            _viewModel.ProgressTotal = spec.ProfileCount;
            _viewModel.StatusMessage = "Running…";

            var progress = new Progress<(int done, int total)>(p =>
            {
                _viewModel.ProgressDone = p.done;
                _viewModel.ProgressTotal = p.total;
            });

            string outputPath = dialog.FileName;

            try
            {
                var batchResult = await Task.Run(
                    () => BatchSimulationService.Run(spec, outputPath, progress, _cts.Token),
                    _cts.Token);

                if (!string.IsNullOrEmpty(batchResult.ErrorMessage))
                {
                    _viewModel.StatusMessage = batchResult.ErrorMessage;
                    MessageBox.Show(batchResult.ErrorMessage, "Synthetic Profiles",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ml)
                {
                    _viewModel.StatusMessage =
                        $"Wrote {batchResult.Written} samples.{Environment.NewLine}" +
                        $"Mixtures: {batchResult.OutputPath}{Environment.NewLine}" +
                        $"Truth: {batchResult.TruthPath}";

                    MessageBox.Show(
                        $"Successfully wrote {batchResult.Written} samples.\n\n" +
                        $"First: {batchResult.SampleNames[0]}\n" +
                        $"Last:  {batchResult.SampleNames[^1]}\n\n" +
                        $"Mixtures:\n{batchResult.OutputPath}\n\n" +
                        $"Truth:\n{batchResult.TruthPath}",
                        "Synthetic Profiles — ML CSV",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    _viewModel.StatusMessage =
                        $"Wrote {batchResult.Written} samples to:{Environment.NewLine}{batchResult.OutputPath}";

                    MessageBox.Show(
                        $"Successfully wrote {batchResult.Written} samples.\n\n" +
                        $"First: {batchResult.SampleNames[0]}\n" +
                        $"Last:  {batchResult.SampleNames[^1]}\n\n" +
                        $"File:\n{batchResult.OutputPath}",
                        "Synthetic Profiles — STRmix",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                _viewModel.StatusMessage = "Cancelled.";
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = "Error: " + ex.Message;
                MessageBox.Show($"Batch failed:\n\n{ex.Message}", "Synthetic Profiles",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _viewModel.IsRunning = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _viewModel.StatusMessage = "Cancelling…";
        }
    }
}
