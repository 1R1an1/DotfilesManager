using DotfilesManager.Core;
using DotfilesManager.UI;
using static DotfilesManager.UI.Colors;

namespace DotfilesManager.Operations;

internal static class DeleteOp
{
    public static void Run(Summary summary)
    {
        summary.Reset();

        string[] topOptions = ["Symlinks de un paquete stow (home)", "Symlink de sistema (/etc u otro)"];
        int choice = Menu.SelectOne("Borrar symlinks — ¿qué querés eliminar?", topOptions);

        Console.Clear();
        Printer.Header("Borrar symlinks");

        if (choice == -1) return;

        if (choice == 0)
            DeletePackage(summary);
        else
            DeleteSystemSymlink(summary);

        summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── Paquete stow ──────────────────────────────────────────────────────────
    private static void DeletePackage(Summary summary)
    {
        string[] packages = Env.GetPackages();
        int pkgIdx = Menu.SelectOne("Seleccioná el paquete a desenlazar", packages);

        Console.Clear();
        Printer.Header("Borrar symlinks");

        if (pkgIdx == -1) return;

        string package = packages[pkgIdx];

        string[] actions =
        [
            "Solo eliminar symlinks (archivos quedan en el repo)",
            "Restaurar archivos a su ubicación original (y eliminar symlinks)",
            "Eliminar todo (symlinks + archivos del repo)",
        ];

        int actionIdx = Menu.SelectOne($"¿Qué querés hacer con '{package}'?", actions);

        Console.Clear();
        Printer.Header("Borrar symlinks");

        if (actionIdx == -1) return;

        switch (actionIdx)
        {
            case 0:
                if (!Menu.Confirm($"¿Eliminar symlinks de '{package}'? Los archivos quedan en el repo.")) return;
                if (Shell.StowDelete(Env.DotfilesDir, Env.HomeDir, package))
                    summary.TrackOk($"Symlinks de '{package}' eliminados.");
                else
                    summary.TrackErr($"No se pudieron eliminar los symlinks de '{package}'.");
                break;

            case 1:
                if (!Menu.Confirm($"¿Restaurar archivos de '{package}' a home y eliminar symlinks?")) return;
                Console.WriteLine();

                if (!Shell.StowDelete(Env.DotfilesDir, Env.HomeDir, package))
                {
                    summary.TrackErr($"stow -D falló para '{package}', abortando restauración.");
                    return;
                }
                summary.TrackOk($"Symlinks de '{package}' eliminados.");

                string pkgDir = Path.Combine(Env.DotfilesDir, package);
                foreach (string src in Directory.EnumerateFiles(pkgDir, "*", SearchOption.AllDirectories))
                {
                    string rel  = Path.GetRelativePath(pkgDir, src);
                    string dest = Path.Combine(Env.HomeDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    try
                    {
                        File.Copy(src, dest, overwrite: true);
                        summary.TrackOk($"Restaurado: ~/{rel}");
                    }
                    catch { summary.TrackErr($"No se pudo restaurar: ~/{rel}"); }
                }
                break;

            case 2:
                if (!Menu.Confirm($"¿Eliminar symlinks Y archivos del repo de '{package}'? Esto no tiene vuelta.")) return;
                Console.WriteLine();

                if (Shell.StowDelete(Env.DotfilesDir, Env.HomeDir, package))
                    summary.TrackOk($"Symlinks de '{package}' eliminados.");
                else
                    summary.TrackErr($"stow -D falló para '{package}'.");

                string repoDir = Path.Combine(Env.DotfilesDir, package);
                try
                {
                    Directory.Delete(repoDir, recursive: true);
                    summary.TrackOk($"Carpeta del repo eliminada: {repoDir}");
                }
                catch { summary.TrackErr($"No se pudo eliminar: {repoDir}"); }
                break;
        }
    }

    // ── Symlink de sistema ────────────────────────────────────────────────────
    private static void DeleteSystemSymlink(Summary summary)
    {
        Console.WriteLine();
        Console.Write("  Ruta del symlink a eliminar: ");
        string? path = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(path))
        {
            Printer.Error("Ruta vacía.");
            return;
        }

        bool exists   = File.Exists(path) || Directory.Exists(path);
        bool isSymlink = exists && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

        if (!isSymlink)
        {
            if (exists)
                Printer.Error($"'{path}' existe pero no es un symlink.");
            else
                Printer.Error($"'{path}' no existe.");
            return;
        }

        string? repoFile = new FileInfo(path).LinkTarget;

        string[] actions =
        [
            "Solo eliminar el symlink (archivo queda en el repo)",
            "Restaurar archivo a su ubicación original (y eliminar symlink)",
            "Eliminar todo (symlink + archivo del repo)",
        ];

        int actionIdx = Menu.SelectOne($"¿Qué querés hacer con '{path}'?", actions);

        Console.Clear();
        Printer.Header("Borrar symlinks");

        if (actionIdx == -1) return;

        switch (actionIdx)
        {
            case 0:
                if (!Menu.Confirm($"¿Eliminar symlink '{path}'? El archivo queda en el repo.")) return;
                if (Shell.SudoRemove(path))
                    summary.TrackOk($"Symlink eliminado: {path}");
                else
                    summary.TrackErr($"No se pudo eliminar: {path}");
                break;

            case 1:
                if (!Menu.Confirm($"¿Restaurar '{repoFile}' a '{path}' y eliminar el symlink?")) return;
                bool removed = Shell.SudoRemove(path);
                bool copied  = repoFile is not null && Shell.SudoCopy(repoFile, path);
                if (removed && copied)
                    summary.TrackOk($"Symlink eliminado y archivo restaurado: {path}");
                else
                    summary.TrackErr($"Falló la restauración de: {path}");
                break;

            case 2:
                if (!Menu.Confirm($"¿Eliminar symlink Y archivo del repo '{repoFile}'? Sin vuelta atrás.")) return;
                if (Shell.SudoRemove(path))
                    summary.TrackOk($"Symlink eliminado: {path}");
                else
                    summary.TrackErr($"No se pudo eliminar el symlink: {path}");

                if (repoFile is not null && Shell.SudoRemove(repoFile))
                    summary.TrackOk($"Archivo del repo eliminado: {repoFile}");
                else
                    summary.TrackErr($"No se pudo eliminar del repo: {repoFile}");
                break;
        }
    }
}
