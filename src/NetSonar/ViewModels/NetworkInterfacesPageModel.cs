using System;
using Material.Icons;
using System.Net.NetworkInformation;
using NetSonar.Avalonia.Network;
using ObservableCollections;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetSonar.Avalonia.Models;
using NetSonar.Avalonia.Settings;
using NetSonar.Avalonia.SystemOS;
using ZLinq;

namespace NetSonar.Avalonia.ViewModels;

public partial class NetworkInterfacesPageModel : PageViewModelBase
{
    public override int Index => 1;
    public override string DisplayName => "Interfaces";
    public override MaterialIconKind Icon => MaterialIconKind.Network;

    private readonly DispatcherTimer _timer = new();

    public ISynchronizedView<KeyValuePair<string, NetworkInterfaceBridge>, NetworkInterfaceBridge> InterfacesView { get; }

    public ObservableDictionary<string, NetworkInterfaceBridge> Interfaces { get; } = [];

    public NotifyCollectionChangedSynchronizedViewList<NetworkInterfaceBridge> InterfacesViewCollection { get; }

    public NotifyCollectionChangedSynchronizedViewList<EnumViewFilter> InterfaceTypeFiltersCollection { get; }
    public NotifyCollectionChangedSynchronizedViewList<EnumViewFilter> InterfaceStatusFiltersCollection { get; }


    [ObservableProperty] public partial int InterfaceCount { get; private set; }
    [ObservableProperty] public partial int OfflineInterfaceCount { get; private set; }

    [ObservableProperty] public partial int OnlineInterfaceCount { get; private set; }

    [ObservableProperty] public partial string FilterText { get; set; } = string.Empty;


    public NetworkInterfacesPageModel()
    {
        InterfacesView = Interfaces.CreateView(pair => pair.Value);
        InterfacesViewCollection = InterfacesView.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        var interfaceTypeFilters = AppSettings.NetworkInterfaces.FilterTypes.CreateView(pair => pair.Value);
        InterfaceTypeFiltersCollection = interfaceTypeFilters.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        var interfaceStatusFilters = AppSettings.NetworkInterfaces.FilterStatus.CreateView(pair => pair.Value);
        InterfaceStatusFiltersCollection = interfaceStatusFilters.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        AppSettings.NetworkInterfaces.PropertyChanged += NetworkInterfacesOnPropertyChanged;


        foreach (var filter in InterfaceTypeFiltersCollection)
        {
            filter.PropertyChanged += Filter_PropertyChanged;
        }

        foreach (var filter in InterfaceStatusFiltersCollection)
        {
            filter.PropertyChanged += Filter_PropertyChanged;
        }

        _timer.Tick += TimerTick;
    }

    protected internal override void OnInitialized()
    {
        _timer.Interval = TimeSpan.FromSeconds(AppSettings.NetworkInterfaces.RefreshEverySeconds);
        _timer.IsEnabled = AppSettings.NetworkInterfaces.AutoRefresh;
        Refresh();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FilterText))
        {
            ReAttachFilters();
        }
        base.OnPropertyChanged(e);
    }

    private void NetworkInterfacesOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null) return;
        if (e.PropertyName == nameof(NetworkInterfacesSettings.AutoRefresh))
        {
            _timer.IsEnabled = AppSettings.NetworkInterfaces.AutoRefresh;
        }
        else if (e.PropertyName == nameof(NetworkInterfacesSettings.RefreshEverySeconds))
        {
            _timer.Interval = TimeSpan.FromSeconds(AppSettings.NetworkInterfaces.RefreshEverySeconds);
        }
        else if (e.PropertyName.Contains("Filter")) ReAttachFilters();
    }

    private void Filter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EnumViewFilter.Include)) ReAttachFilters();
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        var networks = NetworkInterface.GetAllNetworkInterfaces();
        var keys = new string[networks.Length];
        int offlineCount = 0;
        int onlineCount = 0;
        for (var i = 0; i < networks.Length; i++)
        {
            var adapter = networks[i];

            if (Interfaces.TryGetValue(adapter.Id, out var networkInterface))
            {
                networkInterface.Interface = adapter;
            }
            else
            {
                networkInterface = new NetworkInterfaceBridge(adapter);
                networkInterface.Refresh();
                Interfaces.Add(adapter.Id, networkInterface);
            }

            keys[i] = adapter.Id;
            if (networkInterface.IsActive) onlineCount++;
            else offlineCount++;
        }

        // Remove gone interfaces
        using (var keysToRemove = Interfaces
                   .AsValueEnumerable()
                   .Where(adapter => !keys.AsValueEnumerable().Contains(adapter.Value.Interface.Id))
                   .Select(adapter => adapter.Value.Interface.Id)
                   .ToArrayPool())
        {
            foreach (var key in keysToRemove.Span)
            {
                if (!Interfaces.Remove(key, out var networkInterface)) continue;
                networkInterface.Dispose();
            }
        }

        InterfaceCount = networks.Length;
        OfflineInterfaceCount = offlineCount;
        OnlineInterfaceCount = onlineCount;
        ReAttachFilters();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        FilterText = string.Empty;
    }

    [RelayCommand]
    public void AttachStatusOnlineFilter()
    {
        FilterText = "status:active";
    }

    [RelayCommand]
    public void AttachStatusOfflineFilter()
    {
        FilterText = "status:inactive";
    }

    [RelayCommand]
    public void ReAttachFilters()
    {
        InterfacesView.AttachFilter(pair =>
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                if (AppSettings.NetworkInterfaces.EnableFilterTypes)
                {
                    if (AppSettings.NetworkInterfaces.FilterTypes.TryGetValue(pair.Value.Interface.NetworkInterfaceType,
                            out var filter))
                    {
                        if (!filter.Include) return false;
                    }
                }

                if (AppSettings.NetworkInterfaces.EnableFilterStatus)
                {
                    if (AppSettings.NetworkInterfaces.FilterStatus.TryGetValue(pair.Value.Interface.OperationalStatus,
                            out var filter))
                    {
                        if (!filter.Include) return false;
                    }
                }

                if (AppSettings.NetworkInterfaces.EnableFilterOthers)
                {
                    if (AppSettings.NetworkInterfaces.FilterByVirtual.HasValue) if (pair.Value.IsVirtual != AppSettings.NetworkInterfaces.FilterByVirtual.Value) return false;
                    if (AppSettings.NetworkInterfaces.FilterByHavePhysicalAddress.HasValue) if (pair.Value.HavePhysicalAddress != AppSettings.NetworkInterfaces.FilterByHavePhysicalAddress.Value) return false;
                    if (AppSettings.NetworkInterfaces.FilterByHaveIPAddress.HasValue) if (pair.Value.HaveIPAddress != AppSettings.NetworkInterfaces.FilterByHaveIPAddress.Value) return false;
                    if (AppSettings.NetworkInterfaces.FilterByIsTransmittingData.HasValue) if (pair.Value.IsTransmittingData != AppSettings.NetworkInterfaces.FilterByIsTransmittingData.Value) return false;
                }
            }
            else
            {
                var splitText = FilterText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in splitText)
                {
                    if (word.Equals("status:up", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("status:active", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("status:online", StringComparison.OrdinalIgnoreCase))
                    {
                        return pair.Value.IsActive;
                    }

                    if (word.Equals("status:down", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("status:inactive", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("status:offline", StringComparison.OrdinalIgnoreCase))
                    {
                        return !pair.Value.IsActive;
                    }

                    if (pair.Value.Interface.Id.Contains(word, StringComparison.OrdinalIgnoreCase)) return true;
                    if (pair.Value.Interface.Name.Contains(word, StringComparison.OrdinalIgnoreCase)) return true;
                    if (pair.Value.Interface.Description.Contains(word, StringComparison.OrdinalIgnoreCase)) return true;
                    if (pair.Value.Interface.OperationalStatus.ToString().Equals(word, StringComparison.OrdinalIgnoreCase)) return true;
                    if (pair.Value.Interface.NetworkInterfaceType.ToString().Equals(word, StringComparison.OrdinalIgnoreCase)) return true;
                    if (pair.Value.Properties.UnicastAddresses.AsValueEnumerable().Any(address => address.Address.ToString().Contains(word, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }

                return false;
            }
            return true;
        });
    }

    [RelayCommand]
    public static void OpenSystemNetworkManager()
    {
        if (OperatingSystem.IsWindows())
        {
            // ReSharper disable once StringLiteralTypo
            SystemAware.StartProcess("ncpa.cpl");
        }
        else if (OperatingSystem.IsLinux())
        {
            SystemAware.StartProcess("nm-connection-editor");
        }

    }
}
