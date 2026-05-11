namespace DotfilesManager.Core;

internal static class Env
{
    // Directorio del repo de dotfiles: mismo directorio que el ejecutable
    public static readonly string DotfilesDir = Directory.GetCurrentDirectory();

    public static readonly string SystemDir = Path.Combine(DotfilesDir, "system");
    public static readonly string HomeDir = GetRealHome();

    private static string GetRealHome()
    {
        string? sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrEmpty(sudoUser))
            return Path.Combine("/home", sudoUser);

        return Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
    public static string BackupDir =>
        Path.Combine(HomeDir, ".dotfiles-backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

    // Devuelve todos los paquetes stow (subdirectorios, excluyendo .git y system)
    public static string[] GetPackages()
    {
        if (!Directory.Exists(DotfilesDir)) return [];

        return Directory.GetDirectories(DotfilesDir)
            .Select(Path.GetFileName)
            .Where(name => name is not null && name != "system" && !name.StartsWith('.'))
            .Cast<string>()
            .OrderBy(n => n)
            .ToArray();
    }
}
