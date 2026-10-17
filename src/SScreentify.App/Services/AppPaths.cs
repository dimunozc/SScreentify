namespace SScreentify.App.Services;

public static class AppPaths
{
    private static readonly string DataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "sScreentify");

    public static string SettingsFile { get; } = Path.Combine(DataFolder, "settings.json");

    public static string PerfLog { get; } = Path.Combine(DataFolder, "perf.log");

    public static string DefaultPicturesFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "sScreentify");
}
