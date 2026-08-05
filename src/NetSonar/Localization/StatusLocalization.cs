using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;

namespace NetSonar.Avalonia.Localization;

public static class StatusLocalization
{
    public static string GetText(object? status)
    {
        if (status is null) return App.Localization["Status.Unknown"];

        var statusName = status.ToString();
        if (string.IsNullOrWhiteSpace(statusName)) return App.Localization["Status.Unknown"];

        var typedKey = $"Status.{status.GetType().Name}.{statusName}";
        var typedStatus = GetLocalizedValue(typedKey);
        if (typedStatus is not null) return typedStatus;

        var localizationKey = $"Status.{statusName}";
        var localizedStatus = GetLocalizedValue(localizationKey);
        if (localizedStatus is not null) return localizedStatus;

        return status switch
        {
            HttpStatusCode httpStatus => App.Localization.Format("Status.HttpFallback", (int)httpStatus),
            SocketError socketError => App.Localization.Format("Status.SocketErrorFallback", (int)socketError),
            HttpRequestError => App.Localization["Status.HttpRequestErrorFallback"],
            WebSocketError => App.Localization["Status.WebSocketErrorFallback"],
            SslProtocols => FormatTlsProtocol(statusName),
            Enum enumStatus => App.Localization.Format(
                "Status.ErrorFallback",
                Convert.ToInt64(enumStatus, CultureInfo.InvariantCulture)),
            _ => statusName
        };
    }

    private static string? GetLocalizedValue(string key)
    {
        var value = App.Localization[key];
        return string.Equals(value, key, StringComparison.Ordinal) ? null : value;
    }

    private static string FormatTlsProtocol(string statusName) => statusName switch
    {
        "Tls" => "TLS 1.0",
        "Tls11" => "TLS 1.1",
        "Tls12" => "TLS 1.2",
        "Tls13" => "TLS 1.3",
        _ => statusName
    };
}
