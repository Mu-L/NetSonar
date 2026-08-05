using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetSonar.Avalonia.SystemOS;
using NetSonar.Avalonia.ViewModels.Dialogs;
using StageKit;
using SukiUI.Dialogs;
using ZLinq;

namespace NetSonar.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public IReadOnlyList<PageViewModelBase> Pages { get; init; }

    [ObservableProperty]
    public partial PageViewModelBase ActivePage { get; set; } = null!;

    public MainViewModel(IEnumerable<PageViewModelBase> pages)
    {
        Pages = [.. pages];
        if (Pages.Count > 0) ActivePage = Pages[0];
    }

    [RelayCommand]
    public void ShowSettingsCommand()
    {
        var settings = Pages
            .AsValueEnumerable()
            .FirstOrDefault(page => page is SettingsPageModel);
        if (settings is null) return;
        ActivePage = settings;
    }

    [RelayCommand]
    public static void ShowAboutDialog()
    {
        var dialog = DialogManager
            .CreateDialog()
            .WithViewModel(dialog => new AboutDialogModel(dialog));
        dialog.TryShow();
    }

    [RelayCommand]
    public static void OpenProfileFolder()
    {
        SystemAware.StartProcess(ApplicationKit.ProfilePath);
    }

    [RelayCommand]
    public static Task CheckForUpdatesAsync()
    {
        return App.CheckForUpdatesAsync();
    }

    [RelayCommand]
    public static void OpenExecutableFolder()
    {
        SystemAware.StartProcess(AppContext.BaseDirectory);
    }

    [RelayCommand]
    public static async Task TriggerNewUpdate()
    {
        if (App.AppUpdater.Releases.Count == 0)
        {
            await App.AppUpdater.CheckForUpdatesAsync();
        }

        if (App.AppUpdater.Releases.Count == 0)
        {
            App.ShowToast(App.Localization["Update.TriggerUnavailable.Title"],
                App.Localization["Update.TriggerUnavailable.Message"]);
            return;
        }
        var release = App.AppUpdater.Releases[0];
        App.AppUpdater.ForceTriggerUpdateFromRelease(release);
    }


    [RelayCommand]
    public static void ThrowException()
    {
        int i = 50;
        int zero = 0;

        // ReSharper disable once IntDivisionByZero
        _ = i / zero;
    }
}
