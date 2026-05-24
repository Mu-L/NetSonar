using NetSonar.Avalonia.Network;
using StageKit;

namespace NetSonar.Avalonia.Settings;

public sealed class SpeedTestsFile : RootCollectionFile<SpeedTestsFile, SpeedTestResult>
{
    public SpeedTestsFile()
    {
        AutoSave = true;
        DirectoryPath = ApplicationKit.ConfigsPath;
        FileName = "speedtests.json";
    }
}