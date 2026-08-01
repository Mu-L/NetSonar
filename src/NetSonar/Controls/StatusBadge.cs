using System;
using Avalonia;
using Avalonia.Controls;

namespace NetSonar.Avalonia.Controls;

public enum StatusBadgeAppearance
{
    Primary,
    Accent,
    Information,
    Success,
    Warning,
    Danger,
}

public sealed class StatusBadge : ContentControl
{
    public static readonly StyledProperty<StatusBadgeAppearance> AppearanceProperty =
        AvaloniaProperty.Register<StatusBadge, StatusBadgeAppearance>(
            nameof(Appearance),
            StatusBadgeAppearance.Information);

    public StatusBadgeAppearance Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(StatusBadge);
}
