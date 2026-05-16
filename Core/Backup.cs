using DotfilesManager.UI;

namespace DotfilesManager.Core;

internal static class Backup
{
    // Hace backup de los archivos reales (no symlinks) que stow va a reemplazar.
    // Ahora usa un único comando cp --parents para copiar todo de una vez,
    // en lugar de lanzar un proceso por cada archivo.
    public static string[]? BackupPackage(string package, string backupDir, Summary? summary = null)
    {
        string srcDir = Path.Combine(Env.DotfilesDir, package);
        if (!Directory.Exists(srcDir)) return [];

        // Paso 1: Elegir qué archivos necesitan backup
        List<string> relatives = new();
        foreach (string src in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(srcDir, src);
            string dest = Path.Combine(Env.HomeDir, rel);

            if (File.Exists(dest) && !IsSymlink(dest))
                relatives.Add(rel);
        }

        if (relatives.Count == 0) return [];

        // Paso 2: Copiar todos juntos con un solo cp
        string destDir = Path.Combine(backupDir, package);
        Directory.CreateDirectory(destDir);

        // Unimos todas las rutas relativas en un solo string
        string fileArgs = string.Join(" ", relatives.Select(r => $"\"{r}\""));

        // Ejecutamos cp desde el home para que las rutas relativas funcionen
        var (code, _, stderr) = Shell.Copy(fileArgs, destDir, useParents: true, workingDir: Env.HomeDir);

        if (!code)
        {
            string msg = $"Error en backup de: {package} ({stderr})";
            if (summary != null) summary.TrackErr(msg);
            else Printer.Error(msg);
            return null;
        }

        // Paso 3: Devolver las rutas completas
        string[] filesBackedup = relatives
            .Select(r => Path.Combine(Env.HomeDir, r))
            .ToArray();

        Printer.Info($"Backup de '{package}': {relatives.Count} archivo(s) → {destDir}");
        return filesBackedup;
    }

    // Hace backup de un archivo o carpeta en home. Si falla, registra el error en summary
    // (si se pasó uno) o imprime un warning.
    public static bool BackupHomeFile(string absolutePath, string backupDir, Summary? summary = null)
    {
        string rel = Path.GetRelativePath(Env.HomeDir, absolutePath);
        string dest = Path.Combine(backupDir, "home", rel);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        bool isDir = Directory.Exists(absolutePath);
        var (ok, _, stderr) = Shell.Copy(absolutePath, dest, recursive: isDir, contents: false);

        if (ok)
        {
            Printer.Info($"Backup: {absolutePath} → {dest}");
            return true;
        }
        else
        {
            string msg = $"No se pudo hacer backup de: {absolutePath} ({stderr})";
            if (summary != null) summary.TrackErr(msg);
            else Printer.Error(msg);
            return false;
        }
    }

    // Hace backup de un archivo o carpeta de sistema usando sudo.
    // Usa SudoCopyDir para carpetas y SudoCopy para archivos.
    public static bool BackupSystemFile(string absolutePath, string backupDir, Summary? summary = null)
    {
        string dest = Path.Combine(backupDir, "system", absolutePath.TrimStart('/'));

        bool ok = Directory.Exists(absolutePath)
            ? Shell.Copy(absolutePath, dest, asSudo: true, recursive: true).Ok
            : Shell.Copy(absolutePath, dest, asSudo: true).Ok;

        if (ok)
            Printer.Info($"Backup de sistema: {absolutePath} → {dest}");
        else
        {
            string msg = $"No se pudo hacer backup de: {absolutePath}";
            if (summary != null) summary.TrackErr(msg);
            else Printer.Error(msg);
        }
        return ok;
    }

    // FileAttributes.ReparsePoint es el flag que usa Windows/Linux en .NET
    // para indicar que una entrada del filesystem es un symlink.
    public static bool IsSymlink(string path) =>
        File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
