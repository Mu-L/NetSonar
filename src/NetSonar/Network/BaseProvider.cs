using System;

namespace NetSonar.Avalonia.Network;

public abstract record BaseProvider
{
    /// <summary>
    /// The name of the DNS provider.
    /// </summary>
    public required string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// The notes of the DNS.
    /// </summary>
    public string Notes { get; init; } = string.Empty;

    /// <summary>
    /// The formated description.
    /// </summary>
    public virtual string FormatedDescription
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Notes))
            {
                return ProviderName;
            }
            else
            {
                return Notes.StartsWith(ProviderName, StringComparison.OrdinalIgnoreCase)
                    ? Notes
                    : $"{ProviderName}: {Notes}";
            }
        }
    }
}