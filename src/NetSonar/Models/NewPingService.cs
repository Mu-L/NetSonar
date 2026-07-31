﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.Network;

namespace NetSonar.Avalonia.Models;

public partial class NewPingService : ObservableValidatorExtended
{

    [ObservableProperty] public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IpAddressOrUrl))]
    public partial ServiceProtocolType ProtocolType { get; set; } = ServiceProtocolType.ICMP;

    [ObservableProperty]
    [Required]
    [StringLength(1024, MinimumLength = 3)]
    [CustomValidation(typeof(NewPingService), nameof(ValidateIpAddressUrl))]
    public partial string IpAddressOrUrl { get; set; } = string.Empty;

    [ObservableProperty] public partial string Description { get; set; } = string.Empty;

    [ObservableProperty] public partial string Group { get; set; } = string.Empty;

    [ObservableProperty] public partial double PingEverySeconds { get; set; } = PingableService.DefaultPingEverySeconds;

    [ObservableProperty] public partial double TimeoutSeconds { get; set; } = PingableService.DefaultTimeoutSeconds;

    [ObservableProperty] public partial int BufferSize { get; set; } = PingableService.DefaultBufferSize;

    [ObservableProperty] public partial byte Ttl { get; set; } = PingableService.DefaultTtl;

    [ObservableProperty] public partial bool DontFragment { get; set; }

    public NewPingService()
    {
        PingEverySeconds = App.AppSettings.PingServices.DefaultPingEverySeconds;
        TimeoutSeconds = App.AppSettings.PingServices.DefaultTimeoutSeconds;
        BufferSize = App.AppSettings.PingServices.DefaultBufferSize;
        Ttl = App.AppSettings.PingServices.DefaultTtl;
        DontFragment = App.AppSettings.PingServices.DefaultDontFragment;
    }

    public NewPingService(ServiceProtocolType protocolType, string ipAddressOrUrl, string description = "", string group = "") : this()
    {
        ProtocolType = protocolType;
        IpAddressOrUrl = ipAddressOrUrl;
        Description = description;
        Group = group;
    }

    public NewPingService(ServiceProtocolType protocolType, IPAddress ipAddress, string description = "", string group = "")
        : this(protocolType, ipAddress.ToString(), description, group)
    {
    }

    public NewPingService(PingableService service)
    {
        IsEnabled = service.IsEnabled;
        ProtocolType = service.ProtocolType;
        IpAddressOrUrl = service.IpAddressOrUrl;
        Description = service.Description;
        Group = service.Group;
        PingEverySeconds = service.PingEverySeconds;
        TimeoutSeconds = service.TimeoutSeconds;
        BufferSize = service.BufferSize;
        Ttl = service.Ttl;
        DontFragment = service.DontFragment;
    }

    public static ValidationResult? ValidateIpAddressUrl(string ipAddressOrUrl, ValidationContext context)
    {
        if (context.ObjectInstance is not NewPingService service)
            return new ValidationResult("Invalid type on the validator.");

        var usesSocketEndpoint = service.ProtocolType is ServiceProtocolType.TCP
            or ServiceProtocolType.UDP
            or ServiceProtocolType.TLS
            or ServiceProtocolType.DNS
            or ServiceProtocolType.NTP
            or ServiceProtocolType.SSH
            or ServiceProtocolType.SMTP
            or ServiceProtocolType.IMAP
            or ServiceProtocolType.MQTT
            or ServiceProtocolType.STUN
            or ServiceProtocolType.SIP;
        var isValidAddress = IPEndPoint.TryParse(ipAddressOrUrl, out _);

        if (!isValidAddress && usesSocketEndpoint)
        {
            var scheme = service.ProtocolType is ServiceProtocolType.TCP
                or ServiceProtocolType.TLS
                or ServiceProtocolType.SSH
                or ServiceProtocolType.SMTP
                or ServiceProtocolType.IMAP
                or ServiceProtocolType.MQTT
                ? "tcp"
                : "udp";
            isValidAddress = Uri.TryCreate($"{scheme}://{ipAddressOrUrl}", UriKind.Absolute, out var endpointUri)
                             && !string.IsNullOrWhiteSpace(endpointUri.IdnHost);
        }

        if (!isValidAddress
            && !Uri.IsWellFormedUriString(ipAddressOrUrl, UriKind.RelativeOrAbsolute))
        {
            return new ValidationResult("The string is not a valid IP address, hostname, or URL.");
        }

        if (service.ProtocolType is ServiceProtocolType.ICMP)
        {
            if (ipAddressOrUrl.Contains(':'))
                return new ValidationResult($"The {service.ProtocolType} protocol must not contain a port number.");
        }

        if (service.ProtocolType is ServiceProtocolType.TCP or ServiceProtocolType.UDP)
        {
            if (!ipAddressOrUrl.Contains(':'))
                return new ValidationResult($"The {service.ProtocolType} protocol must contain a port number.");
        }

        if (service.ProtocolType == ServiceProtocolType.ICMP || usesSocketEndpoint)
        {
            if (ipAddressOrUrl.Contains('/'))
                return new ValidationResult(
                    $"The address must not contain path separator '/' for the {service.ProtocolType} protocol.");
        }

        if (service.ProtocolType == ServiceProtocolType.WebSocket
            && ipAddressOrUrl.Contains("://", StringComparison.Ordinal)
            && (!Uri.TryCreate(ipAddressOrUrl, UriKind.Absolute, out var webSocketUri)
                || webSocketUri.Scheme is not ("ws" or "wss")))
        {
            return new ValidationResult("The WebSocket address must use the ws:// or wss:// scheme.");
        }

        return ValidationResult.Success;
    }

    protected bool Equals(NewPingService other)
    {
        return ProtocolType == other.ProtocolType && IpAddressOrUrl == other.IpAddressOrUrl;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((NewPingService)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)ProtocolType, IpAddressOrUrl);
    }
}
