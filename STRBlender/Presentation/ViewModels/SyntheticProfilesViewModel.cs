using System.ComponentModel;
using System.Runtime.CompilerServices;
using STRBlender.Core.Application;
using STRBlender.Core.Domain.Common;

namespace STRBlender.Presentation.ViewModels
{
    public class SyntheticProfilesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _selectedKit = "GLOBALFILER";
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

        private int _profileCount = 100;
        public int ProfileCount
        {
            get => _profileCount;
            set { _profileCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(NamingPreview)); }
        }

        private int _startingSeries = 1;
        public int StartingSeries
        {
            get => _startingSeries;
            set { _startingSeries = value; OnPropertyChanged(); OnPropertyChanged(nameof(NamingPreview)); }
        }

        private BatchOutputMode _outputMode = BatchOutputMode.StrmixTxt;
        public BatchOutputMode OutputMode
        {
            get => _outputMode;
            set
            {
                _outputMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsStrmixTxt));
                OnPropertyChanged(nameof(IsMlCsv));
                OnPropertyChanged(nameof(OutputModeDescription));
            }
        }

        public bool IsStrmixTxt
        {
            get => OutputMode == BatchOutputMode.StrmixTxt;
            set { if (value) OutputMode = BatchOutputMode.StrmixTxt; }
        }

        public bool IsMlCsv
        {
            get => OutputMode == BatchOutputMode.MlCsv;
            set { if (value) OutputMode = BatchOutputMode.MlCsv; }
        }

        public string OutputModeDescription =>
            OutputMode == BatchOutputMode.MlCsv
                ? "Writes two CSVs joined by sample_id: *_mixtures.csv (peak features + labels) and *_truth.csv (genotypes + contributor params)."
                : "Writes one STRmix-style sample .txt (Sample Name, Marker, Allele/Size/Height 1–20).";

        private int _progressDone;
        public int ProgressDone
        {
            get => _progressDone;
            set { _progressDone = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
        }

        private int _progressTotal;
        public int ProgressTotal
        {
            get => _progressTotal;
            set { _progressTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
        }

        public string ProgressText =>
            ProgressTotal > 0 ? $"{ProgressDone} / {ProgressTotal}" : "";

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsIdle)); }
        }

        public bool IsIdle => !IsRunning;

        private string _statusMessage = "Ready.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string NamingPreview
        {
            get
            {
                if (ProfileCount < 1) return "";
                string first = BatchSimulationService.BuildSampleName(StartingSeries, 0);
                if (ProfileCount == 1) return first;
                string last = BatchSimulationService.BuildSampleName(StartingSeries, ProfileCount - 1);
                return $"{first}  …  {last}";
            }
        }

        public SyntheticProfilesViewModel()
        {
            LocusDefinitions.SetKit(_selectedKit);
        }
    }
}
