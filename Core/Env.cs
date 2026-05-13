using System.Text.Json;

namespace DotfilesManager.Core;

internal static class Env
{
    public static readonly string HomeDir = GetRealHome();

    private static readonly string ConfigFile =
        Path.Combine(HomeDir, ".config", "dotfiles-manager", "config.json");

    public static string DotfilesDir { get; private set; } = "";
    public static string SystemDir => Path.Combine(DotfilesDir, "system");
    public static string ScriptsDir => Path.Combine(Env.DotfilesDir, ".scripts");
    public static string BackupDir =>
            Path.Combine(HomeDir, ".dotfiles-backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

    public static void LoadOrInit()
    {
        if (File.Exists(ConfigFile))
        {
            var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFile));
            if (cfg?.DotfilesDir is not null && Directory.Exists(cfg.DotfilesDir))
            {
                DotfilesDir = cfg.DotfilesDir;
                return;
            }
        }

        // No existe o la ruta guardada ya no existe — pedir al usuario
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("  Ruta del repo de dotfiles: ");
        Console.Write("  > ");
        string? input = Console.ReadLine()?.Trim();
        string path = input?.StartsWith("~/") == true
            ? Path.Combine(HomeDir, input[2..])
            : input ?? "";

        if (!Directory.Exists(path))
        {
            Console.WriteLine("  El directorio no existe. Saliendo.");
            Environment.Exit(1);
        }

        DotfilesDir = path;
        if (!Directory.Exists(Path.GetDirectoryName(ConfigFile))) Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile));
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(new Config { DotfilesDir = path }));
    }

    public static string[] GetPackages() =>
        Directory.Exists(DotfilesDir)
            ? Directory.GetDirectories(DotfilesDir)
                .Select(Path.GetFileName)
                .Where(n => n is not null && !n.StartsWith('.') && n != "system")
                .Cast<string>()
                .OrderBy(n => n)
                .ToArray()
            : [];

    private static string GetRealHome()
    {
        string? sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrEmpty(sudoUser))
            return Path.Combine("/home", sudoUser);
        return Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private sealed class Config
    {
        public string? DotfilesDir { get; set; }
    }
}