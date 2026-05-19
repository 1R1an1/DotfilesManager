using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class ApplyOp
{
    public static void Run()
    {
        Summary.Reset();

        // ── Elegir qué aplicar ────────────────────────────────────────────
        Console.Clear();
        Printer.Header("Aplicar dotfiles");

        bool hasHomePackages = Env.GetPackages().Length > 0;
        bool hasSystemFiles = Directory.Exists(Env.SystemDir) &&
                              Directory.EnumerateFileSystemEntries(Env.SystemDir).Any();

        if (!hasHomePackages && !hasSystemFiles)
        {
            Printer.Error("No hay paquetes stow ni archivos de sistema disponibles.");
            Printer.PressEnterToContinue();
            return;
        }

        // Construir las opciones dinámicamente según lo que exista
        List<string> applyOptions = new();
        if (hasHomePackages) applyOptions.Add("Paquetes de home (stow)");
        if (hasSystemFiles) applyOptions.Add("Symlinks de sistema (system/)");
        applyOptions.Add("Ambos");

        int applyChoice = Menu.SelectOne("¿Qué querés aplicar?", applyOptions.ToArray());

        Console.Clear();
        Printer.Header("Aplicar dotfiles");

        if (applyChoice == -1) return;

        bool applyHome = applyChoice == 0 || applyChoice == 2;
        bool applySystem = applyChoice == 1 || applyChoice == 2;

        // ── Aplicar paquetes de home ──────────────────────────────────────
        if (applyHome && hasHomePackages)
            ApplyHomePackages();

        // ── Aplicar symlinks de sistema ───────────────────────────────────
        if (applySystem && hasSystemFiles)
            ApplySystemSymlinks();

        Summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── UI: selecciona paquetes y llama al método sin UI ──────────────────
    private static void ApplyHomePackages()
    {
        string[] packages = Env.GetPackages();
        if (packages.Length == 0)
        {
            Printer.Error("No hay paquetes stow disponibles.");
            return;
        }

        int[] selected = Menu.SelectMulti("Aplicar dotfiles — seleccioná paquetes", packages);

        Console.Clear();
        Printer.Header("Aplicar dotfiles");

        if (selected.Length == 0)
        {
            Printer.Warn("No seleccionaste ningún paquete.");
            return;
        }

        string[] chosen = selected.Select(i => packages[i]).ToArray();

        Console.WriteLine();
        Printer.Info($"Paquetes seleccionados: {string.Join(", ", chosen)}");

        if (!Menu.Confirm("¿Aplicar estos paquetes?"))
            return;

        // Delegar en el método sin UI
        ApplyHome(chosen);
    }

    // ── UI: selecciona archivos de sistema y llama al método sin UI ───────
    private static void ApplySystemSymlinks()
    {
        // TreeExplorer retorna las rutas absolutas de los items marcados dentro de system/
        string[] selected = TreeExplorer.Run("Seleccioná archivos/carpetas de sistema a aplicar", Env.SystemDir);

        Console.Clear();
        Printer.Header("Aplicar dotfiles");

        if (selected.Length == 0)
        {
            Printer.Warn("No seleccionaste ningún symlink de sistema.");
            return;
        }

        // Delegar en el método sin UI
        ApplySystem(selected);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Métodos sin UI (usados también por CLI)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aplica paquetes stow sin interfaz interactiva.
    /// </summary>
    public static bool ApplyHome(string[] packages)
    {
        // Crear el directorio de backup con timestamp para esta sesión.
        string backupDir = Env.BackupDir + "_applyHomeAction";

        foreach (string pkg in packages)
        {
            // Backup
            var backups = Backup.BackupHomePackage(pkg, backupDir);
            if (backups is null) return false;

            foreach (var i in backups)
                File.Delete(i);

            var result = Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg);
            // Stow
            if (result.Ok)
                Summary.TrackOk($"stow: {pkg}");

            else if ((result = Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg, adopt: true)).Ok)
                Summary.TrackOk($"stow (adopt): {pkg}");
            else
            {
                Summary.TrackErr($"stow falló: \n{result.Stderr}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Aplica symlinks de sistema sin interfaz interactiva.
    /// </summary>
    public static void ApplySystem(string[] repoPaths)
    {
        // Crear el directorio de backup con timestamp para esta sesión.
        string backupDir = Env.BackupDir + "_applySystemAction";

        foreach (string systemPath in repoPaths)
        {
            if (!Directory.Exists(systemPath))
                Shell.Run("mkdir", $"-p \"{systemPath}\"", asSudo: true);

            string path = systemPath;

            if (path.StartsWith(Env.SystemDir))
                path = path.Substring(Env.SystemDir.Length);

            string relative = path.TrimStart('/');
            string sourcePath = Path.Combine(Env.SystemDir, relative);

            if (Directory.Exists(sourcePath))
            {
                // Backup de cada archivo dentro
                foreach (string file in Directory.GetFiles(systemPath, "*", SearchOption.AllDirectories))
                {
                    if (!Backup.BackupSystemPath(file, backupDir))
                        return;
                }

                // Symlinks individuales
                var created = Shell.SymlinkDirectoryContents(sourcePath, systemPath, asSudo: true);
                foreach (string dest in created!)
                    Summary.TrackOk($"symlink sistema: {dest}");

            }
            else if (File.Exists(sourcePath))
            {
                if (!Backup.BackupSystemPath(systemPath, backupDir))
                    return;

                if (Shell.Symlink(sourcePath, systemPath, true).Ok)
                    Summary.TrackOk($"symlink sistema: {systemPath}");
                else
                    Summary.TrackErr($"symlink sistema falló: {systemPath}");
            }
            else
                Summary.TrackErr($"No existe: {systemPath}");

        }
    }
}
