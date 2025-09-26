using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace CoverLetterGenerator.Converters;

public class StringEqualityConverter : IValueConverter
{
    public static readonly StringEqualityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value?.ToString() == parameter?.ToString();
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // If the radio button is checked, return the parameter in question.
        return (value is bool b && b) ? parameter?.ToString() : BindingOperations.DoNothing;
    }


}