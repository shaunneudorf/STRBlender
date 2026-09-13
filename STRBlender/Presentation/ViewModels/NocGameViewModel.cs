using STRBlender.Core.Application;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Domain.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace STRBlender.Presentation.ViewModels
{
    public class NocGameViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public List<Peak> FinalPeaks { get; private set; } = new();

        public List<List<Peak>> BaseContributorPeaks { get; private set; } = new();

        public List<string> DatabaseOptions { get; } =
            KitConfig.DatabaseOptions.Keys.ToList();

        public List<int> ContributorOptions { get; } = new() { 1, 2, 3, 4, 5 };

        // === Kit ===
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

        // === Database ===
        private string _selectedDatabase;
        public string SelectedDatabase
        {
            get => _selectedDatabase;
            set { _selectedDatabase = value; OnPropertyChanged(); }
        }

        // === Ranges ===
        private int _minContributors = 1;
        public int MinContributors
        {
            get => _minContributors;
            set { _minContributors = value; OnPropertyChanged(); }
        }

        private int _maxContributors = 3;
        public int MaxContributors
        {
            get => _maxContributors;
            set { _maxContributors = value; OnPropertyChanged(); }
        }

        private double _minDegradation = -0.003;
        public double MinDegradation
        {
            get => _minDegradation;
            set { _minDegradation = value; OnPropertyChanged(); }
        }

        private double _maxDegradation = 0.0;
        public double MaxDegradation
        {
            get => _maxDegradation;
            set { _maxDegradation = value; OnPropertyChanged(); }
        }

        private double _minProportion = 0.1;
        public double MinProportion
        {
            get => _minProportion;
            set { _minProportion = value; OnPropertyChanged(); }
        }

        private double _maxProportion = 1.0;
        public double MaxProportion
        {
            get => _maxProportion;
            set { _maxProportion = value; OnPropertyChanged(); }
        }

        private double _minTemplate = 500;
        public double MinTemplate
        {
            get => _minTemplate;
            set { _minTemplate = value; OnPropertyChanged(); }
        }

        private double _maxTemplate = 1000;
        public double MaxTemplate
        {
            get => _maxTemplate;
            set { _maxTemplate = value; OnPropertyChanged(); }
        }

        // === Game State ===
        private bool _mixtureGenerated = false;
        public bool MixtureGenerated
        {
            get => _mixtureGenerated;
            set { _mixtureGenerated = value; OnPropertyChanged(); }
        }

        private string _resultVerdict = "";
        public string ResultVerdict
        {
            get => _resultVerdict;
            set { _resultVerdict = value; OnPropertyChanged(); }
        }

        private string _resultDetail = "";
        public string ResultDetail
        {
            get => _resultDetail;
            set { _resultDetail = value; OnPropertyChanged(); }
        }

        private Brush _resultColor = new SolidColorBrush(Colors.Black);
        public Brush ResultColor
        {
            get => _resultColor;
            set { _resultColor = value; OnPropertyChanged(); }
        }

        private Visibility _resultVisible = Visibility.Collapsed;
        public Visibility ResultVisible
        {
            get => _resultVisible;
            set { _resultVisible = value; OnPropertyChanged(); }
        }

        // === Score ===
        private int _scoreCorrect;
        public int ScoreCorrect
        {
            get => _scoreCorrect;
            set { _scoreCorrect = value; OnPropertyChanged(); }
        }

        private int _scoreWrong;
        public int ScoreWrong
        {
            get => _scoreWrong;
            set { _scoreWrong = value; OnPropertyChanged(); }
        }

        private int _streak;
        public int Streak
        {
            get => _streak;
            set { _streak = value; OnPropertyChanged(); }
        }

        public NocGameViewModel()
        {
            _selectedDatabase = DatabaseOptions.Count > 0 ? DatabaseOptions[0] : "";
        }

        public void RunSimulation(List<ContributorParams> contributors, string kit, double template)
        {
            var lociToUse = KitConfig.Kits[kit];

            var simParams = new SimulationParams
            {
                Kit = kit,
                Template = template,
                LociToUse = lociToUse,
                ContributorParams = contributors,
                LastProfilesBackup = new List<Profile>()
            };

            var result = SimulationService.GenerateProfiles(simParams);

            FinalPeaks = result.FinalPeaks;
            MixtureProfiles = result.Profiles;
            MixturePeakLists = result.PeakLists;

            // === NEW: Store pre-stochastic base peaks for overlays ===
            BaseContributorPeaks.Clear();
            foreach (var peakList in result.PeakLists)
            {
                BaseContributorPeaks.Add(peakList);   // Use raw BaseHeight peaks
            }
        }
        public List<Profile> MixtureProfiles { get; private set; } = new();
        public List<List<Peak>> MixturePeakLists { get; private set; } = new();
    }
}