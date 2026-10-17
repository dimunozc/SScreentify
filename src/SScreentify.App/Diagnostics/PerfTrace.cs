using System.Diagnostics;
using System.Globalization;
using SScreentify.App.Services;

namespace SScreentify.App.Diagnostics;

/// <summary>Opt-in timing log (--perf). Each line: ISO-8601 time, event name, milliseconds since process start, extra detail.</summary>
public static class PerfTrace
{
    private static readonly DateTime ProcessStart = Process.GetCurrentProcess().StartTime;

    public static bool Enabled { get; set; }

    public static void Mark(string name, string detail = "")
    {
        if (!Enabled) return;
        var now = DateTime.Now;
        var line = string.Create(CultureInfo.InvariantCulture,
            $"{now:O} {name} {(now - ProcessStart).TotalMilliseconds:0.0}{(detail.Length > 0 ? " " + detail : "")}{Environment.NewLine}");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.PerfLog)!);
            File.AppendAllText(AppPaths.PerfLog, line);
        }
        catch (IOException)
        {
            // Diagnostics must never break capturing.
        }
    }

    public static void Memory(string name)
    {
        if (!Enabled) return;
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        Mark(name, string.Create(CultureInfo.InvariantCulture,
            $"workingSetMB={process.WorkingSet64 / 1048576.0:0.0} privateMB={process.PrivateMemorySize64 / 1048576.0:0.0} managedMB={GC.GetTotalMemory(false) / 1048576.0:0.0}"));
    }
}
