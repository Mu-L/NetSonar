using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using NetSonar.Avalonia.Models;
using NetSonar.Avalonia.Network;
using NetSonar.Avalonia.ViewModels.Dialogs;
using ObservableCollections;
using StageKit.Primitives.System;
using StageKit.Runtime.System;
using SukiUI.Dialogs;

namespace NetSonar.Avalonia.ViewModels;

public partial class NetworkScannerPageModel : PageViewModelBase
{
    private readonly ObservableCollection<NetworkScannerTreeNode> _hostNodes = [];
    private CancellationTokenSource? _cancellationTokenSource;

    public NetworkScannerPageModel()
    {
        TargetsView = Targets.ToNotifyCollectionChangedSlim(
            SynchronizationContextCollectionEventDispatcher.Current
        );
        HostsSource = new HierarchicalTreeDataGridSource<NetworkScannerTreeNode>(_hostNodes)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<NetworkScannerTreeNode>(
                    new TextColumn<NetworkScannerTreeNode, string>(
                        App.Localization["Ui.ScannerHost"],
                        node => node.Name
                    ),
                    node => node.Children
                ),
                new TextColumn<NetworkScannerTreeNode, string>(
                    App.Localization["Ui.Status"],
                    node => node.State
                ),
                new TextColumn<NetworkScannerTreeNode, string>(
                    App.Localization["Ui.ScannerMacAddress"],
                    node => node.MacAddress
                ),
                new TextColumn<NetworkScannerTreeNode, string>(
                    App.Localization["Ui.ScannerVendor"],
                    node => node.Vendor
                ),
                new TextColumn<NetworkScannerTreeNode, string>(
                    App.Localization["Ui.ScannerPorts"],
                    node => node.PortCount
                ),
                new TextColumn<NetworkScannerTreeNode, string>(
                    App.Localization["Ui.ScannerProtocol"],
                    node => node.Protocol
                ),
                new TextColumn<NetworkScannerTreeNode, string>(
                    App.Localization["Ui.ScannerService"],
                    node => node.Service
                ),
                new TextColumn<NetworkScannerTreeNode, string>(
                    App.Localization["Ui.ScannerVersion"],
                    node => node.Version
                ),
            },
        };
        HostsSource.RowSelection!.SingleSelect = false;
        HostsSource.RowSelection.SelectionChanged += (_, _) => UpdateSelectionState();
    }

    public override int Index => 2;
    public override string DisplayName => App.Localization["Navigation.NetworkScanner"];
    public override MaterialIconKind Icon => MaterialIconKind.Radar;

    public ObservableList<string> Targets { get; } = [];
    public NotifyCollectionChangedSynchronizedViewList<string> TargetsView { get; }
    public ObservableList<NetworkScannerHost> Hosts { get; } = [];
    public HierarchicalTreeDataGridSource<NetworkScannerTreeNode> HostsSource { get; }

    [ObservableProperty]
    public partial string Target { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanNetworkCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScanSelectedPortsCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedToMonitoringCommand))]
    public partial bool IsExecutableAvailable { get; private set; }

    [ObservableProperty]
    public partial bool IsInstalling { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanNetworkCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScanSelectedPortsCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedToMonitoringCommand))]
    public partial bool IsScanning { get; private set; }

    public bool CanAutoInstall => GetInstallRequest() is not null;

    public bool HasSelectedHosts =>
        HostsSource.RowSelection?.SelectedItems.Any(node => node?.Host is not null) == true;

    protected internal override void OnInitialized()
    {
        base.OnInitialized();
        ReloadDependency();
        RefreshTargets();
    }

    protected internal override void OnLoaded()
    {
        base.OnLoaded();
        ReloadDependency();
    }

    [RelayCommand]
    public void ReloadDependency()
    {
        IsExecutableAvailable = NmapScannerService.TryFindExecutable(out _);
        OnPropertyChanged(nameof(CanAutoInstall));
    }

    [RelayCommand]
    public void RefreshTargets()
    {
        var currentTarget = Target;
        Targets.Clear();
        Targets.AddRange(NmapScannerService.GetLocalTargets());
        Target = string.IsNullOrWhiteSpace(currentTarget)
            ? Targets.FirstOrDefault() ?? string.Empty
            : currentTarget;
    }

    [RelayCommand(CanExecute = nameof(CanStartScan))]
    public async Task ScanNetwork()
    {
        if (!NmapScannerService.TryFindExecutable(out var executablePath))
        {
            ReloadDependency();
            return;
        }

        var target = Target.Trim();
        if (!NmapScannerService.IsValidTarget(target))
        {
            App.ShowToast(
                NotificationType.Warning,
                App.Localization["Navigation.NetworkScanner"],
                App.Localization["NetworkScanner.InvalidTarget"]
            );
            return;
        }

        await RunScan(async token =>
        {
            var hosts = await NmapScannerService.DiscoverAsync(executablePath, target, token);
            Hosts.Clear();
            Hosts.AddRange(hosts);
            RebuildHostTree();
        });
    }

    [RelayCommand(CanExecute = nameof(CanScanSelectedPorts))]
    public async Task ScanSelectedPorts()
    {
        if (!NmapScannerService.TryFindExecutable(out var executablePath))
        {
            ReloadDependency();
            return;
        }

        var selected = GetSelectedHosts();
        await RunScan(async token =>
        {
            var scannedHosts = await NmapScannerService.ScanPortsAsync(
                executablePath,
                selected.Select(host => host.Address),
                token
            );
            var portsByAddress = scannedHosts.ToDictionary(
                host => host.Address,
                StringComparer.Ordinal
            );
            for (var i = 0; i < Hosts.Count; i++)
            {
                if (!portsByAddress.TryGetValue(Hosts[i].Address, out var scannedHost))
                    continue;
                Hosts[i] = new NetworkScannerHost
                {
                    Address = Hosts[i].Address,
                    Hostname = string.IsNullOrWhiteSpace(scannedHost.Hostname)
                        ? Hosts[i].Hostname
                        : scannedHost.Hostname,
                    MacAddress = Hosts[i].MacAddress,
                    Vendor = Hosts[i].Vendor,
                    State = scannedHost.State,
                    Ports = scannedHost.Ports,
                };
            }

            RebuildHostTree(selected.Select(host => host.Address));
        });
    }

    [RelayCommand]
    public async Task CancelScan()
    {
        if (_cancellationTokenSource is not null)
            await _cancellationTokenSource.CancelAsync();
    }

    [RelayCommand]
    public void ClearResults()
    {
        Hosts.Clear();
        RebuildHostTree();
    }

    [RelayCommand(CanExecute = nameof(CanScanSelectedPorts))]
    public void AddSelectedToMonitoring()
    {
        var services = GetSelectedHosts()
            .Select(host => new NewPingService(
                ServiceProtocolType.ICMP,
                host.Address,
                string.IsNullOrWhiteSpace(host.Hostname) ? host.Vendor : host.Hostname,
                "Network Scanner"
            ))
            .ToArray();
        if (services.Length == 0)
            return;

        DialogManager
            .CreateDialog()
            .WithViewModel(dialog => AddPingServicesDialogModel.CreateForImport(dialog, services))
            .TryShow();
    }

    [RelayCommand]
    public async Task AutoInstallDependency()
    {
        var request = GetInstallRequest();
        if (request is null || IsInstalling)
            return;

        IsInstalling = true;
        try
        {
            var output = await ProcessHelper.GetProcessOutputAsync(
                request.Value.Executable,
                request.Value.Arguments,
                request.Value.RequireElevation,
                CancellationToken.None
            );
            if (output.ExitCode == 0)
            {
                App.ShowToast(
                    NotificationType.Success,
                    App.Localization["NetworkScanner.Install.Title"],
                    App.Localization["NetworkScanner.Install.Success"]
                );
            }
            else
            {
                App.ShowToast(
                    NotificationType.Error,
                    App.Localization["NetworkScanner.Install.Title"],
                    string.IsNullOrWhiteSpace(output.StandardError)
                        ? App.Localization["NetworkScanner.Install.Error"]
                        : output.StandardError
                );
            }
        }
        catch (Exception exception)
        {
            App.ShowExceptionToast(
                exception,
                App.Localization["NetworkScanner.Install.Title"],
                App.Localization["NetworkScanner.Install.Error"]
            );
        }
        finally
        {
            IsInstalling = false;
            ReloadDependency();
        }
    }

    private bool CanStartScan()
    {
        return IsExecutableAvailable && !IsScanning;
    }

    private bool CanScanSelectedPorts()
    {
        return IsExecutableAvailable && !IsScanning && HasSelectedHosts;
    }

    private async Task RunScan(Func<CancellationToken, Task> scan)
    {
        if (IsScanning)
            return;
        using var cancellationTokenSource = new CancellationTokenSource();
        _cancellationTokenSource = cancellationTokenSource;
        IsScanning = true;
        try
        {
            await scan(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            // The user cancelled the active scan.
        }
        catch (Exception exception)
        {
            App.ShowExceptionToast(exception, App.Localization["Navigation.NetworkScanner"]);
        }
        finally
        {
            if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
                _cancellationTokenSource = null;
            IsScanning = false;
        }
    }

    private NetworkScannerHost[] GetSelectedHosts()
    {
        return HostsSource
                .RowSelection?.SelectedItems.Where(node => node?.Host is not null)
                .Select(node => node!.Host!)
                .ToArray()
            ?? [];
    }

    private void RebuildHostTree(IEnumerable<string>? selectedAddresses = null)
    {
        var selected = selectedAddresses?.ToHashSet(StringComparer.Ordinal) ?? [];
        HostsSource.RowSelection?.Clear();
        _hostNodes.Clear();
        foreach (var host in Hosts)
            _hostNodes.Add(NetworkScannerTreeNode.ForHost(host));

        HostsSource.ExpandAll();
        for (var i = 0; i < _hostNodes.Count; i++)
        {
            if (selected.Contains(_hostNodes[i].Host!.Address))
                HostsSource.RowSelection?.Select(new IndexPath(i));
        }

        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        OnPropertyChanged(nameof(HasSelectedHosts));
        ScanSelectedPortsCommand.NotifyCanExecuteChanged();
        AddSelectedToMonitoringCommand.NotifyCanExecuteChanged();
    }

    private (string Executable, string[] Arguments, bool RequireElevation)? GetInstallRequest()
    {
        if (OperatingSystem.IsWindows())
        {
            // --accept-package-agreements --accept-source-agreements -e --id
            return HostSystem.TryFindExecutable("winget.exe", out var winget)
                ? (
                    winget,
                    [
                        "install",
                        "--id",
                        "Insecure.Nmap",
                        "--exact",
                        "--source",
                        "winget",
                        "--silent",
                        "--disable-interactivity",
                        "--accept-package-agreements",
                        "--accept-source-agreements",
                    ],
                    false
                )
                : null;
        }

        if (OperatingSystem.IsMacOS())
        {
            return HostSystem.TryFindExecutable("brew", out var brew)
                ? (brew, ["install", "nmap"], false)
                : null;
        }

        if (!OperatingSystem.IsLinux())
            return null;
        return LinuxRuntime.PackageManager switch
        {
            LinuxPackageManager.Apt => ("apt", ["install", "-y", "nmap"], true),
            LinuxPackageManager.Dnf or LinuxPackageManager.Dnf5 or LinuxPackageManager.Yum => (
                LinuxRuntime.PackageManager.CommandName,
                ["install", "-y", "nmap"],
                true
            ),
            LinuxPackageManager.Pacman => ("pacman", ["-S", "--noconfirm", "nmap"], true),
            _ => null,
        };
    }
}

public sealed class NetworkScannerTreeNode
{
    private NetworkScannerTreeNode(
        NetworkScannerHost? host,
        NetworkScannerPort? port,
        IReadOnlyList<NetworkScannerTreeNode> children
    )
    {
        Host = host;
        Port = port;
        Children = children;
    }

    public NetworkScannerHost? Host { get; }
    public NetworkScannerPort? Port { get; }
    public IReadOnlyList<NetworkScannerTreeNode> Children { get; }

    public string Name =>
        Host is not null
            ? string.IsNullOrWhiteSpace(Host.Hostname)
                ? Host.Address
                : $"{Host.Address} ({Host.Hostname})"
            : $"{Port!.Number}/{Port.Protocol}";

    public string State => Host?.State ?? Port?.State ?? string.Empty;
    public string MacAddress => Host?.MacAddress ?? string.Empty;
    public string Vendor => Host?.Vendor ?? string.Empty;
    public string PortCount => Host is null ? string.Empty : Host.OpenPortCount.ToString();
    public string Protocol => Port?.Protocol ?? string.Empty;
    public string Service => Port?.Service ?? string.Empty;
    public string Version => Port?.ProductVersion ?? string.Empty;

    public static NetworkScannerTreeNode ForHost(NetworkScannerHost host)
    {
        return new NetworkScannerTreeNode(
            host,
            null,
            host.Ports.Select(port => new NetworkScannerTreeNode(null, port, [])).ToArray()
        );
    }
}
