using System;
using System.ComponentModel;
using System.Globalization;
using NetSonar.Avalonia.Localization;

namespace NetSonar.Avalonia;

public partial class App
{
    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;

    private static void SetupLocalization()
    {
        var configuredCulture = GetSupportedCulture(AppSettings.Language);
        Localization.Culture = configuredCulture;
        Localization.PropertyChanged += LocalizationOnPropertyChanged;
    }

    public static void ChangeLanguage(string? cultureName)
    {
        AppSettings.Language = cultureName ?? string.Empty;
        Localization.Culture = GetSupportedCulture(AppSettings.Language);
    }

    private static CultureInfo GetSupportedCulture(string? cultureName)
    {
        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            try
            {
                return GetSupportedCulture(CultureInfo.GetCultureInfo(cultureName));
            }
            catch (CultureNotFoundException)
            {
                // Ignore an invalid persisted culture and recover using the system UI culture.
            }
        }

        return GetSupportedCulture(SystemUiCulture);
    }

    private static CultureInfo GetSupportedCulture(CultureInfo requestedCulture)
    {
        foreach (var culture in Localization.SupportedCultures)
        {
            if (string.Equals(culture.Name, requestedCulture.Name, StringComparison.OrdinalIgnoreCase)) return culture;
        }

        foreach (var culture in Localization.SupportedCultures)
        {
            if (string.Equals(
                    culture.TwoLetterISOLanguageName,
                    requestedCulture.TwoLetterISOLanguageName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return culture;
            }
        }

        return CultureInfo.GetCultureInfo(LocalizationService.FallbackCultureName);
    }

    // Map only locales bundled by SukiUI; unsupported languages use its English resources.
    private static string GetSukiLocale(CultureInfo culture) => culture.Name switch
    {
        "de" => "de_DE",
        "es" => "es_ES",
        "fr" => "fr_FR",
        "it" => "it_IT",
        "ja" => "ja_JP",
        "nl-NL" => "nl_NL",
        "pt-BR" => "pt_PT",
        "pt-PT" => "pt_PT",
        "ru" => "ru_RU",
        "zh-Hans" => "zh_CN",
        _ => "en_US"
    };

    private static void LocalizationOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILocalizationService.Culture))
        {
            if (Theme is not null) Theme.Locale = GetSukiLocale(Localization.Culture);
        }
    }
}
