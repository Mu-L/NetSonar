using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using StageKit;
using StageKit.Runtime;
using Updatum;

namespace NetSonar.Avalonia.ViewModels.Dialogs;

public partial class CrashReportDialogModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Header { get; set; } = App.Localization.Format("CrashReport.Header", App.Software);

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsContentCopied { get; set; }

    public CrashReport? CrashReport { get; init; }

    public CrashReportDialogModel()
    {
        if (Design.IsDesignMode)
        {
            var divideByZero = new DivideByZeroException[10];

            for (int i = 0; i < divideByZero.Length; i++)
            {
                divideByZero[i] = new DivideByZeroException();
            }

            CrashReport = new CrashReport(new AggregateException(divideByZero), "Crash sample");
        }
        BuildMessage();
    }

    public CrashReportDialogModel(CrashReport? crashReport)
    {
        CrashReport = crashReport;
        BuildMessage();
    }

    private void BuildMessage()
    {
        Message = CrashReport?.FormattedMessage ?? App.Localization["CrashReport.NoDetails"];
    }

    [RelayCommand]
    public async Task CopyInformationToClipboard()
    {
        await CopyToClipboardWithoutToast(Message);
        IsContentCopied = true;
    }

    [RelayCommand]
    public Task<bool> Report()
    {
        if (CrashReport is null) return Task.FromResult(false);
        using var reader = new StringReader(CrashReport.FormattedMessage);
        var url = $"https://github.com/sn4k3/NetSonar/issues/new?template=bug_report_form.yml&title={HttpUtility.UrlEncode($"[Crash] {reader.ReadLine()}")}&system={HttpUtility.UrlEncode(AboutDialogModel.InformationResumeText)}&bug_description={HttpUtility.UrlEncode($"```\n{Message}\n```")}";
        return LaunchUriAsync(url);
    }


    [RelayCommand]
    public static void RestartApplication()
    {
        ApplicationKit.LaunchNewInstanceKeepApplicationArgs();
        CloseWindow();
    }

    [RelayCommand]
    public static void CloseWindow()
    {
        Environment.Exit(0);
    }
}
