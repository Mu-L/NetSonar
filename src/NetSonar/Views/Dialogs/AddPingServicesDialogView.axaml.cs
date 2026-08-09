using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NetSonar.Avalonia.Controls;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.ViewModels.Dialogs;

namespace NetSonar.Avalonia.Views.Dialogs;

public partial class AddPingServicesDialogView : UserControlBase
{
    public AddPingServicesDialogView()
    {
        InitializeComponent();
        ServicesGrid.ExtendDataGridShortcuts();
    }

    /// <summary>
    /// While this dialog is open, Ctrl+V is caught at the window level (tunnelling phase, before the
    /// DataGrid's built-in cell-paste) so it always runs the batch clipboard import — regardless of
    /// where focus currently sits inside the dialog. While a text cell is being edited (focus is inside
    /// a TextBox) the key is left alone so pasting text into a cell keeps working.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
            topLevel.AddHandler(InputElement.KeyDownEvent, OnDialogKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
            topLevel.RemoveHandler(InputElement.KeyDownEvent, OnDialogKeyDown);
        base.OnUnloaded(e);
    }

    private void OnDialogKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Handled
            && e.Key == Key.V
            && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0
            && TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not TextBox
            && DataContext is AddPingServicesDialogModel vm
            && vm.PasteFromClipboardCommand.CanExecute(null))
        {
            vm.PasteFromClipboardCommand.Execute(null);
            e.Handled = true;
        }
    }
}
