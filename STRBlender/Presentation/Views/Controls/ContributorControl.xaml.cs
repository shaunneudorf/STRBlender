using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Presentation.Views.Controls
{
    public partial class ContributorControl : UserControl
    {
        public ContributorControl()
        {
            InitializeComponent();
            RelationshipOptions = new List<string>
            {
                "Unrelated",
                "Partner",
                "Mother",
                "Father",
                "MaternalGrandmother",
                "MaternalGrandfather",
                "PaternalGrandmother",
                "PaternalGrandfather",
                "Sibling",
                "Child",
                "MaternalAuntUncle",
                "PaternalAuntUncle",
                "MaternalCousin",
                "PaternalCousin"
            };
        }

        public ContributorControl(int number, ContributorParams? saved, List<string> databaseOptions)
            : this()
        {
            Load(number, saved, databaseOptions);
        }

        public ContributorParams GetParams() => ToParams();

        public SexType Sex
        {
            get => (SexType)GetValue(SexProperty);
            set => SetValue(SexProperty, value);
        }

        public static readonly DependencyProperty SexProperty =
            DependencyProperty.Register(
                nameof(Sex),
                typeof(SexType),
                typeof(ContributorControl),
                new PropertyMetadata(SexType.Random));

        public void Load(int number, ContributorParams? saved, List<string> databaseOptions)
        {
            HeaderText = $"Contributor {number}";
            DatabaseOptions = databaseOptions;
            IsRelationshipEnabled = number > 1;

            // Unique group name per contributor
            string groupName = $"SexGroup_{number}";
            rb_Random.GroupName = groupName;
            rb_Male.GroupName = groupName;
            rb_Female.GroupName = groupName;

            Proportion = saved?.Proportion ?? 1.0;
            Degradation = saved?.Degradation ?? -0.0015;
            Database = saved?.Database ?? databaseOptions[0];
            Locked = saved?.Locked ?? false;
            Relationship = saved?.Relationship ?? "Unrelated";
            Sex = saved?.Sex ?? SexType.Random;
        }

        public ContributorParams ToParams() =>
            new(Proportion, Degradation, Database, Relationship, Locked, Sex);

        // -------------------------
        // Dependency Properties
        // -------------------------

        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            set => SetValue(HeaderTextProperty, value);
        }
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(ContributorControl));

        public double Proportion
        {
            get => (double)GetValue(ProportionProperty);
            set => SetValue(ProportionProperty, value);
        }
        public static readonly DependencyProperty ProportionProperty =
            DependencyProperty.Register(nameof(Proportion), typeof(double), typeof(ContributorControl));

        public double Degradation
        {
            get => (double)GetValue(DegradationProperty);
            set => SetValue(DegradationProperty, value);
        }
        public static readonly DependencyProperty DegradationProperty =
            DependencyProperty.Register(nameof(Degradation), typeof(double), typeof(ContributorControl));

        public List<string> DatabaseOptions
        {
            get => (List<string>)GetValue(DatabaseOptionsProperty);
            set => SetValue(DatabaseOptionsProperty, value);
        }
        public static readonly DependencyProperty DatabaseOptionsProperty =
            DependencyProperty.Register(nameof(DatabaseOptions), typeof(List<string>), typeof(ContributorControl));

        public string Database
        {
            get => (string)GetValue(DatabaseProperty);
            set => SetValue(DatabaseProperty, value);
        }
        public static readonly DependencyProperty DatabaseProperty =
            DependencyProperty.Register(nameof(Database), typeof(string), typeof(ContributorControl));

        public List<string> RelationshipOptions
        {
            get => (List<string>)GetValue(RelationshipOptionsProperty);
            set => SetValue(RelationshipOptionsProperty, value);
        }
        public static readonly DependencyProperty RelationshipOptionsProperty =
            DependencyProperty.Register(nameof(RelationshipOptions), typeof(List<string>), typeof(ContributorControl));

        public string Relationship
        {
            get => (string)GetValue(RelationshipProperty);
            set => SetValue(RelationshipProperty, value);
        }
        public static readonly DependencyProperty RelationshipProperty =
            DependencyProperty.Register(
                nameof(Relationship),
                typeof(string),
                typeof(ContributorControl),
                new PropertyMetadata(OnRelationshipChanged));

        private static void OnRelationshipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContributorControl control && (string)e.NewValue != "Unrelated")
            {
                control.Locked = false;
            }
        }

        public bool IsRelationshipEnabled
        {
            get => (bool)GetValue(IsRelationshipEnabledProperty);
            set => SetValue(IsRelationshipEnabledProperty, value);
        }
        public static readonly DependencyProperty IsRelationshipEnabledProperty =
            DependencyProperty.Register(nameof(IsRelationshipEnabled), typeof(bool), typeof(ContributorControl));

        public bool Locked
        {
            get => (bool)GetValue(LockedProperty);
            set => SetValue(LockedProperty, value);
        }
        public static readonly DependencyProperty LockedProperty =
            DependencyProperty.Register(nameof(Locked), typeof(bool), typeof(ContributorControl));
    }
}

