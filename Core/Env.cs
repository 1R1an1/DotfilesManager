using System.Text.Json;
using DotfilesManager.UI;

namespace DotfilesManager.Core;

internal static class Env
{
    // HomeDir se inicializa primero porque ConfigFile depende de él.
    // El orden de declaración de campos estáticos importa en C#.
    public static readonly string HomeDir = GetRealHome();

    public static readonly string ConfigFile =
        Path.Combine(HomeDir, ".config", "dotfiles-manager", "config.json");

    public static string DotfilesDir { get; private set; } = "";

    // Propiedades calculadas: se recomputan cada vez que se leen,
    // usando siempre el valor actual de DotfilesDir
    public static string SystemDir => Path.Combine(DotfilesDir, "system");
    public static string ScriptsDir => Path.Combine(DotfilesDir, ".scripts");
    public static string ProfilesFile => Path.Combine(DotfilesDir, "perfiles.json");

    // Cada llamada genera un timestamp nuevo, así cada operación tiene su propio backup
    public static string BackupDir =>
        Path.Combine(HomeDir, ".dotfiles-backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

    // Lee la config guardada. Si no existe o la ruta no existe, pide la ruta al usuario.
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

        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("  Ruta del repo de dotfiles:");
        Console.Write("  > ");
        string? input = Console.ReadLine()?.Trim();

        // Expandir ~ a la ruta real del home
        string path = input?.StartsWith("~/") == true
            ? Path.Combine(HomeDir, input[2..])
            : input ?? "";

        if (!Directory.Exists(path))
        {
            Console.WriteLine("  El directorio no existe. Saliendo.");
            Environment.Exit(1);
        }

        DotfilesDir = path;
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(new Config { DotfilesDir = path }));
    }

    /// <summary>
    /// Recarga la ruta del repo desde config.json sin interacción.
    /// </summary>
    public static void ReloadConfig()
    {
        if (!File.Exists(ConfigFile)) return;
        var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFile));
        if (cfg?.DotfilesDir is not null && Directory.Exists(cfg.DotfilesDir))
            DotfilesDir = cfg.DotfilesDir;
    }

    // Retorna las carpetas del repo que son paquetes stow válidos.
    // Se excluyen carpetas ocultas (.) y la carpeta reservada "system".
    public static string[] GetPackages() =>
        Directory.Exists(DotfilesDir)
            ? Directory.GetDirectories(DotfilesDir)
                .Select(Path.GetFileName)
                .Where(n => n is not null && !n.StartsWith('.') && n != "system")
                .Cast<string>()
                .OrderBy(n => n)
                .ToArray()
            : [];

    // Cuando la app corre con sudo, el usuario real está en SUDO_USER.
    // Si no, usa la variable HOME o el perfil del sistema.
    private static string GetRealHome()
    {
        string? sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrEmpty(sudoUser))
            return Path.Combine("/home", sudoUser);

        return Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    // Modelo del archivo config.json
    private sealed class Config
    {
        public string? DotfilesDir { get; set; }
    }

    /// <summary>
    /// Cambia la ruta del repo de dotfiles y la guarda en config.json.
    /// </summary>
    public static void SetDotfilesDir(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"El directorio '{path}' no existe.");

        DotfilesDir = path;
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(new Config { DotfilesDir = path }));
        Printer.Info($"DotfilesDir cambiado a: {path}");
    }
}
