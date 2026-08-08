using System.Text.Json;

namespace SignalPet;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SignalPet", "settings.json");

    public PetSettings Load()
    {
        if (!File.Exists(_path)) return new PetSettings();
        return JsonSerializer.Deserialize<PetSettings>(File.ReadAllText(_path), JsonOptions) ?? new PetSettings();
    }

    public void Save(PetSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
