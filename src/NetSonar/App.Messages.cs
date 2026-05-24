using Avalonia.Controls.Notifications;
using Cysharp.Diagnostics;
using NetSonar.Avalonia.Controls;
using NetSonar.Avalonia.Settings;
using SukiUI.Toasts;
using System;
using System.Collections.Generic;
using NetSonar.Avalonia.Extensions;

namespace NetSonar.Avalonia;

public partial class App
{
    /// <summary>
    /// Creates a toast notification with the specified title, content, and buttons.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="autoDismiss"></param>
    /// <param name="buttons"></param>
    /// <returns></returns>
    public static SukiToastBuilder CreateToast(string? title, string content, bool autoDismiss, params IEnumerable<ToastActionButton> buttons)
    {
        if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            CreateToastInternal();
        }
        else
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(CreateToastInternal);
        }

        void CreateToastInternal()
        {
            var toast = ToastManager.CreateToast().WithContent(content);

            if (autoDismiss)
            {
                toast.SetCanDismissByClicking(true);
                toast.SetDismissAfter(RuntimeGlobals.ToastDismissAfter);
            }

            if (!string.IsNullOrWhiteSpace(title)) toast.SetTitle(title);
            foreach (var actionButton in buttons)
            {
                toast.AddActionButton(actionButton.ButtonContent, actionButton.OnClicked, actionButton.DismissOnClick,
                    actionButton.Styles);
            }

            return toast;
        }
    }

    public static SukiToastBuilder CreateToast(string? title, string content, params IEnumerable<ToastActionButton> buttons)
    {
        return CreateToast(title, content, true, buttons);
    }

    public static SukiToastBuilder CreateToast(NotificationType type, string? title, string content, bool autoDismiss, params IEnumerable<ToastActionButton> buttons)
    {
        var toast = CreateToast(title, content, autoDismiss, buttons);
        toast.SetType(type);
        return toast;
    }

    public static SukiToastBuilder CreateToast(NotificationType type, string? title, string content, params IEnumerable<ToastActionButton> buttons)
    {
        return CreateToast(type, title, content, true, buttons);
    }

    public static void ShowToast(string title, string content, bool autoDismiss = true, params IEnumerable<ToastActionButton> buttons)
    {
        CreateToast(title, content, autoDismiss, buttons).Queue();
    }

    public static void ShowToast(string title, string content, params IEnumerable<ToastActionButton> buttons)
    {
        CreateToast(title, content, true, buttons).Queue();
    }

    public static void ShowToast(NotificationType type, string? title, string content, bool autoDismiss, params IEnumerable<ToastActionButton> buttons)
    {
        CreateToast(type, title, content, autoDismiss, buttons).Queue();
    }

    public static void ShowToast(NotificationType type, string? title, string content, params IEnumerable<ToastActionButton> buttons)
    {
        CreateToast(type, title, content, true, buttons).Queue();
    }

    public static void ShowExceptionToast(string? title, string content, bool autoDismiss = true, params IEnumerable<ToastActionButton> buttons)
    {
        CreateToast(NotificationType.Error,
                title,
                content,
                autoDismiss,
                buttons)
            .Queue();
    }

    public static void ShowExceptionToast(string? title, string content, params IEnumerable<ToastActionButton> buttons)
    {
        ShowExceptionToast(title, content, true, buttons);
    }

    public static void ShowExceptionToast(Exception ex, string? title = null, string? content = null, bool autoDismiss = true, params IEnumerable<ToastActionButton> buttons)
    {
        CreateToast(NotificationType.Error,
                title,
                string.IsNullOrWhiteSpace(content) ? ex.Message : $"{content}{Environment.NewLine}{ex.Message}",
                autoDismiss,
                buttons)
            .Queue();
    }

    public static void ShowExceptionToast(ProcessErrorException ex, string? title = null, string? content = null, bool autoDismiss = true, params IEnumerable<ToastActionButton> buttons)
    {
        CreateToast(NotificationType.Error,
                title,
                string.IsNullOrWhiteSpace(content) ? $"{ex.GetOutputError()} ({ex.ExitCode})" : $"{content}{Environment.NewLine}{ex.GetOutputError()} ({ex.ExitCode})",
                autoDismiss,
                buttons)
            .Queue();
    }
}