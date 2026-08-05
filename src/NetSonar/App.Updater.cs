using Avalonia.Controls.Notifications;
using System;
using System.Threading.Tasks;
using Material.Icons;
using Material.Icons.Avalonia;
using NetSonar.Avalonia.Controls;
using NetSonar.Avalonia.ViewModels.Dialogs;
using StageKit.Runtime;
using SukiUI.Dialogs;
using Updatum;
using ZLogger;

namespace NetSonar.Avalonia;

public partial class App
{
    /// <summary>
    /// Interval in minutes to check for updates.
    /// </summary>
    public const double CheckUpdateHourInterval = 2; // Hours

    internal static readonly UpdatumManager AppUpdater = new(EntryApplication.AssemblyRepositoryUrl)
    {
        InstallUpdateWindowsInstallerArguments = "/qb",
        InstallUpdateSingleFileExecutableName = Software,
        InstallUpdateCodesignMacOSApp = true,
    };


    /// <summary>
    /// Check for updates asynchronously and show a toast notification if an update is available.
    /// </summary>
    /// <param name="showNoUpdateFoundMessage"></param>
    /// <returns></returns>
    public static async Task<bool> CheckForUpdatesAsync(bool showNoUpdateFoundMessage = true)
    {
        try
        {
            var updateFound = await AppUpdater.CheckForUpdatesAsync();
            Logger.ZLogInformation($"Checking for updates: ({updateFound})");
            if (!updateFound && showNoUpdateFoundMessage)
            {
                ShowToast(NotificationType.Success, Localization["Update.None.Title"],
                    Localization.Format("Update.None.Message", SoftwareWithVersion));
            }
            AppSettings.LastUpdateDateTimeCheck = AppUpdater.LastCheckDateTime;
        }
        catch (Exception ex)
        {
            if (showNoUpdateFoundMessage)
            {
                ShowExceptionToast(ex, Localization["Update.CheckFailed"]);
            }
        }

        return false;
    }

    private void AppUpdaterOnUpdateFound(object? sender, EventArgs e)
    {
        if (!AppUpdater.IsUpdateAvailable) return;

        var release = AppUpdater.LatestRelease;

        ToastActionButton[] buttons =
        [
            new(new MaterialIconText
            {
                Kind = MaterialIconKind.Eye,
                Text = Localization["Common.View"],
            }, toast =>
            {
                DialogManager.CreateDialog()
                    .WithViewModel(dialog => new AppUpdateDialogModel(dialog, release))
                    .TryShow();
            }, true),
            new(new MaterialIconText
            {
                Kind = MaterialIconKind.Close,
                Text = Localization["Common.Ignore"],
            }, null, true),
        ];
        CreateToast(NotificationType.Information, Localization.Format("Update.Found.Title", Software),
            Localization.Format("Update.Found.Message", EntryApplication.AssemblyVersionString, release.TagName,
                AppUpdater.ReleasesAheadCount, release.CreatedAt.ToLocalTime()),
            false,
            buttons).Queue();

    }

}
