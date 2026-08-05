using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace NetSonar.Avalonia.Converters;

public sealed class LocalizedFallbackConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || parameter is not string resourceKey) return null;

        var value = values[0];
        return value is null or string { Length: 0 } || ReferenceEquals(value, AvaloniaProperty.UnsetValue)
            ? App.Localization[resourceKey]
            : value;
    }
}
