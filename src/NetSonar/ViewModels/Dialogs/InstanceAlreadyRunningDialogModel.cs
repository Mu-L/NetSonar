using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using Avalonia.Controls;
using ZLinq;

namespace NetSonar.Avalonia.ViewModels.Dialogs;

public partial class InstanceAlreadyRunningDialogModel : ViewModelBase
{
    public string Message { get; init; }
    public Process? FirstProcess { get; init; }

    public InstanceAlreadyRunningDialogModel()
    {
        var processes = Process.GetProcessesByName(App.Software);

        Message = App.Localization.Format("InstanceAlreadyRunning.Message", App.Software);

        if (Design.IsDesignMode)
        {
            Message += App.Localization.Format("Common.ProcessId", 1001);
        }
        else
        {
            if (processes.Length > 1)
            {
                FirstProcess = processes
                    .AsValueEnumerable()
                    .FirstOrDefault(p => p.Id != Environment.ProcessId);
                if (FirstProcess is not null)
                {
                    Message += App.Localization.Format("Common.ProcessId", FirstProcess.Id);
                }
            }
        }
    }

    [RelayCommand]
    public static void CloseWindow()
    {
        Environment.Exit(0);
    }
}
