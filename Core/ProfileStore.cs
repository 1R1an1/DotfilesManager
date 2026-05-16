using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotfilesManager.Core;

internal static class ProfileStore
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static List<Profile> Load()
    {
        if (!File.Exists(Env.ProfilesFile)) return [];

        string json = File.ReadAllText(Env.ProfilesFile);
        return JsonSerializer.Deserialize<List<Profile>>(json, _opts) ?? [];
    }

    public static void Save(List<Profile> profiles)
    {
        string json = JsonSerializer.Serialize(profiles, _opts);
        File.WriteAllText(Env.ProfilesFile, json);
    }
}

internal sealed class Profile
{
    public string Nombre { get; set; } = "";
    public List<string> Paquetes { get; set; } = [];
    public List<string> Dotfiles { get; set; } = [];
}