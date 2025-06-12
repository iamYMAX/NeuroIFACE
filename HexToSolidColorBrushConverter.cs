using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NeuroIFACE
{
    public class HexToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor)
            {
                try
                {
                    // Ensure the hex string is valid for BrushConverter.
                    // It should start with '#' and be ARGB (e.g., #AARRGGBB) or RGB (e.g., #RRGGBB).
                    // SolidColorBrush expects ARGB. If RGB, assume full opacity.
                    if (hexColor.StartsWith("#") && (hexColor.Length == 7 || hexColor.Length == 9))
                    {
                         return (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
                    }
                }
                catch (FormatException)
                {
                    // Handle invalid format if necessary, or return a default brush
                    return Brushes.Transparent; // Or some other default
                }
                catch (Exception)
                {
                    // Other errors with BrushConverter
                    return Brushes.Transparent;
                }
            }
            // Return a default or transparent brush if input is not a valid string
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter is one-way, so ConvertBack is not implemented.
            throw new NotImplementedException();
        }
    }
}
