using DotfilesManager.UI;

namespace DotfilesManager.Core;

internal static class Backup
{
    // Backup de todos los archivos reales (no symlinks) de un paquete stow
    public static void BackupPackage(string package, string backupDir)
    {
        string srcDir = Path.Combine(Env.DotfilesDir, package);
        if (!Directory.Exists(srcDir)) return;

        int count = 0;
        foreach (string src in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(srcDir, src);
            string dest = Path.Combine(Env.HomeDir, rel);

            if (File.Exists(dest) && !IsSymlink(dest))
            {
                string bkp = Path.Combine(backupDir, package, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(bkp)!);
                File.Copy(dest, bkp, overwrite: true);
                count++;
            }
        }

        if (count > 0)
            Printer.Info($"Backup de '{package}': {count} archivo(s) → {backupDir}");
    }

    // Backup de un archivo en home
    public static bool BackupHomeFile(string absolutePath, string backupDir)
    {
        string rel = Path.GetRelativePath(Env.HomeDir, absolutePath);
        string dest = Path.Combine(backupDir, "home", rel);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        try
        {
            File.Copy(absolutePath, dest, overwrite: true);
            Printer.Info($"Backup: {absolutePath} → {dest}");
            return true;
        }
        catch (Exception ex)
        {
            Printer.Warn($"No se pudo hacer backup de: {absolutePath} ({ex.Message})");
            return false;
        }
    }

    // Backup de un archivo de sistema (usa sudo cp)
    public static bool BackupSystemFile(string absolutePath, string backupDir)
    {
        string dest = Path.Combine(backupDir, "system", absolutePath.TrimStart('/'));
        bool ok;

        if (Directory.Exists(absolutePath))
            ok = Shell.SudoCopyDir(absolutePath, dest);
        else
            ok = Shell.SudoCopy(absolutePath, dest);

        if (ok)
            Printer.Info($"Backup de sistema: {absolutePath} → {dest}");
        else
            Printer.Warn($"No se pudo hacer backup de: {absolutePath}");
        return ok;
    }

    public static bool IsSymlink(string path) =>
        File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
