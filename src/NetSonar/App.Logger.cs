using Microsoft.Extensions.Logging;
using StageKit;
using System;
using System.Diagnostics;
using System.IO;
using ZLogger;
using ZLogger.Providers;

namespace NetSonar.Avalonia;

public partial class App
{
    public static ILogger Logger { get; private set; } = null!;

    private static void SetupLogger()
    {
        var factory = LoggerFactory.Create(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddZLoggerRollingFile(options =>
            {
                // File name determined by parameters to be rotated
                options.FilePathSelector = (timestamp, sequenceNumber) =>
                    Path.Combine(ApplicationKit.LogsPath, $"{timestamp.ToLocalTime():yyyy-MM}_{sequenceNumber:000}.log");

                // The period of time for which you want to rotate files at time intervals.
                options.RollingInterval = RollingInterval.Month;

                // Limit of size if you want to rotate by file size. (KB)
                options.RollingSizeKB = 1024 * 2;

                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"(*){0}|{1}|", (in template, in info) => template.Format(info.Timestamp, info.LogLevel));
                    //formatter.SetSuffixFormatter($" ({0})", (in MessageTemplate template, in LogInfo info) => template.Format(info.Category));
                    //formatter.SetExceptionFormatter((writer, ex) => Utf8String.Format(writer, $"{ex.Message}"));
                });
            });

#if DEBUG
            // Add to output of simple rendered strings into memory. You can subscribe to this and use it.
            logging.AddZLoggerInMemory(processor =>
            {
                processor.MessageReceived += WriteLine;
            });
#endif
            // Output Structured Logging, setup options
            // logging.AddZLoggerConsole(options => options.UseJsonFormatter());
        });

        Logger = factory.CreateLogger(Software);
        ApplicationKit.Logger = Logger;
    }

    /// <summary>
    /// Writes the specified string to the console and debug output.
    /// </summary>
    /// <param name="str"></param>
    public static void WriteLine(object? str)
    {
#if DEBUG
        Console.WriteLine(str);
        Debug.WriteLine(str);
#endif
    }

    public static void WriteLine()
    {
#if DEBUG
        Console.WriteLine();
#endif
    }
}