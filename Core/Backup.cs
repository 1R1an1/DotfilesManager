using DotfilesManager.UI;

namespace DotfilesManager.Core;

internal static class Backup
{
    // Hace backup de los archivos reales (no symlinks) que stow va a reemplazar.
    // Ahora usa un único comando cp --parents para copiar todo de una vez,
    // en lugar de lanzar un proceso por cada archivo.
    public static string[]? BackupHomePackage(string package, string backupDir)
    {
        string srcDir = Path.Combine(Env.DotfilesDir, package);
        if (!Directory.Exists(srcDir))
        {
            Summary.TrackErr($"El paquete '{package}' no existe en el repo.");
            return null;
        }

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
            Summary.TrackErr($"Error en backup de: {package} ({stderr})");
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
    public static bool BackupHomePath(string absolutePath, string backupDir)
    {
        // Validar que la ruta esté dentro del home
        if (!absolutePath.StartsWith(Env.HomeDir))
        {
            Summary.TrackErr($"La ruta debe estar dentro del home ({Env.HomeDir}): {absolutePath}");
            return false;
        }

        if (!File.Exists(absolutePath) && !Directory.Exists(absolutePath))
        {
            Summary.TrackErr($"No existe: {absolutePath}");
            return false;
        }

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
            Summary.TrackErr($"No se pudo hacer backup de: {absolutePath} ({stderr})");
            return false;
        }
    }

    // Hace backup de un archivo o carpeta de sistema usando sudo.
    public static bool BackupSystemPath(string absolutePath, string backupDir)
    {
        // Validar que sea ruta absoluta
        if (!absolutePath.StartsWith('/'))
        {
            Summary.TrackErr($"La ruta debe ser absoluta: {absolutePath}");
            return false;
        }

        // Validar que no esté dentro del home
        if (absolutePath.StartsWith(Env.HomeDir))
        {
            Summary.TrackErr($"La ruta no puede estar dentro del home, usá BackupHomeFile: {absolutePath}");
            return false;
        }

        if (!File.Exists(absolutePath) && !Directory.Exists(absolutePath))
        {
            Summary.TrackErr($"No existe: {absolutePath}");
            return false;
        }

        string dest = Path.Combine(backupDir, "system", absolutePath.TrimStart('/'));

        bool ok = Directory.Exists(absolutePath)
            ? Shell.Copy(absolutePath, dest, asSudo: true, recursive: true).Ok
            : Shell.Copy(absolutePath, dest, asSudo: true).Ok;

        if (ok)
            Printer.Info($"Backup de sistema: {absolutePath} → {dest}");
        else
            Summary.TrackErr($"No se pudo hacer backup de: {absolutePath}");

        return ok;
    }

    // ═══════════════════════════════════════════════════════════════════
    // BACKUPS DEL REPO (CLI)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Backup de uno o varios paquetes del repo.
    /// </summary>
    public static bool BackupRepoPackages(string[] packages, string backupDir)
    {
        bool allOk = true;
        foreach (string pkg in packages)
        {
            string srcDir = Path.Combine(Env.DotfilesDir, pkg);
            if (!Directory.Exists(srcDir))
            {
                Summary.TrackErr($"El paquete '{pkg}' no existe en el repo.");
                allOk = false;
                continue;
            }

            string destDir = Path.Combine(backupDir, pkg);
            Directory.CreateDirectory(Path.GetDirectoryName(destDir)!);

            var (ok, _, stderr) = Shell.Copy(srcDir, destDir, recursive: true);

            if (ok)
                Printer.Info($"Backup del paquete '{pkg}' → {destDir}");
            else
            {
                Summary.TrackErr($"Error en backup del paquete '{pkg}': {stderr}"); ;
                allOk = false;
            }
        }
        return allOk;
    }

    /// <summary>
    /// Backup de todos los paquetes del repo.
    /// </summary>
    public static bool BackupAllRepoPackages(string backupDir)
    {
        string[] packages = Env.GetPackages();
        if (packages.Length == 0)
        {
            Summary.TrackErr("No hay paquetes en el repo.");
            return false;
        }

        return BackupRepoPackages(packages, backupDir);
    }

    /// <summary>
    /// Backup de una o varias rutas relativas a system/.
    /// </summary>
    public static bool BackupRepoSystemPaths(string[] paths, string backupDir)
    {
        bool allOk = true;
        foreach (string path in paths)
        {
            string fullPath = Path.Combine(Env.SystemDir, path);
            string dest = Path.Combine(backupDir, "system", path);

            if (Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                var (ok, _, stderr) = Shell.Copy(fullPath, dest, recursive: true);
                if (ok)
                    Printer.Info($"Backup de system/{path} → {dest}");
                else
                {
                    Summary.TrackErr($"Error en backup de system/{path}: {stderr}");
                    allOk = false;
                }
            }
            else if (File.Exists(fullPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                var (ok, _, stderr) = Shell.Copy(fullPath, dest);
                if (ok)
                    Printer.Info($"Backup de system/{path} → {dest}");
                else
                {
                    Summary.TrackErr($"Error en backup de system/{path}: {stderr}");
                    allOk = false;
                }
            }
            else
            {
                Summary.TrackErr($"No existe system/{path}");
                allOk = false;
            }
        }
        return allOk;
    }

    /// <summary>
    /// Backup de toda la carpeta system/.
    /// </summary>
    public static bool BackupAllRepoSystem(string backupDir)
    {
        string srcDir = Env.SystemDir;
        if (!Directory.Exists(srcDir))
        {
            Summary.TrackErr("La carpeta system/ no existe en el repo.");
            return false;
        }

        string destDir = Path.Combine(backupDir, "system");
        Directory.CreateDirectory(Path.GetDirectoryName(destDir)!);

        var (ok, _, stderr) = Shell.Copy(srcDir, destDir, recursive: true);
        if (ok)
            Printer.Info($"Backup de system/ → {destDir}");
        else
            Summary.TrackErr($"Error en backup de system/: {stderr}");

        return ok;
    }

    // FileAttributes.ReparsePoint es el flag que usa Windows/Linux en .NET
    // para indicar que una entrada del filesystem es un symlink.
    public static bool IsSymlink(string path) =>
        File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
