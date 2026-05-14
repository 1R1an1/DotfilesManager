using DotfilesManager.UI;

namespace DotfilesManager.Core;

internal static class Backup
{
    // Hace backup de los archivos reales (no symlinks) que stow va a reemplazar.
    // Solo copia archivos que existen en home Y no son symlinks,
    // porque los symlinks no tienen contenido propio que perder.
    public static void BackupPackage(string package, string backupDir)
    {
        string srcDir = Path.Combine(Env.DotfilesDir, package);
        if (!Directory.Exists(srcDir)) return;

        int count = 0;
        foreach (string src in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            string rel  = Path.GetRelativePath(srcDir, src);
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

    // Hace backup de un archivo en home. Si falla, registra el error en summary
    // (si se pasó uno) o imprime un warning.
    public static bool BackupHomeFile(string absolutePath, string backupDir, Summary? summary = null)
    {
        string rel  = Path.GetRelativePath(Env.HomeDir, absolutePath);
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
            string msg = $"No se pudo hacer backup de: {absolutePath} ({ex.Message})";
            if (summary != null) summary.TrackErr(msg);
            else                 Printer.Warn(msg);
            return false;
        }
    }

    // Hace backup de un archivo o carpeta de sistema usando sudo.
    // Usa SudoCopyDir para carpetas y SudoCopy para archivos.
    public static bool BackupSystemFile(string absolutePath, string backupDir, Summary? summary = null)
    {
        string dest = Path.Combine(backupDir, "system", absolutePath.TrimStart('/'));

        bool ok = Directory.Exists(absolutePath)
            ? Shell.SudoCopyDir(absolutePath, dest)
            : Shell.SudoCopy(absolutePath, dest);

        if (ok)
            Printer.Info($"Backup de sistema: {absolutePath} → {dest}");
        else
        {
            string msg = $"No se pudo hacer backup de: {absolutePath}";
            if (summary != null) summary.TrackErr(msg);
            else                 Printer.Warn(msg);
        }
        return ok;
    }

    // FileAttributes.ReparsePoint es el flag que usa Windows/Linux en .NET
    // para indicar que una entrada del filesystem es un symlink.
    public static bool IsSymlink(string path) =>
        File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
