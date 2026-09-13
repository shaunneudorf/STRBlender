using STRBlender.Core.Application;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Domain.Services;
using STRBlender.Presentation.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;

namespace STRBlender.Presentation.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // State (your properties and fields go here)
        private readonly List<Profile> _lastProfiles = new();
        private List<List<Peak>> _lastPeakLists = new();
        private Pedigree? _cachedPedigree;
        private string? _cacheKit;
        private string? _cacheC1Database;

        public ObservableCollection<Peak> Results { get; } = new();

        public List<Peak> AllPeaks { get; private set; } = new();
        public List<Peak> FinalPeaks { get; private set; } = new();

        // Bindable Properties
        private string _selectedKit = "IDPLUS";
        public string SelectedKit
        {
            get => _selectedKit;
            set
            {
                _selectedKit = value;
                LocusDefinitions.SetKit(value);
                OnPropertyChanged();
            }
        }

        private double _template = 2000;
        public double Template
        {
            get => _template;
            set { _template = value; OnPropertyChanged(); }
        }

        private int _contributorCount = 1;
        public int ContributorCount
        {
            get => _contributorCount;
            set { _contributorCount = value; OnPropertyChanged(); }
        }

        private List<ContributorParams> _contributorParams = new();
        public List<ContributorParams> ContributorParams
        {
            get => _contributorParams;
            private set => _contributorParams = value;
        }

        // Form fields
        public string FileNumber { get; set; } = "2025A-000001";
        public string ExhibitNumber { get; set; } = "0001-AA-01";
        public string ItemNumber { get; set; } = "1000";
        public bool AppendToFile { get; set; } = false;

        // Commands
        public ICommand RunCommand { get; }
        public ICommand ResampleCommand { get; }
        public ICommand ViewContributorsCommand { get; }
        public ICommand ExportSampleCommand { get; }
        public ICommand ExportReferenceCommand { get; }

        public MainViewModel()
        {
            RunCommand = new RelayCommand(ExecuteRun);
            ResampleCommand = new RelayCommand(ExecuteResample);
            ViewContributorsCommand = new RelayCommand(ExecuteViewContributors);
            ExportSampleCommand = new RelayCommand(ExecuteExportSample);
            ExportReferenceCommand = new RelayCommand(ExecuteExportReference);
        }

        public void SetContributorParams(List<ContributorParams> paramsList)
        {
            ContributorParams = paramsList ?? new List<ContributorParams>();
        }

        private void ExecuteRun()
        {
            try
            {
                var lociToUse = KitConfig.Kits[SelectedKit];

                string? currentC1Database = ContributorParams.Count > 0 ? ContributorParams[0].Database : null;
                bool cacheStillValid = _cacheKit == SelectedKit && _cacheC1Database == currentC1Database;

                if (!cacheStillValid)
                {
                    // Kit or C1's population changed since these were cached — a
                    // locked C1/relative would otherwise silently reuse profiles
                    // built against a different loci set or allele frequency table.
                    // Discarding both forces a full fresh rebuild this run, as if
                    // nothing were locked, regardless of any Locked checkboxes.
                    _lastProfiles.Clear();
                    _cachedPedigree = null;
                }

                var simParams = new SimulationParams
                {
                    Kit = SelectedKit,
                    Template = Template,
                    LociToUse = lociToUse,
                    ContributorParams = ContributorParams,
                    LastProfilesBackup = new List<Profile>(_lastProfiles),
                    CachedPedigree = _cachedPedigree
                };

                var result = SimulationService.GenerateProfiles(simParams);

                // Update state
                _lastPeakLists = result.PeakLists;
                _lastProfiles.Clear();
                _lastProfiles.AddRange(result.Profiles);
                _cachedPedigree = result.CachedPedigree;
                _cacheKit = SelectedKit;
                _cacheC1Database = currentC1Database;

                AllPeaks = result.AllPeaks;
                FinalPeaks = result.FinalPeaks;

                RefreshDisplayedPeaks();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Simulation error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshDisplayedPeaks()
        {
            Results.Clear();
            if (FinalPeaks == null || FinalPeaks.Count == 0) return;

            var channels = SelectedKit == "GLOBALFILER"
                ? KitConfig.GlobalfilerChannels
                : KitConfig.IdplusChannels;

            var locusThreshold = channels.Values
                .SelectMany(c => c.Loci.Select(locus => new { locus, c.Threshold }))
                .ToDictionary(x => x.locus, x => x.Threshold);

            foreach (var peak in FinalPeaks)
            {
                int threshold = locusThreshold.TryGetValue(peak.Locus, out int t) ? t : 100;

                if (peak.Height >= threshold)
                {
                    Results.Add(new Peak
                    {
                        Locus = peak.Locus,
                        Allele = peak.Allele,
                        Mw = peak.Mw,
                        Height = Math.Round(peak.Height)
                    });
                }
            }
        }

        private void ExecuteResample()
        {
            if (_lastProfiles.Count == 0)
            {
                MessageBox.Show("Run a simulation first.", "Resample", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var originalLocks = ContributorParams.Select(p => p.Locked).ToList();

            // Lock all for resampling
            for (int i = 0; i < ContributorParams.Count; i++)
                ContributorParams[i] = ContributorParams[i] with { Locked = true };

            ExecuteRun();

            // Restore original lock states
            for (int i = 0; i < ContributorParams.Count && i < originalLocks.Count; i++)
                ContributorParams[i] = ContributorParams[i] with { Locked = originalLocks[i] };
        }

        private void ExecuteViewContributors()
        {
            if (_lastProfiles.Count == 0)
            {
                MessageBox.Show("Run a simulation first.", "View Contributors", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new ContributorProfilesWindow(_lastProfiles, KitConfig.Kits[SelectedKit]);
            window.Show();
        }

        private void ExecuteExportSample()
        {
            if (FinalPeaks.Count == 0)
            {
                MessageBox.Show("Run a simulation first.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string? sampleName = ExportService.BuildSampleName(FileNumber, ExhibitNumber, ItemNumber);
            if (string.IsNullOrEmpty(sampleName))
            {
                MessageBox.Show("Invalid sample naming format.\n\nExpected formats:\n" +
                               "File: 2025A-000001\n" +
                               "Exhibit: 0001-AA-01\n" +
                               "Item: 1000",
                    "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string defaultFileName = $"{FileNumber}_Q.txt";

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = defaultFileName,
                Title = "Save Sample Electropherogram",
                CheckPathExists = true,
                OverwritePrompt = !AppendToFile   // ← Key change: Don't prompt when appending
            };

            if (dialog.ShowDialog() == true)
            {
                bool fileExisted = File.Exists(dialog.FileName);

                try
                {
                    var lociToUse = KitConfig.Kits[SelectedKit];

                    var channels = SelectedKit == "GLOBALFILER"
                    ? KitConfig.GlobalfilerChannels
                    : KitConfig.IdplusChannels;

                    var locusThresholds = channels.Values
                        .SelectMany(c => c.Loci.Select(locus => new { locus, c.Threshold }))
                        .ToDictionary(x => x.locus, x => x.Threshold);

                    ExportService.ExportSample(
                        dialog.FileName,
                        FinalPeaks,
                        sampleName,
                        lociToUse,
                        AppendToFile,
                        locusThresholds);

                    string message = AppendToFile
                        ? (fileExisted ? "Sample successfully appended!" : "Sample exported to new file!")
                        : "Sample exported successfully!";

                    MessageBox.Show($"{message}\n\nFile: {dialog.FileName}\nInternal Sample: {sampleName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
                {
                    MessageBox.Show(ex.Message, "Duplicate Sample Name",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ExecuteExportReference()
        {
            MessageBox.Show("Reference export coming soon.", "Info");
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple RelayCommand
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
    }
}