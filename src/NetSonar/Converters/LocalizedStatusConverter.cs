using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using NetSonar.Avalonia.Localization;

namespace NetSonar.Avalonia.Converters;

public sealed class LocalizedStatusConverter : IMultiValueConverter, IValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.Count == 0 || ReferenceEquals(values[0], AvaloniaProperty.UnsetValue)
            ? App.Localization["Status.Unknown"]
            : StatusLocalization.GetText(values[0]);
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ReferenceEquals(value, AvaloniaProperty.UnsetValue)
            ? App.Localization["Status.Unknown"]
            : StatusLocalization.GetText(value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
