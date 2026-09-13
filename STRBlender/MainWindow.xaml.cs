using System.Windows;
using STRBlender.Presentation.Views;

namespace STRBlender
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new EpgView();  // default view
        }

        private void Nav_Epg_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new EpgView();

        private void Nav_Noc_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new NocGameView();

        private void Nav_Relatives_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new RelativesView();

        private void Nav_Synthetic_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new ProfileGenView();
    }
}