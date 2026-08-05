using Avalonia.Rendering.Composition;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using System.ComponentModel;

namespace NetSonar.Avalonia.ViewModels;

public abstract partial class PageViewModelBase : ViewModelBase
{
    protected PageViewModelBase()
    {
        App.Localization.PropertyChanged += LocalizationOnPropertyChanged;
    }

    private void LocalizationOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(App.Localization.Culture)) OnPropertyChanged(nameof(DisplayName));
    }

    /// <summary>
    /// The index of the page.
    /// </summary>
    public abstract int Index { get; }

    /// <summary>
    /// The display name of the page.
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// The icon of the page.
    /// </summary>
    public abstract MaterialIconKind Icon { get; }

    /// <summary>
    /// The visibility of the page on the side menu.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVisibleOnSideMenu { get; protected set; } = true;

    /// <summary>
    /// Set to true to auto hide the page on the side menu.
    /// </summary>
    public virtual bool AutoHideOnSideMenu => false;


    protected internal override void OnLoaded()
    {
        if (AutoHideOnSideMenu) IsVisibleOnSideMenu = true;
        base.OnLoaded();
    }

    protected internal override void OnUnloaded()
    {
        if (AutoHideOnSideMenu) IsVisibleOnSideMenu = false;
        base.OnUnloaded();
    }
}
