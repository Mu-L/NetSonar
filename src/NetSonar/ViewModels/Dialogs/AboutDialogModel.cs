using Avalonia;
using NetSonar.Avalonia.SystemOS;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;

namespace NetSonar.Avalonia.ViewModels.Dialogs;

public partial class AboutDialogModel : DialogViewModelBase
{
    public AboutDialogModel()
    {
    }

    public AboutDialogModel(ISukiDialog dialog) : base(dialog)
    {
    }

    public override string DialogTitle => App.Localization.Format("Dialog.About.Title", App.SoftwareWithVersion);

    public static string OperativeSystemDescription => $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}";

    public static string RuntimeDescription => RuntimeInformation.RuntimeIdentifier;

    public static string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    public static string AvaloniaUIDescription => typeof(AvaloniaObject).Assembly.GetName().Version!.ToString(3);

    public static string? GraphicCardName => SystemAware.GetGraphicCardName();

    public static string? ProcessorName => SystemAware.GetProcessorName();

    public static int ProcessorCount => Environment.ProcessorCount;

    public static string MemoryRamDescription
    {
        get
        {
            var memory = SystemAware.GetMemoryStatus();
            if (memory.ullTotalPhys == 0)
            {
                return App.Localization["Common.Unknown"];
            }

            var factor = Math.Pow(1024, 3);
            return $"{(memory.ullTotalPhys - memory.ullAvailPhys) / factor:F2} / {memory.ullTotalPhys / factor:F2} GB";
        }
    }

    public static int ScreenCount => App.MainWindow.Screens.ScreenCount;
    //public string ScreenResolution => $"{Screens.Primary.Bounds.Width} x {Screens.Primary.Bounds.Height} @ {Screens.Primary.PixelDensity*100}%";
    //public string WorkingArea => $"{Screens.Primary.WorkingArea.Width} x {Screens.Primary.WorkingArea.Height}";
    //public string RealWorkingArea => $"{App.MaxWindowSize.Width} x {App.MaxWindowSize.Height}";

    public static string ScreensDescription
    {
        get
        {
            var result = new StringBuilder();
            for (var i = 0; i < App.MainWindow.Screens.All.Count; i++)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                var onScreen = App.MainWindow.Screens.ScreenFromVisual(App.MainWindow);
                var screen = App.MainWindow.Screens.All[i];
                result.AppendLine($"{i + 1}: {screen.Bounds.Width} x {screen.Bounds.Height} @ {Math.Round(screen.Scaling * 100, 2)}%" +
                                  (screen.IsPrimary ? " (Primary)" : null) +
                                  (onScreen == screen ? " (On this)" : null)
                );
                result.AppendLine($"    WA: {screen.WorkingArea.Width} x {screen.WorkingArea.Height}    UA: {Math.Round(screen.WorkingArea.Width / screen.Scaling)} x {Math.Round(screen.WorkingArea.Height / screen.Scaling)}");
            }
            return result.ToString().TrimEnd();
        }
    }

    public static string InformationResumeText => $"""
                                                 Software: {App.SoftwareWithVersionRuntime}
                                                 Operative System: {OperativeSystemDescription}
                                                 Graphic Card: {GraphicCardName}
                                                 Processor: {ProcessorName}
                                                 Processor Cores: {ProcessorCount}
                                                 Memory RAM: {MemoryRamDescription}
                                                 Framework: {FrameworkDescription}
                                                 AvaloniaUI: {AvaloniaUIDescription}
                                                 Screen(s): {ScreensDescription}
                                                 """;

    [ObservableProperty]
    public partial bool IsContentCopied { get; set; }

    [RelayCommand]
    public async Task CopyInformationToClipboard()
    {
        await CopyToClipboardWithToast(InformationResumeText);
        IsContentCopied = true;
    }
}
