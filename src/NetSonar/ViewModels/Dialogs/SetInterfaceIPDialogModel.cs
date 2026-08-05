using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.OpenGL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetSonar.Avalonia.Cache;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.Network;
using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Utf8StringInterpolation;
using ZLinq;

namespace NetSonar.Avalonia.ViewModels.Dialogs;

public partial class SetInterfaceIPDialogModel : DialogViewModelBase
{
    private readonly PropertiesSnapshot _propertiesSnapshot = new();
    public NetworkInterfaceBridge NetworkInterface { get; } = null!;

    [ObservableProperty]
    public partial bool IPv4DHCP { get; set; }

    [ObservableProperty]
    public partial byte IPv4Octet1 { get; set; }

    [ObservableProperty]
    public partial byte IPv4Octet2 { get; set; }

    [ObservableProperty]
    public partial byte IPv4Octet3 { get; set; }

    [ObservableProperty]
    public partial byte IPv4Octet4 { get; set; }

    public IPAddress IPv4Address
    {
        get => new([IPv4Octet1, IPv4Octet2, IPv4Octet3, IPv4Octet4]);
        set
        {
            var bytes = value.GetAddressBytes();
            IPv4Octet1 = bytes[0];
            IPv4Octet2 = bytes[1];
            IPv4Octet3 = bytes[2];
            IPv4Octet4 = bytes[3];
        }
    }


    [ObservableProperty]
    public partial byte IPv4MaskCIDR { get; set; }

    [ObservableProperty]
    public partial byte IPv4MaskOctet1 { get; set; }

    [ObservableProperty]
    public partial byte IPv4MaskOctet2 { get; set; }

    [ObservableProperty]
    public partial byte IPv4MaskOctet3 { get; set; }

    [ObservableProperty]
    public partial byte IPv4MaskOctet4 { get; set; }

    public IPAddress IPv4Mask
    {
        get => new([IPv4MaskOctet1, IPv4MaskOctet2, IPv4MaskOctet3, IPv4MaskOctet4]);
        set
        {
            var bytes = value.GetAddressBytes();
            IPv4MaskOctet1 = bytes[0];
            IPv4MaskOctet2 = bytes[1];
            IPv4MaskOctet3 = bytes[2];
            IPv4MaskOctet4 = bytes[3];
        }
    }

    [ObservableProperty]
    public partial byte? IPv4GatewayOctet1 { get; set; }

    [ObservableProperty]
    public partial byte? IPv4GatewayOctet2 { get; set; }

    [ObservableProperty]
    public partial byte? IPv4GatewayOctet3 { get; set; }

    [ObservableProperty]
    public partial byte? IPv4GatewayOctet4 { get; set; }

    public IPAddress? IPv4Gateway
    {
        get
        {
            if (IPv4GatewayOctet1 is null
                || IPv4GatewayOctet2 is null
                || IPv4GatewayOctet3 is null
                || IPv4GatewayOctet4 is null) return null;
            return new IPAddress([
                IPv4GatewayOctet1.Value,
                IPv4GatewayOctet2.Value,
                IPv4GatewayOctet3.Value,
                IPv4GatewayOctet4.Value]);
        }
        set
        {
            if (value is null)
            {
                IPv4GatewayOctet1 = null;
                IPv4GatewayOctet2 = null;
                IPv4GatewayOctet3 = null;
                IPv4GatewayOctet4 = null;
            }
            else
            {
                var bytes = value.GetAddressBytes();
                IPv4GatewayOctet1 = bytes[0];
                IPv4GatewayOctet2 = bytes[1];
                IPv4GatewayOctet3 = bytes[2];
                IPv4GatewayOctet4 = bytes[3];
            }
        }
    }


    [ObservableProperty]
    public partial bool DNSv4DHCP { get; set; }

    [ObservableProperty]
    public partial byte? DNSv4PrimaryOctet1 { get; set; }

    [ObservableProperty]
    public partial byte? DNSv4PrimaryOctet2 { get; set; }

    [ObservableProperty]
    public partial byte? DNSv4PrimaryOctet3 { get; set; }

    [ObservableProperty]
    public partial byte? DNSv4PrimaryOctet4 { get; set; }

    public IPAddress? DNSv4PrimaryAddress
    {
        get
        {
            if (DNSv4PrimaryOctet1 is null
                || DNSv4PrimaryOctet2 is null
                || DNSv4PrimaryOctet3 is null
                || DNSv4PrimaryOctet4 is null) return null;
            return new IPAddress([
                DNSv4PrimaryOctet1.Value,
                DNSv4PrimaryOctet2.Value,
                DNSv4PrimaryOctet3.Value,
                DNSv4PrimaryOctet4.Value]);
        }
        set
        {
            if (value is null)
            {
                DNSv4PrimaryOctet1 = null;
                DNSv4PrimaryOctet2 = null;
                DNSv4PrimaryOctet3 = null;
                DNSv4PrimaryOctet4 = null;
            }
            else
            {
                var bytes = value.GetAddressBytes();
                DNSv4PrimaryOctet1 = bytes[0];
                DNSv4PrimaryOctet2 = bytes[1];
                DNSv4PrimaryOctet3 = bytes[2];
                DNSv4PrimaryOctet4 = bytes[3];
            }
        }
    }

    [ObservableProperty]
    public partial byte? DNSv4SecondaryOctet1 { get; set; }

    [ObservableProperty]
    public partial byte? DNSv4SecondaryOctet2 { get; set; }

    [ObservableProperty]
    public partial byte? DNSv4SecondaryOctet3 { get; set; }

    [ObservableProperty]
    public partial byte? DNSv4SecondaryOctet4 { get; set; }

    public IPAddress? DNSv4SecondaryAddress
    {
        get
        {
            if (DNSv4SecondaryOctet1 is null
                || DNSv4SecondaryOctet2 is null
                || DNSv4SecondaryOctet3 is null
                || DNSv4SecondaryOctet4 is null) return null;

            return new IPAddress([
                DNSv4SecondaryOctet1.Value,
                DNSv4SecondaryOctet2.Value,
                DNSv4SecondaryOctet3.Value,
                DNSv4SecondaryOctet4.Value]);
        }
        set
        {
            if (value is null)
            {
                DNSv4SecondaryOctet1 = null;
                DNSv4SecondaryOctet2 = null;
                DNSv4SecondaryOctet3 = null;
                DNSv4SecondaryOctet4 = null;
            }
            else
            {
                var bytes = value.GetAddressBytes();
                DNSv4SecondaryOctet1 = bytes[0];
                DNSv4SecondaryOctet2 = bytes[1];
                DNSv4SecondaryOctet3 = bytes[2];
                DNSv4SecondaryOctet4 = bytes[3];
            }
        }
    }

    public List<MenuItem> DNSv4Presets { get; } = [];

    [ObservableProperty]
    public partial bool IPv6DHCP { get; set; }

    [ObservableProperty]
    [CustomValidation(typeof(SetInterfaceIPDialogModel), nameof(ValidateIPv6Address))]
    public partial string IPv6Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial byte IPv6MaskCIDR { get; set; }

    [ObservableProperty]
    [CustomValidation(typeof(SetInterfaceIPDialogModel), nameof(ValidateIPv6Address))]
    public partial string IPv6Gateway { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool DNSv6DHCP { get; set; }

    [ObservableProperty]
    [CustomValidation(typeof(SetInterfaceIPDialogModel), nameof(ValidateIPv6Address))]
    public partial string DNSv6PrimaryAddress { get; set; } = string.Empty;

    [ObservableProperty]
    [CustomValidation(typeof(SetInterfaceIPDialogModel), nameof(ValidateIPv6Address))]
    public partial string DNSv6SecondaryAddress { get; set; } = string.Empty;

    public List<MenuItem> DNSv6Presets { get; } = [];

    private bool _syncMask = true;

    public SetInterfaceIPDialogModel()
    {
        Init();
        if (Design.IsDesignMode)
        {
            NetworkInterface = NetworkInterfaceBridge.PrimaryInterface!;
            Reload();
        }
    }

    public SetInterfaceIPDialogModel(ISukiDialog dialog, NetworkInterfaceBridge networkInterface) : base(dialog)
    {
        Init();
        NetworkInterface = networkInterface;
        Reload();
    }

    private void Init()
    {
        DNSv4Presets.Clear();
        DNSv6Presets.Clear();
        var providers = DnsProvider.DnsProviders
            .AsValueEnumerable()
            .Where(provider => provider.DNSv4PrimaryAddress.IsValid() && provider.DNSv6PrimaryAddress.IsValid())
            .OrderBy(provider => provider.ProviderName)
            .GroupBy(provider => provider.ProviderName).ToArray();



        foreach (var providerGroup in providers)
        {
            var groupV4MenuItem = new MenuItem
            {
                Header = providerGroup.Key,
            };
            DNSv4Presets.Add(groupV4MenuItem);
            var subMenuV4 = new List<MenuItem>(providers.Length);

            var groupV6MenuItem = new MenuItem
            {
                Header = providerGroup.Key,
            };
            DNSv6Presets.Add(groupV6MenuItem);
            var subMenuV6 = new List<MenuItem>(providers.Length);

            foreach (var provider in providerGroup)
            {
                var menuItemV4 = new MenuItem
                {
                    Header = $"{provider.DnsBlockCategoriesString}\n{provider.DNSv4PrimaryAddress}{(provider.DNSv4SecondaryAddress.IsValid() ? $"\n{provider.DNSv4SecondaryAddress}" : string.Empty)}",
                    Command = SetDNSv4Command,
                    CommandParameter = provider
                };

                if (!string.IsNullOrWhiteSpace(provider.Notes))
                {
                    ToolTip.SetTip(menuItemV4, provider.Notes);
                    ToolTip.SetPlacement(menuItemV4, PlacementMode.Right);
                }

                subMenuV4.Add(menuItemV4);



                var menuItemV6 = new MenuItem
                {
                    Header = $"{provider.DnsBlockCategoriesString}\n{provider.DNSv6PrimaryAddress}{(provider.DNSv6SecondaryAddress.IsValid() ? $"\n{provider.DNSv6SecondaryAddress}" : string.Empty)}",
                    Command = SetDNSv6Command,
                    CommandParameter = provider
                };

                if (!string.IsNullOrWhiteSpace(provider.Notes))
                {
                    ToolTip.SetTip(menuItemV6, provider.Notes);
                    ToolTip.SetPlacement(menuItemV6, PlacementMode.Right);
                }

                subMenuV6.Add(menuItemV6);
            }

            groupV4MenuItem.ItemsSource = subMenuV4;
            groupV6MenuItem.ItemsSource = subMenuV6;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IPv4DHCP))
        {
            DNSv4DHCP = IPv4DHCP;
        }
        else if (e.PropertyName == nameof(DNSv4DHCP))
        {
            if (DNSv4DHCP && !IPv4DHCP)
            {
                DNSv4DHCP = false;
            }
        }
        else if (e.PropertyName is nameof(IPv4MaskOctet1) or nameof(IPv4MaskOctet2) or nameof(IPv4MaskOctet3) or nameof(IPv4MaskOctet4))
        {
            if (!_syncMask) return;
            _syncMask = false;
            var cidr = IPAddressExtensions.MaskToCidr(IPv4Mask);
            IPv4MaskCIDR = (byte)cidr;
            _syncMask = true;
        }
        else if (e.PropertyName == nameof(IPv4MaskCIDR))
        {
            if (!_syncMask) return;
            _syncMask = false;
            IPv4Mask = IPAddressExtensions.CidrToMask(IPv4MaskCIDR);
            _syncMask = true;
        }
        else if (e.PropertyName == nameof(IPv6DHCP))
        {
            DNSv6DHCP = IPv6DHCP;
        }
        else if (e.PropertyName == nameof(DNSv6DHCP))
        {
            if (DNSv6DHCP && !IPv6DHCP)
            {
                DNSv6DHCP = false;
            }
        }
    }

    public void Reload()
    {
        _syncMask = false;
        if (NetworkInterface.SupportsIPv4)
        {
            var ipv4 = NetworkInterface.Properties.UnicastAddresses
                .AsValueEnumerable()
                .LastOrDefault(ip =>
                ip.Address.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4 is not null)
            {
                if (OperatingSystem.IsWindows())
                {
                    IPv4DHCP = ipv4.PrefixOrigin != PrefixOrigin.Manual;
                }
                IPv4Address = ipv4.Address;
                IPv4Mask = ipv4.IPv4Mask;
                IPv4MaskCIDR = (byte)ipv4.PrefixLength;
            }

            var ipv4Gateway =
                NetworkInterface.Properties.GatewayAddresses
                    .AsValueEnumerable()
                    .LastOrDefault(ip =>
                    ip.Address.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4Gateway is not null)
            {
                IPv4Gateway = ipv4Gateway.Address;
            }

            using var dnses = NetworkInterface.Properties.DnsAddresses
                .AsValueEnumerable()
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .ToArrayPool();

            if (dnses.Size >= 1)
            {
                DNSv4PrimaryAddress = dnses.Array[0];
            }

            if (dnses.Size >= 2)
            {
                DNSv4SecondaryAddress = dnses.Array[1];
            }
        }

        if (NetworkInterface.SupportsIPv6)
        {
            var ipv6 = NetworkInterface.Properties.UnicastAddresses
                .AsValueEnumerable()
                .LastOrDefault(ip =>
                ip.Address.AddressFamily == AddressFamily.InterNetworkV6);
            if (ipv6 is not null)
            {
                if (OperatingSystem.IsWindows())
                {
                    IPv6DHCP = ipv6.PrefixOrigin != PrefixOrigin.Manual;
                }

                IPv6Address = ipv6.Address.ToString();
                IPv6MaskCIDR = (byte)ipv6.PrefixLength;
            }

            var ipv6Gateway =
                NetworkInterface.Properties.GatewayAddresses
                    .AsValueEnumerable()
                    .LastOrDefault(ip =>
                    ip.Address.AddressFamily == AddressFamily.InterNetworkV6);

            if (ipv6Gateway is not null)
            {
                IPv6Gateway = ipv6Gateway.Address.ToString();
            }

            using var dnses = NetworkInterface.Properties.DnsAddresses
                .AsValueEnumerable()
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                .ToArrayPool();

            if (dnses.Size >= 1)
            {
                DNSv6PrimaryAddress = dnses.Array[0].ToString();
            }

            if (dnses.Size >= 2)
            {
                DNSv6SecondaryAddress = dnses.Array[1].ToString();
            }
        }

        _syncMask = true;

        _propertiesSnapshot.Snapshot(this);
    }

    [RelayCommand]
    public void ClearIPv4Gateway()
    {
        IPv4Gateway = null;
    }

    [RelayCommand]
    public void SetDNSv4PrimaryFromGateway()
    {
        DNSv4PrimaryAddress = IPv4Gateway;
    }

    [RelayCommand]
    public void SetDNSv4SecondaryFromGateway()
    {
        DNSv4SecondaryAddress = IPv4Gateway;
    }

    [RelayCommand]
    public void ClearDNSv4PrimaryAddress()
    {
        DNSv4PrimaryAddress = null;
    }

    [RelayCommand]
    public void ClearDNSv4SecondaryAddress()
    {
        DNSv4SecondaryAddress = null;
    }

    [RelayCommand]
    public void SetDNSv4(DnsProvider provider)
    {
        DNSv4PrimaryAddress = provider.DNSv4PrimaryAddress.IsValid() ? provider.DNSv4PrimaryAddress : null;
        DNSv4SecondaryAddress = provider.DNSv4SecondaryAddress.IsValid() ? provider.DNSv4SecondaryAddress : null;
    }

    [RelayCommand]
    public void SetDNSv6PrimaryFromGateway()
    {
        DNSv6PrimaryAddress = IPv6Gateway;
    }

    [RelayCommand]
    public void SetDNSv6SecondaryFromGateway()
    {
        DNSv6SecondaryAddress = IPv6Gateway;
    }

    [RelayCommand]
    public void ClearDNSv6PrimaryAddress()
    {
        DNSv6PrimaryAddress = string.Empty;
    }

    [RelayCommand]
    public void ClearDNSv6SecondaryAddress()
    {
        DNSv6SecondaryAddress = string.Empty;
    }

    [RelayCommand]
    public void SetDNSv6(DnsProvider provider)
    {
        DNSv6PrimaryAddress = provider.DNSv6PrimaryAddress.IsValid() ? provider.DNSv6PrimaryAddress.ToString() : string.Empty;
        DNSv6SecondaryAddress = provider.DNSv6SecondaryAddress.IsValid() ? provider.DNSv6SecondaryAddress.ToString() : string.Empty;
    }

    protected override async Task<bool> ApplyInternal()
    {
        if (!DNSv4PrimaryAddress?.IsValid() ?? false) DNSv4PrimaryAddress = null;
        if (!DNSv4SecondaryAddress?.IsValid() ?? false) DNSv4SecondaryAddress = null;
        if (Equals(DNSv4PrimaryAddress, DNSv4SecondaryAddress)) DNSv4SecondaryAddress = null;
        if (DNSv4PrimaryAddress is null && DNSv4SecondaryAddress is not null)
        {
            DNSv4PrimaryAddress = DNSv4SecondaryAddress;
            DNSv4SecondaryAddress = null;
        }

        if (string.IsNullOrWhiteSpace(DNSv6PrimaryAddress) && !string.IsNullOrWhiteSpace(DNSv6SecondaryAddress))
        {
            DNSv6PrimaryAddress = DNSv6SecondaryAddress;
            DNSv6SecondaryAddress = string.Empty;
        }
        var commands = new List<string>();
        bool requireAdminRights = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        using var writer = Utf8String.CreateWriter(out var zsb);
        zsb.AppendLine("IP successfully assigned.");


        if (NetworkInterface.SupportsIPv4)
        {
            if (IPv4DHCP)
            {
                if (_propertiesSnapshot.IsChanged(IPv4DHCP))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        commands.Add($"netsh interface ipv4 set address name=\"{NetworkInterface.Interface.Name}\" source=dhcp");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        commands.Add($"networksetup -setdhcp \"{NetworkInterface.Interface.Name}\"");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.method auto");
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.gateway \"\"");
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.address \"\"");
                    }

                    zsb.AppendLine("IPv4=DHCP");
                }
            }
            else
            {
                if (_propertiesSnapshot.IsChanged(IPv4DHCP)
                    || _propertiesSnapshot.IsChanged(IPv4Address)
                    || _propertiesSnapshot.IsChanged(IPv4Mask)
                    || _propertiesSnapshot.IsChanged(IPv4Gateway)
                    )
                {
                    if (OperatingSystem.IsWindows())
                    {
                        commands.Add($"netsh interface ipv4 set address name=\"{NetworkInterface.Interface.Name}\" static {IPv4Address} {IPv4Mask} {IPv4Gateway}");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        commands.Add($"networksetup -setmanual \"{NetworkInterface.Interface.Name}\" {IPv4Address} {IPv4Mask} {IPv4Gateway}");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.addresses {IPv4Address}/{IPv4MaskCIDR} ipv4.gateway \"{IPv4Gateway}\" ipv4.method manual");
                    }

                    zsb.AppendLine($"IPv4={IPv4Address}/{IPv4MaskCIDR} {IPv4Gateway}");
                }
            }

            if (DNSv4DHCP)
            {
                if (_propertiesSnapshot.IsChanged(DNSv4DHCP))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        commands.Add($"netsh interface ipv4 set dnsservers name=\"{NetworkInterface.Interface.Name}\" source=dhcp");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        commands.Add($"networksetup -setdnsservers \"{NetworkInterface.Interface.Name}\" empty");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.ignore-auto-dns no");
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.dns \"\"");
                    }

                    zsb.AppendLine("DNSv4=DHCP");
                }
            }
            else
            {
                if (_propertiesSnapshot.IsChanged(DNSv4DHCP)
                    || _propertiesSnapshot.IsChanged(DNSv4PrimaryAddress)
                    || _propertiesSnapshot.IsChanged(DNSv4SecondaryAddress))
                {
                    if (DNSv4PrimaryAddress is not null)
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            commands.Add($"netsh interface ipv4 set dnsservers name=\"{NetworkInterface.Interface.Name}\" static {DNSv4PrimaryAddress} validate=no");
                        }
                        else if (OperatingSystem.IsMacOS())
                        {
                            commands.Add($"networksetup -setdnsservers \"{NetworkInterface.Interface.Name}\" {DNSv4PrimaryAddress} {DNSv4SecondaryAddress}");
                        }
                        else if (OperatingSystem.IsLinux())
                        {
                            commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.dns \"{DNSv4PrimaryAddress} {DNSv4SecondaryAddress}\"");
                            commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv4.ignore-auto-dns yes");
                        }

                        zsb.AppendLine($"DNSv4={DNSv4PrimaryAddress}");
                    }

                    if (DNSv4SecondaryAddress is not null)
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            commands.Add($"netsh interface ipv4 add dnsservers name=\"{NetworkInterface.Interface.Name}\" {DNSv4SecondaryAddress} validate=no");
                        }

                        zsb.AppendLine($"DNSv4={DNSv4SecondaryAddress}");
                    }
                }

            }
        }

        if (NetworkInterface.SupportsIPv6)
        {
            if (IPv6DHCP)
            {
                if (_propertiesSnapshot.IsChanged(IPv6DHCP))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        commands.AddRange([
                            $"netsh interface ipv6 set interface \"{NetworkInterface.Interface.Name}\" dhcp=enabled",
                            $"netsh interface ipv6 set interface \"{NetworkInterface.Interface.Name}\" routerdiscovery=enabled"
                        ]);
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        commands.Add($"networksetup -setv6automatic \"{NetworkInterface.Interface.Name}\"");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        commands.AddRange([
                            $"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.method auto",
                            $"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.gateway \"\"",
                            $"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.address \"\""
                        ]);
                    }

                    zsb.AppendLine("IPv6=DHCP");
                }
            }
            else
            {
                if (_propertiesSnapshot.IsChanged(IPv6DHCP)
                    || _propertiesSnapshot.IsChanged(IPv6Address)
                    || _propertiesSnapshot.IsChanged(IPv6MaskCIDR)
                    || _propertiesSnapshot.IsChanged(IPv6Gateway)
                    )
                {
                    if (OperatingSystem.IsWindows())
                    {
                        commands.Add($"netsh interface ipv6 set address name=\"{NetworkInterface.Interface.Name}\" static {IPv6Address} {IPv6MaskCIDR} {IPv6Gateway}");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        commands.Add($"networksetup -setv6manual \"{NetworkInterface.Interface.Name}\" {IPv6Address} {IPv6MaskCIDR} {IPv6Gateway}");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.addresses {IPv6Address}/{IPv6MaskCIDR} ipv6.gateway \"{IPv6Gateway}\" ipv6.method manual");
                    }

                    zsb.AppendLine($"IPv6={IPv6Address}/{IPv6MaskCIDR} {IPv6Gateway}");
                }
            }

            if (DNSv6DHCP)
            {
                if (_propertiesSnapshot.IsChanged(DNSv6DHCP))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        commands.Add($"netsh interface ipv6 set dnsservers name=\"{NetworkInterface.Interface.Name}\" source=dhcp");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        commands.Add($"networksetup -setdnsservers \"{NetworkInterface.Interface.Name}\" empty");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.ignore-auto-dns no");
                        commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.dns \"\"");
                    }

                    zsb.AppendLine("DNSv6=DHCP");
                }
            }
            else
            {
                if (_propertiesSnapshot.IsChanged(DNSv6DHCP)
                    || _propertiesSnapshot.IsChanged(DNSv6PrimaryAddress)
                    || _propertiesSnapshot.IsChanged(DNSv6SecondaryAddress))
                {
                    if (DNSv6PrimaryAddress is not null)
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            commands.Add($"netsh interface ipv6 set dnsservers name=\"{NetworkInterface.Interface.Name}\" static {DNSv6PrimaryAddress} validate=no");
                        }
                        else if (OperatingSystem.IsMacOS())
                        {
                            commands.Add($"networksetup -setdnsservers \"{NetworkInterface.Interface.Name}\" {DNSv6PrimaryAddress} {DNSv6SecondaryAddress}");
                        }
                        else if (OperatingSystem.IsLinux())
                        {
                            commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.dns \"{DNSv6PrimaryAddress} {DNSv6SecondaryAddress}\"");
                            commands.Add($"nmcli device modify \"{NetworkInterface.Interface.Name}\" ipv6.ignore-auto-dns yes");
                        }

                        zsb.AppendLine($"DNSv6={DNSv6PrimaryAddress}");
                    }

                    if (DNSv6SecondaryAddress is not null)
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            commands.Add($"netsh interface ipv6 add dnsservers name=\"{NetworkInterface.Interface.Name}\" {DNSv6SecondaryAddress} validate=no");
                        }

                        zsb.AppendLine($"DNSv6={DNSv6SecondaryAddress}");
                    }
                }

            }

        }

        if (commands.Count == 0)
        {
            App.ShowToast(NotificationType.Information,
                App.Localization.Format("Network.IpAssignment.Title", NetworkInterface.Interface.Name),
                App.Localization["Network.IpAssignment.NoChanges"]);
            return true;
        }

        if (OperatingSystem.IsLinux())
        {
            //commands.Add($"nmcli device down \"{NetworkInterface.Interface.Name}\"");
            //commands.Add($"nmcli device up \"{NetworkInterface.Interface.Name}\"");
            // restart the NetworkManager service (if you don't want be disconnected) :
            commands.Add("service NetworkManager restart");
        }

        zsb.Flush();
        var success = await ProcessXExtensions.ExecuteHandled(commands, new ProcessXToast()
        {
            Title = App.Localization.Format("Network.IpAssignment.Title", NetworkInterface.Interface.Name),
            SuccessGenericMessage = writer.ToString(),
            ErrorGenericMessage = App.Localization["Network.IpAssignment.Error"],
        }, requireAdminRights);

        NetworkInterface.Reset();

        return success;
    }

    public static ValidationResult? ValidateIPv6Address(string ipV6Address, ValidationContext context)
    {
        if (context.ObjectInstance is not SetInterfaceIPDialogModel) return new ValidationResult("Invalid type on the validator.");
        if (string.IsNullOrWhiteSpace(ipV6Address)) return ValidationResult.Success;
        if (!IPAddress.TryParse(ipV6Address, out var address)) return new ValidationResult($"The {context.DisplayName} is not a valid IPv6 address.");
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return new ValidationResult($"The {context.DisplayName} is a IPv4, must be a IPv6 address.");
        return ValidationResult.Success;
    }
}
