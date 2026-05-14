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
        string? backupDir = Env.BackupDir + "_applyAction";

        string[] packages = Env.GetPackages();
        if (packages.Length == 0)
        {
            Console.Clear();
            Printer.Header("Aplicar dotfiles");
            Printer.Error("No hay paquetes stow disponibles.");
            goto system;
        }

        int[] selected = Menu.SelectMulti("Aplicar dotfiles — seleccioná paquetes", packages);

        Console.Clear();
        Printer.Header("Aplicar dotfiles");

        if (selected.Length == 0)
        {
            Printer.Warn("No seleccionaste ningún paquete.");
            goto system;
        }

        string[] chosenPackages = selected.Select(i => packages[i]).ToArray();

        Console.WriteLine();
        Printer.Info($"Paquetes seleccionados: {string.Join(", ", chosenPackages)}");

        if (!Menu.Confirm("¿Aplicar estos paquetes?"))
        {
            Printer.PressEnterToContinue();
            return;
        }

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

    system:
        if (Directory.Exists(Env.SystemDir) &&
            Directory.EnumerateFileSystemEntries(Env.SystemDir).Any())
        {
            Console.WriteLine();
            if (Menu.Confirm("¿Aplicar también symlinks de sistema?"))
                ApplySystemSymlinks(summary, backupDir);
        }

        summary.Print();
        Printer.PressEnterToContinue();
    }

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
                foreach (string file in Directory.EnumerateFiles(entryInRepo, "*", SearchOption.AllDirectories))
                {
                    string dest = "/" + Path.GetRelativePath(Env.SystemDir, file);
                    if (!Backup.BackupSystemFile(dest, backupDir, summary)) return;
                }
            }
            else
            {
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