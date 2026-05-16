using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class ApplyOp
{
    public static void Run(Summary summary)
    {
        summary.Reset();

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
            ApplyHomePackages(summary);

        // ── Aplicar symlinks de sistema ───────────────────────────────────
        if (applySystem && hasSystemFiles)
            ApplySystemSymlinks(summary);

        summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── UI: selecciona paquetes y llama al método sin UI ──────────────────
    private static void ApplyHomePackages(Summary summary)
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
        ApplyHome(chosen, summary);
    }

    // ── UI: selecciona archivos de sistema y llama al método sin UI ───────
    private static void ApplySystemSymlinks(Summary summary)
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
        ApplySystem(selected, summary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Métodos sin UI (usados también por CLI)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aplica paquetes stow sin interfaz interactiva.
    /// </summary>
    public static bool ApplyHome(string[] packages, Summary? summary = null)
    {
        // Crear el directorio de backup con timestamp para esta sesión.
        string backupDir = Env.BackupDir + "_applyHomeAction";

        foreach (string pkg in packages)
        {
            // Backup
            var backups = Backup.BackupPackage(pkg, backupDir, summary);
            if (backups is null) return false;

            foreach (var i in backups)
                File.Delete(i);

            // Stow
            if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg).Ok)
            {
                Printer.Success($"stow: {pkg}");
                summary?.TrackOk($"stow: {pkg}");
            }
            else if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg, adopt: true).Ok)
            {
                Printer.Success($"stow (adopt): {pkg}");
                summary?.TrackOk($"stow (adopt): {pkg}");
            }
            else
            {
                Printer.Error($"stow falló: {pkg}");
                summary?.TrackErr($"stow falló: {pkg}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Aplica symlinks de sistema sin interfaz interactiva.
    /// </summary>
    public static void ApplySystem(string[] repoPaths, Summary? summary = null)
    {
        // Crear el directorio de backup con timestamp para esta sesión.
        string backupDir = Env.BackupDir + "_applySystemAction";

        foreach (string entryInRepo in repoPaths)
        {
            string systemPath = "/" + Path.GetRelativePath(Env.SystemDir, entryInRepo);

            if (Directory.Exists(entryInRepo))
            {
                // Backup de cada archivo dentro
                foreach (string file in Directory.GetFiles(entryInRepo, "*", SearchOption.AllDirectories))
                {
                    string dest = "/" + Path.GetRelativePath(Env.SystemDir, file);
                    Backup.BackupSystemFile(dest, backupDir, summary);
                }

                // Symlinks individuales
                var created = Shell.SymlinkDirectoryContents(entryInRepo, systemPath, asSudo: true);
                foreach (string dest in created)
                {
                    Printer.Success($"symlink sistema: {dest}");
                    summary?.TrackOk($"symlink sistema: {dest}");
                }
            }
            else if (File.Exists(entryInRepo))
            {
                Backup.BackupSystemFile(systemPath, backupDir, summary);
                if (Shell.Symlink(entryInRepo, systemPath, true).Ok)
                {
                    Printer.Success($"symlink sistema: {systemPath}");
                    summary?.TrackOk($"symlink sistema: {systemPath}");
                }
                else
                {
                    Printer.Error($"symlink sistema falló: {systemPath}");
                    summary?.TrackErr($"symlink sistema falló: {systemPath}");
                }
            }
            else
            {
                Printer.Error($"No se encontró '{systemPath}' en el repo.");
                summary?.TrackErr($"No se encontró '{systemPath}' en el repo.");
            }
        }
    }
}