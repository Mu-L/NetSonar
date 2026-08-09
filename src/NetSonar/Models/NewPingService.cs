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
        var isIpAddressLiteral = IPAddressExtensions.TryParseLiteral(ipAddressOrUrl, out _);

        if (usesSocketEndpoint)
        {
            var hasExplicitPort = HasExplicitPort(ipAddressOrUrl, isIpAddressLiteral);
            if (service.ProtocolType is ServiceProtocolType.TCP or ServiceProtocolType.UDP
                && !hasExplicitPort)
            {
                return new ValidationResult($"The {service.ProtocolType} protocol must contain a port number.");
            }

            if (!IsValidSocketAddress(ipAddressOrUrl, service.ProtocolType, isIpAddressLiteral, hasExplicitPort))
                return new ValidationResult("The string is not a valid IP address, hostname, or URL.");

            return ValidationResult.Success;
        }

        if (!isIpAddressLiteral
            && !Uri.IsWellFormedUriString(ipAddressOrUrl, UriKind.RelativeOrAbsolute))
        {
            return new ValidationResult("The string is not a valid IP address, hostname, or URL.");
        }

        if (service.ProtocolType is ServiceProtocolType.ICMP)
        {
            if (ipAddressOrUrl.Contains(':') && !isIpAddressLiteral)
                return new ValidationResult($"The {service.ProtocolType} protocol must not contain a port number.");
        }

        if (service.ProtocolType == ServiceProtocolType.ICMP)
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

    private static bool HasExplicitPort(string address, bool isIpAddressLiteral)
    {
        if (isIpAddressLiteral) return false;
        if (!address.StartsWith('[')) return address.Contains(':');

        var closingBracket = address.IndexOf(']');
        return closingBracket >= 0
               && closingBracket + 1 < address.Length
               && address[closingBracket + 1] == ':';
    }

    private static bool IsValidSocketAddress(
        string address,
        ServiceProtocolType protocolType,
        bool isIpAddressLiteral,
        bool hasExplicitPort)
    {
        if (address.Contains('/') || address.Contains('?') || address.Contains('#') || address.Contains('@'))
            return false;

        if (isIpAddressLiteral) return true;

        if (IPEndPoint.TryParse(address, out var ipEndPoint))
            return !hasExplicitPort || ipEndPoint.Port > IPEndPoint.MinPort;

        var scheme = protocolType is ServiceProtocolType.TCP
            or ServiceProtocolType.TLS
            or ServiceProtocolType.SSH
            or ServiceProtocolType.SMTP
            or ServiceProtocolType.IMAP
            or ServiceProtocolType.MQTT
            ? "tcp"
            : "udp";
        return Uri.TryCreate($"{scheme}://{address}", UriKind.Absolute, out var endpointUri)
               && !string.IsNullOrWhiteSpace(endpointUri.IdnHost)
               && (!hasExplicitPort
                   || endpointUri.Port is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort);
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
