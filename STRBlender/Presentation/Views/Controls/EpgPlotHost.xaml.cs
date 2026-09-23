using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Presentation.Plotting;

namespace STRBlender.Presentation.Views.Controls
{
    public partial class EpgPlotHost : UserControl
    {
        public EpgPlotHost() => InitializeComponent();

        public void DrawMixture(List<Peak> peaks, string kit)
        {
            if (peaks?.Count == 0) return;

            var kitDef = KitRegistry.Get(kit);
            var channels = kitDef.Channels;

            EpgPlotter.DrawChannel(plot_Blue, peaks, channels["Blue"].Loci, channels["Blue"].Color, channels["Blue"].Threshold, kitDef);
            EpgPlotter.DrawChannel(plot_Green, peaks, channels["Green"].Loci, channels["Green"].Color, channels["Green"].Threshold, kitDef);
            EpgPlotter.DrawChannel(plot_Yellow, peaks, channels["Yellow"].Loci, channels["Yellow"].Color, channels["Yellow"].Threshold, kitDef);
            EpgPlotter.DrawChannel(plot_Red, peaks, channels["Red"].Loci, channels["Red"].Color, channels["Red"].Threshold, kitDef);

            if (kit == "GLOBALFILER" && channels.ContainsKey("Purple"))
            {
                plot_Purple.Visibility = Visibility.Visible;
                plot_Purple.UpdateLayout();   // force layout now so ActualWidth/Height are valid before DrawChannel reads them
                EpgPlotter.DrawChannel(plot_Purple, peaks, channels["Purple"].Loci, channels["Purple"].Color, channels["Purple"].Threshold, kitDef);
            }
            else
            {
                plot_Purple.Visibility = Visibility.Collapsed;
            }
        }

        public void Clear()
        {
            foreach (var c in new[] { plot_Blue, plot_Green, plot_Yellow, plot_Red, plot_Purple })
                c?.Children.Clear();
        }

        // Exposed for NOC overlays
        public Canvas BlueCanvas => plot_Blue;
        public Canvas GreenCanvas => plot_Green;
        public Canvas YellowCanvas => plot_Yellow;
        public Canvas RedCanvas => plot_Red;
        public Canvas PurpleCanvas => plot_Purple;
    }
}