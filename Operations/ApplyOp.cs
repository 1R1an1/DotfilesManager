using DotfilesManager.Core;
using DotfilesManager.UI;
using static DotfilesManager.UI.Colors;

namespace DotfilesManager.Operations;

internal static class ApplyOp
{
    public static void Run(Summary summary)
    {
        summary.Reset();

        string[] packages = Env.GetPackages();
        if (packages.Length == 0)
        {
            Console.Clear();
            Printer.Header("Aplicar dotfiles");
            Printer.Error("No hay paquetes stow disponibles.");
            Printer.PressEnterToContinue();
            return;
        }

        int[] selected = Menu.SelectMulti("Aplicar dotfiles — seleccioná paquetes", packages);

        Console.Clear();
        Printer.Header("Aplicar dotfiles");

        if (selected.Length == 0)
        {
            Printer.Warn("No seleccionaste ningún paquete.");
            Printer.PressEnterToContinue();
            return;
        }

        string[] chosenPackages = selected.Select(i => packages[i]).ToArray();

        Console.WriteLine();
        Printer.Info($"Paquetes seleccionados: {string.Join(", ", chosenPackages)}");

        if (!Menu.Confirm("¿Aplicar estos paquetes?"))
        {
            Printer.PressEnterToContinue();
            return;
        }

        string backupDir = Env.BackupDir;

        Console.WriteLine();
        Printer.Info("Haciendo backup de archivos existentes...");
        foreach (string pkg in chosenPackages)
            Backup.BackupPackage(pkg, backupDir);

        Console.WriteLine();
        Printer.Info("Aplicando symlinks...");
        Console.WriteLine();

        foreach (string pkg in chosenPackages)
        {
            if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg))
                summary.TrackOk($"stow: {pkg}");
            else if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg, adopt: true))
                summary.TrackOk($"stow (adopt): {pkg}");
            else
                summary.TrackErr($"stow falló: {pkg}");
        }

        // Symlinks de sistema
        if (Directory.Exists(Env.SystemDir))
        {
            Console.WriteLine();
            Printer.Info("Aplicando symlinks de sistema...");
            Console.WriteLine();

            foreach (string file in Directory.EnumerateFiles(Env.SystemDir, "*", SearchOption.AllDirectories))
            {
                string destPath = "/" + Path.GetRelativePath(Env.SystemDir, file);
                if (Shell.SudoSymlink(file, destPath))
                    summary.TrackOk($"symlink sistema: {destPath}");
                else
                    summary.TrackErr($"symlink sistema falló: {destPath}");
            }
        }

        summary.Print();
        Printer.PressEnterToContinue();
    }
}
