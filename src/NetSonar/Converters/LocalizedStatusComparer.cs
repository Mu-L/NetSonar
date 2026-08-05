using System.Collections;
using System.Globalization;
using NetSonar.Avalonia.Localization;
using NetSonar.Avalonia.Network;

namespace NetSonar.Avalonia.Converters;

public sealed class LocalizedStatusComparer : IComparer
{
    public int Compare(object? x, object? y)
    {
        var culture = App.Localization.Culture;
        return culture.CompareInfo.Compare(
            StatusLocalization.GetText(GetStatus(x)),
            StatusLocalization.GetText(GetStatus(y)),
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
    }

    private static object? GetStatus(object? value) => value switch
    {
        PingableService service => service.LastStatus,
        BasePingReply reply => reply.Status,
        _ => value
    };
}
