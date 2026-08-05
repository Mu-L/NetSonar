using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace NetSonar.Avalonia.Localization;

/// <summary>
/// Provides localized strings to views and view models.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets a localized string by its resource key.
    /// </summary>
    string this[string key] { get; }

    /// <summary>
    /// Gets or sets the culture used to resolve localized resources.
    /// </summary>
    CultureInfo Culture { get; set; }

    /// <summary>
    /// Gets the cultures offered by the application.
    /// </summary>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    /// Gets a localized string by its resource key.
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Gets and formats a localized string using the current formatting culture.
    /// </summary>
    string Format(string key, params object?[] arguments);
}
