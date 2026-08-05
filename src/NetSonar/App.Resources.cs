using Avalonia.Media;
using NetSonar.Avalonia.Localization;

namespace NetSonar.Avalonia;

public partial class App
{
    public static ILocalizationService Localization { get; } = new LocalizationService();

    public static Color SukiTextResource
    {
        get
        {
            if (Current is null) return default;
            if (!Current.TryGetResource("SukiText", Theme.ActiveBaseTheme, out var value)) return default;
            if (value is not Color color) return default;
            return color;
        }
    }
}
