using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace CoverLetterGenerator.Converters;

public class IntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // safely handle null
        if (value is int count)
            return count > 0;

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Not intended for two-way binding
        throw new NotSupportedException();
    }
}
