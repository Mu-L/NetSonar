using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.Settings;

namespace NetSonar.Avalonia.ViewModels;

public partial class ViewModelBase : ObservableValidatorExtended
{
    public static RuntimeGlobals RuntimeGlobals => App.RuntimeGlobals;
    public static AppSettings AppSettings => App.AppSettings;

    public static TopLevel TopLevel => App.TopLevel;

    public static SukiDialogManager DialogManager => App.DialogManager;
    public static SukiToastManager ToastManager => App.ToastManager;

    /*public static TopLevel? TopLevel
    {
        get
        {
            if (Application.Current is null) return null;
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }

            return null;
        }
    }*/

    /// <summary>
    /// Called when the control finishes initialization.
    /// </summary>
    protected internal virtual void OnInitialized() { }

    /// <summary>
    /// Called when the control finishes load.
    /// </summary>
    protected internal virtual void OnLoaded(){}

    /// <summary>
    /// Called when the control finishes unload.
    /// </summary>
    protected internal virtual void OnUnloaded(){}

    [RelayCommand]
    public static Task<bool> LaunchUriAsync(string url)
    {
        return App.LaunchUriAsync(url);
    }

    [RelayCommand]
    public static Task CopyToClipboardWithToast(object? text)
    {
        return App.CopyToClipboard(text?.ToString(), true);
    }

    [RelayCommand]
    public static Task CopyToClipboardWithoutToast(object? text)
    {
        return App.CopyToClipboard(text?.ToString(), false);
    }


    public static SukiDialogBuilder CreateMessageBox(NotificationType type, string? title = null, object? content = null)
    {
        return CreateMessageBox(title, content).OfType(type);
    }

    public static SukiDialogBuilder CreateMessageBox(string? title = null, object? content = null)
    {
        var dialog = DialogManager.CreateDialog();
        if (title is not null)
        {
            dialog.SetTitle(title);
        }
        if (content is not null)
        {
            dialog.SetContent(content);
        }

        return dialog;
    }

    public static SukiDialogBuilder CreateMessageBoxYesNo(NotificationType type, string? title, object? content,
        Action<ISukiDialog> yesAction, Action<ISukiDialog>? noAction = null)
    {
        var dialog = CreateMessageBox(type, title, content)
            .WithActionButton(App.Localization["Common.Yes"], yesAction, true)
            .WithActionButton(App.Localization["Common.No"], noAction ?? (_ => { }), true);


        if (dialog.Dialog.ActionButtons[0] is Button yesButton)
        {
            yesButton.IsDefault = true;
            yesButton.HotKey = new KeyGesture(Key.Enter);
        }
        if (dialog.Dialog.ActionButtons[1] is Button noButton)
        {
            noButton.IsCancel = true;
        }

        return dialog;
    }

    public static SukiDialogBuilder CreateMessageBoxYesNoCancel(NotificationType type, string? title, object? content,
        Action<ISukiDialog> yesAction, Action<ISukiDialog>? noAction = null, Action<ISukiDialog>? cancelAction = null)
    {

        var dialog = CreateMessageBox(type, title, content)
            .WithActionButton(App.Localization["Common.Yes"], yesAction, true)
            .WithActionButton(App.Localization["Common.No"], noAction ?? (_ => { }), true)
            .WithActionButton(App.Localization["Common.Cancel"], cancelAction ?? (_ => { }), true);

        if (dialog.Dialog.ActionButtons[0] is Button yesButton)
        {
            yesButton.IsDefault = true;
            yesButton.HotKey = KeyGesture.Parse("Enter");
        }
        if (dialog.Dialog.ActionButtons[2] is Button cancelButton)
        {
            cancelButton.IsCancel = true;
        }

        return dialog;
    }
}
