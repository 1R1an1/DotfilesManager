using DotfilesManager.Core;
using DotfilesManager.UI;

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

        // Crear el directorio de backup con timestamp para esta sesión.
        // Se guarda en una variable porque Env.BackupDir genera un timestamp nuevo cada vez que se llama.
        string backupDir = Env.BackupDir + "_applyAction";

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

        // Los symlinks de sistema son una operación separada y más riesgosa,
        // así que se preguntan aparte y solo si hay algo en la carpeta system/
        if (Directory.Exists(Env.SystemDir) &&
            Directory.EnumerateFileSystemEntries(Env.SystemDir).Any())
        {
            Console.WriteLine();
            if (Menu.Confirm("¿Aplicar también los symlinks de sistema?"))
            {
                Console.WriteLine();
                Printer.Info("Aplicando symlinks de sistema...");
                Console.WriteLine();

                foreach (string file in Directory.EnumerateFiles(Env.SystemDir, "*", SearchOption.AllDirectories))
                {
                    // Reconstruir la ruta de destino quitando el prefijo de SystemDir
                    // Ej: /repo/system/etc/grub/grub.cfg → /etc/grub/grub.cfg
                    string destPath = "/" + Path.GetRelativePath(Env.SystemDir, file);

                    if (!Backup.BackupSystemFile(destPath, backupDir)) return;

                    if (Shell.SudoSymlink(file, destPath))
                        summary.TrackOk($"symlink sistema: {destPath}");
                    else
                        summary.TrackErr($"symlink sistema falló: {destPath}");
                }
            }
        }

        summary.Print();
        Printer.PressEnterToContinue();
    }
}
