using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using NetSonar.Avalonia.Controls;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.Network;
using NetSonar.Avalonia.Settings;
using NetSonar.Avalonia.SystemOS;
using ObservableCollections;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NetSonar.Avalonia.ViewModels;

public partial class SpeedTestPageModel : PageViewModelBase
{
    public override int Index => 2;
    public override string DisplayName => App.Localization["Navigation.SpeedTest"];
    public override MaterialIconKind Icon => MaterialIconKind.SpeedometerMedium;

    private readonly DispatcherTimer _timer = new();

    private CancellationTokenSource? _cancellationTokenSource;

    public static ObservableList<SpeedTestResult> Results => SpeedTestsFile.Instance.Items;

    public NotifyCollectionChangedSynchronizedViewList<SpeedTestResult> ResultsView { get; }

    [ObservableProperty]
    public partial SpeedTestResult? SelectedResult { get; set; }

    [ObservableProperty]
    public partial SpeedTestResult? DisplayResult { get; set; } = new();


    [ObservableProperty]
    public partial bool IsExecutableAvailable { get; private set; }

    [ObservableProperty]
    public partial bool IsExecutableInstalling { get; private set; }

    public bool CanExecutableAutoInstall
    {
        get
        {
            if (OperatingSystem.IsMacOS())
            {
                return SystemAware.TryFindEnvFile("brew", out _);
            }
            if (OperatingSystem.IsWindows())
            {
                return SystemAware.TryFindEnvFile("winget.exe", out _);
            }

            if (OperatingSystem.IsLinux())
            {
                return LinuxOS.PackageManager != LinuxOS.LinuxPackageManager.Unknown;
            }

            return true;
        }
    }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial ObservableList<SpeedTestResultServer?> Servers { get; private set; } = [];

    [ObservableProperty]
    public partial SpeedTestResultServer? SelectedServer { get; set; }

    [ObservableProperty]
    public partial string? SpeedTestVersion { get; private set; }

    [ObservableProperty]
    public partial int AngularMeterMaxValue { get; set; } = AppSettings.SpeedTest.InitialSpeedGaugeRange;

    [ObservableProperty]
    public partial int AngularMeterSlowSpeedSeries { get; set; } = 100;

    [ObservableProperty]
    public partial int AngularMeterMediumSpeedSeries { get; set; } = 100;

    [ObservableProperty]
    public partial int AngularMeterFastSpeedSeries { get; set; } = 100;



    private DataGrid _speedTestDataGrid = null!;

    public SpeedTestPageModel()
    {
        ResultsView = Results.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current);

        AngularMeterSlowSpeedSeries = AppSettings.SpeedTest.InitialSpeedGaugeRange / 4;
        AngularMeterMediumSpeedSeries = AppSettings.SpeedTest.InitialSpeedGaugeRange / 4;
        AngularMeterFastSpeedSeries = AppSettings.SpeedTest.InitialSpeedGaugeRange - AngularMeterSlowSpeedSeries - AngularMeterMediumSpeedSeries;

        _timer.Tick += TimerOnTick;
        UpdateAutoSpeedTestTimer();

        AppSettings.SpeedTest.PropertyChanged += SpeedTestOnPropertyChanged;

        if (Design.IsDesignMode)
        {
            IsExecutableAvailable = true;
        }
    }

    protected internal override void OnInitialized()
    {
        base.OnInitialized();
        CheckSpeedTestAvailable();
        if (IsExecutableAvailable)
        {
            SpeedTestVersion = SpeedTestService.GetSpeedTestVersion().GetAwaiter().GetResult();
            _ = UpdateServerList();
        }

        _speedTestDataGrid.KeyUp += SpeedTestDataGridOnKeyUp;
    }

    private void SpeedTestDataGridOnKeyUp(object? sender, KeyEventArgs e)
    {

        if (e.KeyModifiers == KeyModifiers.Shift)
        {
            if (e.Key == Key.Delete)
            {
                RemoveSelectedResults();
                e.Handled = true;
                return;
            }
            return;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedResult))
        {
            DisplayResult = SelectedResult;
        }
        else if (e.PropertyName == nameof(DisplayResult))
        {
            if (DisplayResult is null)
            {
                AngularMeterMaxValue = AppSettings.SpeedTest.InitialSpeedGaugeRange;
                return;
            }
            var maxSpeed = Math.Max(DisplayResult.Download.BandwidthMbps, DisplayResult.Upload.BandwidthMbps);
            if (maxSpeed >= AngularMeterMaxValue)
            {
                var increment = AppSettings.SpeedTest.SpeedGaugeRangeIncrement;
                AngularMeterMaxValue = (int)(Math.Ceiling(maxSpeed / increment) * increment);
            }
        }
    }

    private void SpeedTestOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.SpeedTest.AutoSpeedTest))
        {
            UpdateAutoSpeedTestTimer();
        }
        else if (e.PropertyName == nameof(AppSettings.SpeedTest.AutoSpeedTestIntervalSeconds))
        {
            UpdateAutoSpeedTestTimer();
        }
    }

    private void UpdateAutoSpeedTestTimer()
    {
        var intervalSeconds = AppSettings.SpeedTest.AutoSpeedTestIntervalSeconds;
        if (intervalSeconds <= 0)
        {
            _timer.IsEnabled = false;
            return;
        }

        _timer.Interval = TimeSpan.FromSeconds(intervalSeconds);
        _timer.IsEnabled = AppSettings.SpeedTest.AutoSpeedTest;
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        _ = StartSpeedTest();
    }

    [RelayCommand]
    public void CheckSpeedTestAvailable()
    {
        IsExecutableAvailable = SpeedTestService.IsSpeedTestAvailable();
    }

    [RelayCommand]
    public async Task AutoInstallDependency()
    {
        if (IsExecutableInstalling) return;
        IsExecutableInstalling = true;
        ProcessXToast toast = new()
        {
            Title = App.Localization["SpeedTest.Install.Title"],
            ShowOnlySuccessGenericMessage = true,
            SuccessGenericMessage = App.Localization["SpeedTest.Install.Success"],
            ErrorGenericMessage = App.Localization["SpeedTest.Install.Error"]
        };

        if (OperatingSystem.IsMacOS())
        {
            await ProcessXExtensions.ExecuteHandled(
                [
                    "brew tap teamookla/speedtest",
                    "brew install speedtest --force"
                ],
                toast,
                true);
        }
        else if (OperatingSystem.IsWindows())
        {
            await ProcessXExtensions.ExecuteHandled(
                "winget.exe install --id \"Ookla.Speedtest.CLI\" --exact --source winget --accept-source-agreements --accept-package-agreements  --disable-interactivity --silent --force",
                toast,
                true);
        }
        else if (OperatingSystem.IsLinux())
        {
            if (LinuxOS.PackageManager != LinuxOS.LinuxPackageManager.Unknown)
            {
                await ProcessXExtensions.ExecuteHandled(
                    $"{LinuxOS.PackageManagerCommand} install speedtest-cli",
                    toast,
                    true);
            }
        }
        CheckSpeedTestAvailable();
        IsExecutableInstalling = false;
    }

    [RelayCommand]
    public async Task UpdateServerList()
    {
        var servers = await SpeedTestService.GetServerList();
        Servers.Clear();
        Servers.Add(null);
        Servers.AddRange(servers);
    }

    [RelayCommand]
    public async Task StartSpeedTest()
    {
        if (IsRunning) return;

        var cancellationTokenSource = new CancellationTokenSource();
        _cancellationTokenSource = cancellationTokenSource;
        IsRunning = true;

        try
        {
            await foreach (var result in SpeedTestService.StartSpeedTest(SelectedServer, cancellationTokenSource.Token))
            {
                if (result.HasError)
                {
                    App.ShowToast(NotificationType.Error, App.Localization["SpeedTest.Error"], result.Error);
                    continue;
                }

                if (!Enum.TryParse(result.Type.AsSpan(), true, out SpeedTestType resultType)) continue;
                switch (resultType)
                {
                    case SpeedTestType.TestStart:
                        AngularMeterMaxValue = AppSettings.SpeedTest.InitialSpeedGaugeRange;
                        DisplayResult = result;
                        break;
                    case SpeedTestType.Ping:
                        DisplayResult = DisplayResult! with
                        {
                            Type = result.Type,
                            ISP = result.ISP,
                            PacketLoss = result.PacketLoss,
                            Timestamp = result.Timestamp,
                            Error = result.Error,
                            Ping = result.Ping,
                        };
                        break;
                    case SpeedTestType.Download:
                        DisplayResult = DisplayResult! with
                        {
                            Type = result.Type,
                            ISP = result.ISP,
                            PacketLoss = result.PacketLoss,
                            Timestamp = result.Timestamp,
                            Error = result.Error,
                            Download = result.Download
                        };
                        break;
                    case SpeedTestType.Upload:
                        DisplayResult = DisplayResult! with
                        {
                            Type = result.Type,
                            ISP = result.ISP,
                            PacketLoss = result.PacketLoss,
                            Timestamp = result.Timestamp,
                            Error = result.Error,
                            Upload = result.Upload
                        };
                        break;
                    case SpeedTestType.Result:
                        Results.Insert(0, result);
                        SelectedResult = result;
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            // The active speed test was stopped by the user.
        }
        catch (Exception e)
        {
            App.ShowExceptionToast(e, App.Localization["SpeedTest.Error"]);
        }
        finally
        {
            if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
            {
                _cancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
            IsRunning = false;
        }

    }

    [RelayCommand]
    public async Task StopSpeedTest()
    {
        var cancellationTokenSource = _cancellationTokenSource;
        if (cancellationTokenSource is null) return;

        await cancellationTokenSource.CancelAsync();
    }

    [RelayCommand]
    public async Task ExportSelectedResultsToJson()
    {
        if (_speedTestDataGrid.SelectedIndex == -1) return;
        using var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            ShowOverwritePrompt = true,
            SuggestedFileName = StringExtensions.GetSafeFilename($"Speedtests#{_speedTestDataGrid.SelectedItems.Count}-{DateTime.Now:dd-MM-yyyy-HH-mm-ss}.json"),
            DefaultExtension = "json",
            FileTypeChoices = AvaloniaExtensions.FilePickerJson
        });

        if (file is null) return;

        try
        {
            var filePath = file.TryGetLocalPath();
            if (filePath is null) return;
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, _speedTestDataGrid.SelectedItems, App.JsonSerializerOptions);
            App.ShowToast(NotificationType.Success,
                App.Localization["Export.Results.Title"],
                App.Localization.Format("Export.Results.Success", _speedTestDataGrid.SelectedItems.Count, file.Name),
                new ToastActionButton(App.Localization["Common.OpenFile"], toast => { SystemAware.StartProcess(filePath); }),
                new ToastActionButton(App.Localization["Common.OpenFolder"], toast => { SystemAware.SelectFileOnExplorer(filePath); })
            );

        }
        catch (Exception e)
        {
            App.ShowExceptionToast(e, App.Localization["Export.Results.Title"], App.Localization["Export.Results.Error"]);
        }
    }

    [RelayCommand]
    public static async Task ExportAllResultsToJson()
    {
        if (Results.Count == 0) return;
        using var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            ShowOverwritePrompt = true,
            SuggestedFileName = StringExtensions.GetSafeFilename($"Speedtests#{Results.Count}-{DateTime.Now:dd-MM-yyyy-HH-mm-ss}.json"),
            DefaultExtension = "json",
            FileTypeChoices = AvaloniaExtensions.FilePickerJson
        });

        if (file is null) return;

        try
        {
            var filePath = file.TryGetLocalPath();
            if (filePath is null) return;
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, Results, App.JsonSerializerOptions);
            App.ShowToast(NotificationType.Success,
                App.Localization["Export.Results.Title"],
                App.Localization.Format("Export.Results.Success", Results.Count, file.Name),
                new ToastActionButton(App.Localization["Common.OpenFile"], toast => { SystemAware.StartProcess(filePath); }),
                new ToastActionButton(App.Localization["Common.OpenFolder"], toast => { SystemAware.SelectFileOnExplorer(filePath); })
            );

        }
        catch (Exception e)
        {
            App.ShowExceptionToast(e, App.Localization["Export.Results.Title"], App.Localization["Export.Results.Error"]);
        }
    }

    [RelayCommand]
    public void RemoveSelectedResults()
    {
        if (_speedTestDataGrid.SelectedIndex == -1) return;
        CreateMessageBoxYesNo(NotificationType.Warning,
                App.Localization.Format("SpeedTest.RemoveSelected.Title", _speedTestDataGrid.SelectedItems.Count),
                App.Localization.Format("SpeedTest.RemoveSelected.Message", _speedTestDataGrid.SelectedItems.Count),
                _ => Results.RemoveRange(_speedTestDataGrid.SelectedItems))
            .TryShow();

    }

    [RelayCommand]
    public static void RemoveAllResults()
    {
        if (Results.Count == 0) return;
        CreateMessageBoxYesNo(NotificationType.Warning,
                App.Localization.Format("SpeedTest.RemoveAll.Title", Results.Count),
                App.Localization.Format("SpeedTest.RemoveAll.Message", Results.Count), _ => Results.Clear())
            .TryShow();

    }

    public void SetControls(DataGrid speedTestDataGrid)
    {
        _speedTestDataGrid = speedTestDataGrid;
        _speedTestDataGrid.ExtendDataGridShortcuts();
    }
}
