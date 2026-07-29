using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NetSonar.Avalonia.ViewModels;
using NetSonar.Avalonia.Views;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using NetSonar.Avalonia.Common;
using NetSonar.Avalonia.ViewModels.Dialogs;
using NetSonar.Avalonia.Views.Dialogs;
using ZLogger;
using System.Globalization;
using StageKit;
using Updatum;
using ZLinq;

namespace NetSonar.Avalonia;

public partial class App : Application
{
    /// <summary>
    /// Mutex to prevent multiple instances of the application from running.
    /// </summary>
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private static ApplicationInstanceGuard? _appInstanceGuard;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    //private static readonly Mutex AppMutex = new(true, $"Mutex_{Environment.UserDomainName}_{Environment.UserName}_{EntryApplication.AssemblyName}_{{8AEA6BAE-D5D5-49FA-8A8E-479FFC369D5D}}");

    public const bool IsDebug =
#if DEBUG
            true
#else
            false
#endif
        ;

    /// <summary>
    /// Flag to determine if the application was launched minimized to tray.
    /// </summary>
    public static bool StartMinimized { get; private set; }

    /// <summary>
    /// Main window of the application.
    /// </summary>
    public static TopLevel TopLevel => MainWindow;

    public static Window MainWindow { get; private set; } = null!;

    public static readonly SukiDialogManager DialogManager = new();
    public static readonly SukiToastManager ToastManager = new();

    public static AppViewModel AppViewModel { get; private set; } = null!;

    public App()
    {
        CultureInfo.DefaultThreadCurrentUICulture =
            CultureInfo.DefaultThreadCurrentCulture =
                OptimalCultureInfo;

        ApplicationKit.JsonSerializerOptions = JsonSerializerOptions;
        ApplicationKit.Birth = DateTime.SpecifyKind(new(2025, 7, 1, 2, 00, 00), DateTimeKind.Utc);
        ApplicationKit.UiFrameworkInfo = $"Avalonia {typeof(AvaloniaObject).Assembly.GetName().Version?.ToString(3)}";
        ApplicationKit.ConfigsDirectoryName = "settings";
        ApplicationKit.ParseProfilePathFromArgs();

        UnhandledExceptions.RegisterAppDomainUnhandledException();
        UnhandledExceptions.RegisterTaskSchedulerUnobservedTaskException();
        UnhandledExceptions.IgnoreAvaloniaSafeExceptions();
        UnhandledExceptions.BeforeForcedExit += (sender, args) => PanicSaveSettings();

        CrashReportsFile.IsEnabled = true;
        CrashReportsFile.CrashReportsFileName = "crash_reports.json";
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AppViewModel = new AppViewModel();
        DataContext = AppViewModel;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetupLogger();
        SetupTheme();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopCheck)
        {
            if (desktopCheck.Args?.Length > 0)
            {
                ApplicationKit.ApplicationArgs = desktopCheck.Args;
            }
            if (ApplicationKit.ApplicationArgs?.Length >= 1)
            {
                if (Array.IndexOf(ApplicationKit.ApplicationArgs, "--minimized") >= 0)
                {
                    StartMinimized = true;
                }
            }
        }

        if (ApplicationKit.HasCrashReportFlag && ApplicationKit.CrashReportIndex > 0)
        {
            var crashReport = ApplicationKit.CrashReport;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow = new GenericWindow
                {
                    Title = $"{SoftwareWithVersion} - Crash Report",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    CanResize = false,
                    CanMaximize = false,
                    MaxHeightScreenRatio = 0.75,
                    MaxWidthScreenRatio = 0.75,
                    Content = new CrashReportDialogView
                    {
                        DataContext = new CrashReportDialogModel(crashReport)
                    }
                };
                desktop.MainWindow = MainWindow;
            }
            else
            {
                Logger.ZLogCritical($"{crashReport.FormattedMessage ?? "The application crashed due an unexpected exception. (Unable to present the information in the UI"}.");
                Environment.Exit(0);
            }
        }
        else
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            //BindingPlugins.DataValidators.RemoveAt(0);
#if DEBUG
            if(true)
#else
            _appInstanceGuard = ApplicationInstanceGuard.AcquirePerUser();
            if (Design.IsDesignMode || _appInstanceGuard.IsPrimary)
#endif
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    /*MainWindow = new MainWindow
                    {
                        DataContext = MainViewModel
                    };
                    desktop.MainWindow = MainWindow;
                    desktop.Exit += DesktopOnExit;*/

                    Services.AddSingleton(desktop);
                    SetupViews();

                    SystemOS.Autostart.RefreshIfEnabled();

                    AppViewModel.PingsPage = ServicesProvider
                        .GetServices<PageViewModelBase>()
                        .AsValueEnumerable()
                        .OfType<PingableServicesPageModel>()
                        .FirstOrDefault();

                    DataTemplates.Add(new ViewLocator(Views));

                    MainWindow = (Views.CreateView<MainViewModel>(ServicesProvider) as Window)!;
                    if (StartMinimized && AppSettings.IsTrayVisible)
                    {
                        MainWindow.WindowState = WindowState.Minimized;
                        MainWindow.ShowInTaskbar = false;
                        MainWindow.Opened += MainWindowOnOpenedOnAutoStartup;
                    }
                    desktop.MainWindow = MainWindow;
                    desktop.Exit += DesktopOnExit;
                    AppUpdater.UpdateFound += AppUpdaterOnUpdateFound;
                }
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
                {
                    /*singleViewPlatform.MainView = new MainView
                    {
                        DataContext = MainViewModel
                    };*/
                }
            }
            else
            {
#pragma warning disable CS0162 // Unreachable code detected
                _appInstanceGuard?.Dispose();

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    MainWindow = new GenericWindow()
                    {
                        Title = $"{SoftwareWithVersion} - Already running",
                        SizeToContent = SizeToContent.WidthAndHeight,
                        CanResize = false,
                        CanMaximize = false,
                        MaxHeightScreenRatio = 0.75,
                        MaxWidthScreenRatio = 0.75,
                        Topmost = true,
                        Content = new InstanceAlreadyRunningDialogView
                        {
                            DataContext = new InstanceAlreadyRunningDialogModel()
                        }
                    };
                    desktop.MainWindow = MainWindow;
                }
                else
                {
                    Logger.ZLogInformation($"""
                                There is another instance of {Software} running. Only one instance of {Software} can run at a time.
                                Please find and open the running instance or close it before starting a new one. (Unable to present this information in the UI).
                                """);
                    Environment.Exit(0);
                }
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void MainWindowOnOpenedOnAutoStartup(object? sender, EventArgs e)
    {
        MainWindow.Hide();
        MainWindow.Opened -= MainWindowOnOpenedOnAutoStartup;
    }

    private void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        PanicSaveSettings();
        //AppMutex.ReleaseMutex();
        _appInstanceGuard?.Dispose();
    }
}
