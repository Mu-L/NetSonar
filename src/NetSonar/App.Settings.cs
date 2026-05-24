using NetSonar.Avalonia.Settings;

namespace NetSonar.Avalonia;

public partial class App
{
    /// <summary>
    /// Immediately saves the current application settings to persistent storage.
    /// </summary>
    /// <remarks>This method is intended for emergency scenarios where settings must be saved without delay,
    /// such as during unexpected shutdowns or critical failures. It does not perform validation or prompt for user
    /// confirmation.</remarks>
    public static void PanicSaveSettings()
    {
        AppSettings.SaveInstance();
        SpeedTestsFile.SaveInstance();
        PingableServicesFile.SaveInstance();
        PingableServicesFile.SavePingRepliesInstance();
    }
}