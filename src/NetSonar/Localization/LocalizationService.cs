using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace NetSonar.Avalonia.Localization;

/// <summary>
/// Resolves localized strings from the application's satellite resource assemblies.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    public const string FallbackCultureName = "en";

    private const string ResourceBaseName = "NetSonar.Avalonia.Localization.Strings";

    private static readonly CultureInfo FallbackCulture = CultureInfo.GetCultureInfo(FallbackCultureName);

    // The language selector preserves this CultureInfo.Name order.
    private static readonly IReadOnlyList<CultureInfo> Cultures = Array.AsReadOnly([
        CultureInfo.GetCultureInfo("de"),
        FallbackCulture, // en
        CultureInfo.GetCultureInfo("es"),
        CultureInfo.GetCultureInfo("fr"),
        CultureInfo.GetCultureInfo("it"),
        CultureInfo.GetCultureInfo("ja"),
        CultureInfo.GetCultureInfo("ko"),
        CultureInfo.GetCultureInfo("nl-NL"),
        CultureInfo.GetCultureInfo("pl"),
        CultureInfo.GetCultureInfo("pt-BR"),
        CultureInfo.GetCultureInfo("pt-PT"),
        CultureInfo.GetCultureInfo("ru"),
        CultureInfo.GetCultureInfo("tr"),
        CultureInfo.GetCultureInfo("zh-Hans"),
    ]);

    private readonly ResourceManager _resourceManager = new(ResourceBaseName, typeof(LocalizationService).Assembly);

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => GetString(key);

    public CultureInfo Culture
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (string.Equals(field.Name, value.Name, StringComparison.OrdinalIgnoreCase)) return;

            field = value;
            CultureInfo.CurrentUICulture = value;
            CultureInfo.DefaultThreadCurrentUICulture = value;

            OnPropertyChanged();
            OnPropertyChanged("Item[]");
        }
    } = CultureInfo.CurrentUICulture;

    public IReadOnlyList<CultureInfo> SupportedCultures => Cultures;

    public string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var localizedValue = _resourceManager.GetString(key, Culture);
        if (!string.IsNullOrWhiteSpace(localizedValue)) return localizedValue;

        var fallbackValue = _resourceManager.GetString(key, FallbackCulture);
        return string.IsNullOrWhiteSpace(fallbackValue) ? key : fallbackValue;
    }

    public string Format(string key, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return string.Format(CultureInfo.CurrentCulture, GetString(key), arguments);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
