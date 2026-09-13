using System;
using System.Globalization;
using System.Windows.Data;

namespace STRBlender.Presentation.Views.Controls
{
    /// Returns true only when the bound Relationship is "Unrelated". Used to
    /// disable the Database dropdown for related contributors, since their
    /// profile is always drawn from C1's frequency table regardless of what
    /// this control shows.
    public class RelationshipIsUnrelatedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) == "Unrelated";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
