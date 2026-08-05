using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NetSonar.Avalonia.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    public AppViewModel()
    {
        App.Localization.PropertyChanged += LocalizationOnPropertyChanged;
    }

    [ObservableProperty]
    public partial PingableServicesPageModel? PingsPage { get; set; }

    public int PingsTotalCount => PingsPage is null ? 0 : PingableServicesPageModel.ServicesCount;
    public int PingsUpCount => PingsPage?.ServicesSucceededCount ?? 0;
    public int PingsDownCount => PingsPage?.ServicesFailedCount ?? 0;
    public string PingsSummary => PingsTotalCount == 0
        ? string.Empty
        : $"🟢 {App.Localization.Format("Tray.PingsUp", PingsUpCount)}  ·  🔴 {App.Localization.Format("Tray.PingsDown", PingsDownCount)}";
    public string TrayShow => App.Localization["Ui.Show"];
    public string TrayOptions => App.Localization["Ui.Options"];
    public string TrayStartWithSystem => App.Localization["Ui.StartWithSystem"];
    public string TrayThemeSystem => App.Localization["Ui.ThemeSystem"];
    public string TrayThemeLight => App.Localization["Ui.ThemeLight"];
    public string TrayThemeDark => App.Localization["Ui.ThemeDark"];
    public string TrayExit => App.Localization["Ui.Exit"];

    partial void OnPingsPageChanged(PingableServicesPageModel? oldValue, PingableServicesPageModel? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= OnPingsPagePropertyChanged;
        if (newValue is not null) newValue.PropertyChanged += OnPingsPagePropertyChanged;

        OnPropertyChanged(nameof(PingsTotalCount));
        OnPropertyChanged(nameof(PingsUpCount));
        OnPropertyChanged(nameof(PingsDownCount));
        OnPropertyChanged(nameof(PingsSummary));
    }

    private void OnPingsPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PingableServicesPageModel.ServicesCount):
            case nameof(PingableServicesPageModel.ServicesSucceededCount):
            case nameof(PingableServicesPageModel.ServicesFailedCount):
                OnPropertyChanged(nameof(PingsTotalCount));
                OnPropertyChanged(nameof(PingsUpCount));
                OnPropertyChanged(nameof(PingsDownCount));
                OnPropertyChanged(nameof(PingsSummary));
                break;
        }
    }

    private void LocalizationOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(App.Localization.Culture)) return;

        OnPropertyChanged(nameof(PingsSummary));
        OnPropertyChanged(nameof(TrayShow));
        OnPropertyChanged(nameof(TrayOptions));
        OnPropertyChanged(nameof(TrayStartWithSystem));
        OnPropertyChanged(nameof(TrayThemeSystem));
        OnPropertyChanged(nameof(TrayThemeLight));
        OnPropertyChanged(nameof(TrayThemeDark));
        OnPropertyChanged(nameof(TrayExit));
    }

    [RelayCommand]
    public void SetThemeSystem()
    {
        App.ChangeBaseTheme(ApplicationTheme.Default);
    }

    [RelayCommand]
    public void SetThemeLight()
    {
        App.ChangeBaseTheme(ApplicationTheme.Light);
    }

    [RelayCommand]
    public void SetThemeDark()
    {
        App.ChangeBaseTheme(ApplicationTheme.Dark);
    }

    [RelayCommand]
    public void ToggleApplicationVisibility()
    {
        if (App.MainWindow.ShowInTaskbar)
        {
            HideApplication();
        }
        else
        {
            ShowApplication();
        }
    }

    [RelayCommand]
    public void ShowApplication()
    {
        App.MainWindow.WindowState = AppSettings.LastWindowState;
        App.MainWindow.ShowInTaskbar = true;
        App.MainWindow.Show();
    }

    [RelayCommand]
    public void HideApplication()
    {
        App.MainWindow.WindowState = WindowState.Minimized;
        App.MainWindow.ShowInTaskbar = false;
        App.MainWindow.Hide();
    }

    [RelayCommand]
    public void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
