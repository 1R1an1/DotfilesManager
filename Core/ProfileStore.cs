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
    public List<ProfileStep> Pasos { get; set; } = [];
}

internal sealed class ProfileStep
{
    public StepType Tipo { get; set; }
    public string Valor { get; set; } = "";

    // Valor puede contener múltiples items separados por coma
    // Ej: "nvim,bash,hypr" para dotfiles o "nginx,docker" para paquetes
    public string[] ObtenerItems() =>
        string.IsNullOrWhiteSpace(Valor)
            ? []
            : Valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

internal enum StepType
{
    Script,
    Dotfile,
    Package
}