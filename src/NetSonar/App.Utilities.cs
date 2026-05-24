using Avalonia.Controls.Notifications;
using System.Threading.Tasks;
using System.Threading;
using System;
using Avalonia.Input.Platform;

namespace NetSonar.Avalonia;

public partial class App
{
    /// <summary>
    /// Gets the debug parallel options.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ParallelOptions GetDebugParallelOptions(CancellationToken cancellationToken = default)
    {
        return new ParallelOptions
        {
            MaxDegreeOfParallelism = 1,
            CancellationToken = cancellationToken
        };
    }

    /// <summary>
    /// Gets the parallel options.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ParallelOptions GetParallelOptions(CancellationToken cancellationToken = default)
    {
        var maxDegreeOfParallelism = AppSettings.MaxDegreeOfParallelism;
        if (maxDegreeOfParallelism == 0) maxDegreeOfParallelism = -1;
        return new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };
    }

    public static Task<bool> LaunchUriAsync(string url)
    {
        var launcher = TopLevel.Launcher;
        if (!url.StartsWith("http")) url = $"http://{url}";
        return launcher.LaunchUriAsync(new Uri(url));
    }

    public static async Task CopyToClipboard(string? text, bool showToast = true)
    {
        if (string.IsNullOrWhiteSpace(text) || MainWindow.Clipboard is null) return;
        await MainWindow.Clipboard.SetTextAsync(text);

        if (text.Length > 1024) text = $"{text[..1024]}...";

        if (!showToast) return;
        ShowToast(NotificationType.Information, "Copied to clipboard", text);
    }
}