using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class ApplyOp
{
    public static void Run(Summary summary)
    {
        summary.Reset();

        // Crear el directorio de backup con timestamp para esta sesión.
        // Se guarda en una variable porque Env.BackupDir genera un timestamp nuevo cada vez que se llama.
        string backupDir = Env.BackupDir + "_applyAction";

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
        {
            ApplyHomePackages(summary, backupDir);
        }

        // ── Aplicar symlinks de sistema ───────────────────────────────────
        if (applySystem && hasSystemFiles)
        {
            ApplySystemSymlinks(summary, backupDir);
        }

        summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── Paquetes de home (stow) ──────────────────────────────────────────────

    private static void ApplyHomePackages(Summary summary, string backupDir)
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

        string[] chosenPackages = selected.Select(i => packages[i]).ToArray();

        Console.WriteLine();
        Printer.Info($"Paquetes seleccionados: {string.Join(", ", chosenPackages)}");

        if (!Menu.Confirm("¿Aplicar estos paquetes?"))
            return;

        Console.WriteLine();
        Printer.Info("Haciendo backup de archivos existentes...");
        foreach (string pkg in chosenPackages)
            foreach (var i in Backup.BackupPackage(pkg, backupDir))
                File.Delete(i);

        Console.WriteLine();
        Printer.Info("Aplicando symlinks...");
        Console.WriteLine();

        foreach (string pkg in chosenPackages)
        {
            // Intentar stow normal primero.
            // Si falla por archivos existentes que no son de stow, intentar con --adopt
            // que los mueve al repo y crea el symlink en su lugar.
            if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg))
                summary.TrackOk($"stow: {pkg}");
            else if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg, adopt: true))
                summary.TrackOk($"stow (adopt): {pkg}");
            else
                summary.TrackErr($"stow falló: {pkg}");
        }
    }

    // ── Symlinks de sistema (system/) ────────────────────────────────────────

    private static void ApplySystemSymlinks(Summary summary, string backupDir)
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

        // Primero backup de todo lo seleccionado
        Console.WriteLine();
        Printer.Info("Haciendo backup de archivos de sistema...");
        Console.WriteLine();

        foreach (string entryInRepo in selected)
        {
            if (Directory.Exists(entryInRepo))
            {
                // Si es carpeta, hacer backup de cada archivo adentro
                foreach (string file in Directory.EnumerateFiles(entryInRepo, "*", SearchOption.AllDirectories))
                {
                    string dest = "/" + Path.GetRelativePath(Env.SystemDir, file);
                    if (!Backup.BackupSystemFile(dest, backupDir, summary)) return;
                }
            }
            else
            {
                // Si es archivo, backup directo
                string dest = "/" + Path.GetRelativePath(Env.SystemDir, entryInRepo);
                if (!Backup.BackupSystemFile(dest, backupDir, summary)) return;
            }
        }

        // Después aplicar todo
        Console.WriteLine();
        Printer.Info("Aplicando symlinks de sistema...");
        Console.WriteLine();

        foreach (string entryInRepo in selected)
        {
            if (Directory.Exists(entryInRepo))
            {
                // Si es carpeta, aplicar cada archivo adentro como symlink individual
                foreach (string file in Directory.EnumerateFiles(entryInRepo, "*", SearchOption.AllDirectories))
                {
                    string rel = Path.GetRelativePath(Env.SystemDir, file);
                    string dest = "/" + rel;
                    if (Shell.SudoSymlink(file, dest))
                        summary.TrackOk($"symlink sistema: {dest}");
                    else
                        summary.TrackErr($"symlink sistema falló: {dest}");
                }
            }
            else
            {
                // Si es archivo, aplicar directamente
                string dest = "/" + Path.GetRelativePath(Env.SystemDir, entryInRepo);
                if (Shell.SudoSymlink(entryInRepo, dest))
                    summary.TrackOk($"symlink sistema: {dest}");
                else
                    summary.TrackErr($"symlink sistema falló: {dest}");
            }
        }
    }
}