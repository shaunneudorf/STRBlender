using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Presentation.Views
{
    public partial class ContributorProfilesWindow : Window
    {
        public ContributorProfilesWindow(List<Profile> profiles, List<string> loci)
        {
            InitializeComponent();
            BuildTabs(profiles, loci);
        }

        private void BuildTabs(List<Profile> profiles, List<string> loci)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                var dt = new DataTable();
                dt.Columns.Add("Locus");
                dt.Columns.Add("Allele 1");
                dt.Columns.Add("Allele 2");

                foreach (var locus in loci)
                {
                    var row = dt.NewRow();
                    row["Locus"] = locus;
                    if (profiles[i].Loci.TryGetValue(locus, out var alleles))
                    {
                        row["Allele 1"] = alleles.A1;
                        row["Allele 2"] = alleles.A2;
                    }
                    else
                    {
                        row["Allele 1"] = "-";
                        row["Allele 2"] = "-";
                    }
                    dt.Rows.Add(row);
                }

                var grid = new DataGrid
                {
                    AutoGenerateColumns = true,
                    IsReadOnly = true,
                    ItemsSource = dt.DefaultView,
                    AlternatingRowBackground = Brushes.WhiteSmoke
                };

                var tab = new TabItem
                {
                    Header = $"Contributor {i + 1}",
                    Content = grid
                };

                tabControl_Contributors.Items.Add(tab);
            }
        }
    }
}