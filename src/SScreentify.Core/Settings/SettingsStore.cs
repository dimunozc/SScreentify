using System.Text.Json;
using System.Text.Json.Serialization;

namespace SScreentify.Core.Settings;

public sealed record SettingsLoadResult(AppSettings Settings, bool WasCorrupt);

/// <summary>Reads and atomically writes settings.json.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public SettingsStore(string path) => _path = path;

    public SettingsLoadResult Load()
    {
        if (!File.Exists(_path))
            return new SettingsLoadResult(new AppSettings(), false);

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), Options)
                           ?? throw new JsonException("El archivo está vacío.");
            return new SettingsLoadResult(settings.Normalize(), false);
        }
        catch (JsonException)
        {
            File.Move(_path, _path + ".bak", overwrite: true);
            return new SettingsLoadResult(new AppSettings(), true);
        }
    }

    public void Save(AppSettings s)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(s, Options));
        File.Move(temp, _path, overwrite: true);
    }
}
