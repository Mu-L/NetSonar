using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.Localization;
using NetSonar.Avalonia.Models;
using NetSonar.Avalonia.Network;
using NetSonar.Avalonia.Settings;
using ObservableCollections;
using StageKit;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using ZLinq;

namespace NetSonar.Avalonia.ViewModels.Dialogs;

public partial class AddPingServicesDialogModel : DialogViewModelBase
{
    private readonly PingableService[] _editingServices;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddPingServicesDialogModel"/> class.
    /// </summary>
    /// <param name="dialog">The hosting dialog.</param>
    /// <param name="servicesToEdit">The services to edit, or <c>null</c>/empty to import new services.</param>
    public AddPingServicesDialogModel(ISukiDialog dialog, IEnumerable<PingableService>? servicesToEdit = null)
        : base(dialog)
    {
        ServicesView =
            Services.ToWritableNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        _editingServices = servicesToEdit?.ToArray() ?? [];

        if (_editingServices.Length == 0) AddEmpty();
        else Services.AddRange(_editingServices.Select(service => new NewPingService(service)));

        App.Localization.PropertyChanged += LocalizationOnPropertyChanged;
    }

    /// <summary>
    /// Raised when the edited service had to be rebuilt, carrying the replaced and the replacement instances.
    /// </summary>
    public event Action<PingableService, PingableService>? ServiceReplaced;

    public ObservableList<NewPingService> Services { get; } = [];

    public NotifyCollectionChangedSynchronizedViewList<NewPingService> ServicesView { get; }

    /// <summary>
    /// Gets a value indicating whether the dialog edits existing services instead of importing new ones.
    /// </summary>
    public bool IsEditing => _editingServices.Length > 0;

    public string HeaderText => IsEditing
        ? App.Localization[_editingServices.Length > 1 ? "Ui.EditServices" : "Ui.EditService"]
        : App.Localization["Ui.Services"];

    public string ApplyButtonText => App.Localization[IsEditing ? "Ui.Save" : "Ui.Import"];

    public MaterialIconKind ApplyButtonIcon => IsEditing ? MaterialIconKind.ContentSave : MaterialIconKind.Import;

    protected internal override void OnUnloaded()
    {
        base.OnUnloaded();
        App.Localization.PropertyChanged -= LocalizationOnPropertyChanged;
        ServicesView.Dispose();
    }

    private void LocalizationOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ILocalizationService.Culture)) return;
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(ApplyButtonText));
    }

    [RelayCommand]
    public void Clear()
    {
        if (IsEditing) return;
        Services.Clear();
        AddEmpty();
    }

    [RelayCommand]
    public void AddEmpty()
    {
        Services.Add(new NewPingService());
    }

    [RelayCommand]
    public async Task ImportFromJson()
    {
        var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = AvaloniaExtensions.FilePickerJson
        });

        if (files.Count == 0) return;

        foreach (var file in files)
        {
            try
            {
                await using var stream = await file.OpenReadAsync();
                var services =
                    await JsonSerializer.DeserializeAsync<PingableService[]>(stream, App.JsonSerializerOptions);
                if (services is null) continue;
                PurgeEmptyRecords();
                AddUniques(services.Select(service => new NewPingService(service)));
            }
            catch (Exception e)
            {
                App.ShowExceptionToast(App.Localization["Import.Json.ErrorTitle"],
                    App.Localization["Import.Json.ErrorMessage"]);
                UnhandledExceptions.HandleSafeException(e, "Import service from json");
            }
        }
    }

    [RelayCommand]
    public void ImportAllPublicProtocolHosts()
    {
        PurgeEmptyRecords();


        AddUniques(DnsProvider.DnsProviders
            .Where(dnsProvider => dnsProvider.DNSv4PrimaryAddress.IsValid())
            .Select(dnsProvider => new NewPingService(ServiceProtocolType.DNS,
                dnsProvider.DNSv4PrimaryAddress,
                dnsProvider.FormatedDescription,
                nameof(ServiceProtocolType.DNS))));

        AddUniques(BaseProvider.PublicHosts
            .Select(provider => new NewPingService(provider.ProtocolType, provider.Address,
                provider.FormatedDescription, provider.ProtocolType.ToString())));
    }

    [RelayCommand]
    public void ImportPublicProtocolHosts(ServiceProtocolType protocolType)
    {
        PurgeEmptyRecords();

        if (protocolType == ServiceProtocolType.DNS)
        {
            AddUniques(DnsProvider.DnsProviders
                .Where(dnsProvider => dnsProvider.DNSv4PrimaryAddress.IsValid())
                .Select(dnsProvider => new NewPingService(ServiceProtocolType.DNS,
                    dnsProvider.DNSv4PrimaryAddress,
                    dnsProvider.FormatedDescription,
                    nameof(ServiceProtocolType.DNS))));
            return;
        }

        AddUniques(BaseProvider.PublicHosts
            .Where(provider => provider.ProtocolType == protocolType)
            .Select(provider => new NewPingService(provider.ProtocolType, provider.Address,
                provider.FormatedDescription, provider.ProtocolType.ToString())));
    }

    [RelayCommand]
    public void ImportNetworkGateways()
    {
        var cache = new List<NewPingService>();
        var gatewayCount = 0;
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            foreach (var address in network.GetIPProperties().GatewayAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork || !address.Address.IsValid()) continue;
                cache.Add(new NewPingService(ServiceProtocolType.ICMP, address.Address, $"Gateway {++gatewayCount}",
                    "Network Gateway"));
            }
        }

        if (cache.Count == 0) return;
        PurgeEmptyRecords();
        AddUniques(cache);
    }

    [RelayCommand]
    public void RemoveServices(IList list)
    {
        if (IsEditing) return;
        Services.RemoveRange(list.Cast<NewPingService>());
    }

    [RelayCommand]
    public async Task PasteFromClipboard()
    {
        if (IsEditing) return;
        var clipboard = TopLevel.Clipboard;
        if (clipboard is null) return;
        var data = await clipboard.TryGetDataAsync();
        var text = data is null ? null : await data.TryGetTextAsync();

        if (string.IsNullOrWhiteSpace(text))
        {
            App.ShowToast(NotificationType.Warning,
                App.Localization["Import.Clipboard.ErrorTitle"],
                App.Localization["Import.Clipboard.NoData"]);
            return;
        }

        var result = ClipboardPasteParser.Parse(text);
        if (result.Services.Count == 0)
        {
            ShowPasteSummary(0, result.SkippedCount);
            return;
        }

        PurgeEmptyRecords();
        var addedCount = AddUniques(result.Services);
        var duplicateCount = result.Services.Count - addedCount;

        ShowPasteSummary(addedCount, result.SkippedCount + duplicateCount);
    }

    private void PurgeEmptyRecords()
    {
        for (var i = Services.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(Services[i].IpAddressOrUrl)) Services.RemoveAt(i);
        }
    }

    private int AddUniques(IEnumerable<NewPingService> services)
    {
        using var servicesPool =
            services.AsValueEnumerable().Distinct().Where(service => !Services.Contains(service)).ToArrayPool();
        Services.AddRange(servicesPool.Span);
        return servicesPool.Size;
    }

    private static void ShowPasteSummary(int addedCount, int skippedCount)
    {
        ToastManager.CreateSimpleInfoToast()
            .WithContent(App.Localization.Format("Import.Clipboard.Summary", addedCount, skippedCount))
            .Queue();
    }

    protected override bool ValidateInternal()
    {
        foreach (var service in Services)
        {
            if (service.Validate()) continue;
            CustomErrors.Add(service.GetErrorsRaw());
        }

        return base.ValidateInternal();
    }

    protected override Task<bool> ApplyInternal()
    {
        if (IsEditing)
        {
            if (!ApplyEdit()) return Task.FromResult(false);

            ToastManager.CreateSimpleInfoToast()
                .WithContent(_editingServices.Length > 1
                    ? App.Localization.Format("Ui.ServicesUpdated", _editingServices.Length)
                    : App.Localization["Ui.ServiceUpdated"])
                .Queue();
            return base.ApplyInternal();
        }

        var importCount = 0;

        foreach (var service in Services)
        {
            // Check for duplicates
            if (PingableServicesFile.Instance.AsValueEnumerable().FirstOrDefault(pingableService =>
                    pingableService.ProtocolType == service.ProtocolType &&
                    pingableService.IpAddressOrUrl == service.IpAddressOrUrl) is not null) continue;
            try
            {
                PingableServicesFile.Instance.Add(new PingableService(service));
                importCount++;
            }
            catch (Exception ex)
            {
                UnhandledExceptions.HandleSafeException(ex, "Add Service");
            }
        }

        ToastManager.CreateSimpleInfoToast()
            .WithContent($"{importCount} services were imported.")
            .Queue();

        return base.ApplyInternal();
    }

    /// <summary>
    /// Applies the edited values back to the services being edited.
    /// </summary>
    /// <returns><c>true</c> when every service was updated; otherwise <c>false</c> and the dialog stays open.</returns>
    private bool ApplyEdit()
    {
        if (_editingServices.Length == 0 || Services.Count != _editingServices.Length) return false;

        var services = PingableServicesFile.Instance;

        // Validate all identities up-front so a rejected entry never leaves a half-applied edit behind.
        for (var i = 0; i < Services.Count; i++)
        {
            var edited = Services[i];

            // Two edited rows must not end up with the same identity.
            for (var j = 0; j < i; j++)
            {
                if (!HasSameIdentity(Services[j], edited)) continue;
                ShowDuplicateWarning();
                return false;
            }

            // Nor may an edited row collide with a service that is not part of this edit.
            foreach (var service in services)
            {
                if (IsUnderEdit(service) || !HasSameIdentity(edited, service)) continue;
                ShowDuplicateWarning();
                return false;
            }
        }

        for (var i = 0; i < Services.Count; i++)
        {
            var edited = Services[i];
            var target = _editingServices[i];

            if (HasSameIdentity(edited, target))
            {
                target.Description = edited.Description;
                target.Group = edited.Group;
                target.IsEnabled = edited.IsEnabled;
                target.PingEverySeconds = edited.PingEverySeconds;
                target.TimeoutSeconds = edited.TimeoutSeconds;
                target.BufferSize = PingableService.GetProtocolBufferSize(target.ProtocolType, edited.BufferSize);
                target.Ttl = edited.Ttl;
                target.DontFragment = edited.DontFragment;
                continue;
            }

            // ProtocolType and IpAddressOrUrl are init-only, so the service has to be rebuilt in place.
            var index = services.IndexOf(target);
            var replacement = new PingableService(edited) { Order = target.Order };

            services.Remove(target);
            if (index >= 0) services.Insert(index, replacement);
            else services.Add(replacement);

            _editingServices[i] = replacement;
            ServiceReplaced?.Invoke(target, replacement);
        }

        return true;
    }

    /// <summary>
    /// Checks whether a service is one of the services this dialog is editing.
    /// </summary>
    private bool IsUnderEdit(PingableService service)
    {
        foreach (var editing in _editingServices)
        {
            if (ReferenceEquals(editing, service)) return true;
        }

        return false;
    }

    private static bool HasSameIdentity(NewPingService edited, PingableService service)
    {
        return edited.ProtocolType == service.ProtocolType
               && string.Equals(edited.IpAddressOrUrl, service.IpAddressOrUrl, StringComparison.Ordinal);
    }

    private static bool HasSameIdentity(NewPingService left, NewPingService right)
    {
        return left.ProtocolType == right.ProtocolType
               && string.Equals(left.IpAddressOrUrl, right.IpAddressOrUrl, StringComparison.Ordinal);
    }

    private void ShowDuplicateWarning()
    {
        App.ShowToast(NotificationType.Warning, HeaderText, App.Localization["Ui.ServiceAlreadyExists"]);
    }
}
