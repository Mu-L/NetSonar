using System.Globalization;

namespace NetSonar.Avalonia.Extensions;

public static class ParseExtensions
{
    /// <summary>
    /// Tries to parse a double value from a string using the current culture and invariant culture.
    /// </summary>
    /// <param name="value">The string representation of the number to parse.</param>
    /// <param name="result">When this method returns, contains the double value equivalent of the number contained in <paramref name="value"/>, if the conversion succeeded, or zero if the conversion failed.</param>
    /// <param name="numberStyles">A bitwise combination of enumeration values that indicates the style elements that can be present in <paramref name="value"/>.</param>
    /// <returns><c>true</c> if <paramref name="value"/> was converted successfully; otherwise, <c>false</c>.</returns>
    public static bool TryParseLocalizedDouble(string? value, out double result,
        NumberStyles numberStyles = NumberStyles.Float)
    {
        return double.TryParse(value, numberStyles, CultureInfo.CurrentCulture, out result)
               || double.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out result);
    }

    public static double ParseLocalizedDoubleOrDefault(string value, double defaultValue,
        NumberStyles numberStyles = NumberStyles.Float)
    {
        return TryParseLocalizedDouble(value, out var result, numberStyles) ? result : defaultValue;
    }
}
