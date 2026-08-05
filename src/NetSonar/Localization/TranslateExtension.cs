using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace NetSonar.Avalonia.Localization;

/// <summary>
/// Creates a one-way binding to a localized resource key.
/// </summary>
public sealed class TranslateExtension(string key) : MarkupExtension
{
    private static readonly IValueConverter Converter = new TranslationConverter();

    public string Key { get; } = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding(nameof(ILocalizationService.Culture))
        {
            Converter = Converter,
            ConverterParameter = Key,
            Mode = BindingMode.OneWay,
            Source = App.Localization
        };
    }

    private sealed class TranslationConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return parameter is string resourceKey
                ? App.Localization.GetString(resourceKey)
                : null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
