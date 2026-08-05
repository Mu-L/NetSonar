# Localization

English strings live in `Strings.resx` and are the neutral fallback. The included catalogs are listed
in the [project README](../../../README.md#languages). Add another translation as
`Strings.<culture>.resx` and add its `CultureInfo` to `LocalizationService.Cultures` so it can be
offered by the language selector. A missing translated key falls back to English; a key missing from
every catalog is displayed as the key itself.

Use a localized value in AXAML:

```xml
xmlns:localization="clr-namespace:NetSonar.Avalonia.Localization"
Text="{localization:Translate Navigation.Settings}"
```

Inject `ILocalizationService` into a view model and use its indexer or formatting helper:

```csharp
public ExampleViewModel(ILocalizationService localization)
{
    Title = localization["Navigation.Settings"];
    Message = localization.Format("Example.Count", count);
}
```

Assign `Culture` to switch languages at runtime. Existing `Translate` bindings refresh automatically.
