using Avalonia.Data.Converters;
using Avalonia.Media;
using gptLogApp.Model;
using System;
using System.Globalization;

namespace gptLogApp.Converters
{
    public class RoleToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Role role)
            {
                return role switch
                {
                    Role.User => new SolidColorBrush(Color.Parse("#0066ff")),      // Blue
                    Role.Assistant => new SolidColorBrush(Color.Parse("#cc0000")), // Red
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}