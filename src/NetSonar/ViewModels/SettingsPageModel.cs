using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using NetSonar.Avalonia.Localization;
using SukiUI.Models;

namespace NetSonar.Avalonia.ViewModels;

public sealed class LanguageOption : ObservableObject
{
    private readonly ILocalizationService _localization;

    public LanguageOption(ILocalizationService localization, CultureInfo? culture = null)
    {
        _localization = localization;
        Culture = culture;
        if (culture is null) localization.PropertyChanged += LocalizationOnPropertyChanged;
    }

    public CultureInfo? Culture { get; }
    public string DisplayName => Culture?.NativeName ?? _localization["Settings.Language.FollowSystem"];

    private void LocalizationOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILocalizationService.Culture)) OnPropertyChanged(nameof(DisplayName));
    }
}

public partial class SettingsPageModel : PageViewModelBase
{
    public SettingsPageModel(ILocalizationService localization)
    {
        Localization = localization;

        var followSystem = new LanguageOption(localization);
        var languageOptions = new List<LanguageOption>(localization.SupportedCultures.Count + 1)
        {
            followSystem
        };
        foreach (var culture in localization.SupportedCultures)
        {
            languageOptions.Add(new LanguageOption(localization, culture));
        }

        LanguageOptions = languageOptions;

        var selectedLanguage = followSystem;
        if (!string.IsNullOrWhiteSpace(AppSettings.Language))
        {
            foreach (var option in languageOptions)
            {
                if (string.Equals(option.Culture?.Name, AppSettings.Language,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    selectedLanguage = option;
                    break;
                }
            }
        }

        SelectedLanguage = selectedLanguage;

        IsVisibleOnSideMenu = false;

        IsSystemTheme = AppSettings.Theme == ApplicationTheme.Default;
        IsLightTheme = AppSettings.Theme == ApplicationTheme.Light;
        IsDarkTheme = AppSettings.Theme == ApplicationTheme.Dark;
    }

    public ILocalizationService Localization { get; }

    public override int Index => -1;
    public override string DisplayName => Localization["Navigation.Settings"];
    public override MaterialIconKind Icon => MaterialIconKind.Cog;
    public override bool AutoHideOnSideMenu => true;

    [ObservableProperty] public partial bool IsSystemTheme { get; set; }

    [ObservableProperty] public partial bool IsLightTheme { get; set; }

    [ObservableProperty] public partial bool IsDarkTheme { get; set; }

    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

    [ObservableProperty] public partial LanguageOption? SelectedLanguage { get; set; }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value is not null) App.ChangeLanguage(value.Culture?.Name);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsSystemTheme))
        {
            if (IsSystemTheme) App.ChangeBaseTheme(ApplicationTheme.Default);
        }
        else if (e.PropertyName == nameof(IsLightTheme))
        {
            if (IsLightTheme) App.ChangeBaseTheme(ApplicationTheme.Light);
        }
        else if (e.PropertyName == nameof(IsDarkTheme))
        {
            if (IsDarkTheme) App.ChangeBaseTheme(ApplicationTheme.Dark);
        }
    }

    [RelayCommand]
    public void SwitchToColorTheme(SukiColorTheme color)
    {
        App.ChangeColorTheme(color);
    }
}