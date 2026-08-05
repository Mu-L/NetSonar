using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NetSonar.Avalonia.Converters;

public sealed class LocalizedStringFormatConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || parameter is not string resourceKey) return null;
        return App.Localization.Format(resourceKey, values[0]);
    }
}
