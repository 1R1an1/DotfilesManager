using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class DeleteOp
{
    public static void Run(Summary summary)
    {
        summary.Reset();

        string[] topOptions = ["Symlinks de un paquete stow (home)", "Symlink de sistema (/etc u otro)"];
        int choice = Menu.SelectOne("Borrar symlinks — ¿qué querés eliminar?", topOptions);

        if (choice == -1) return;

        Console.Clear();
        Printer.Header("Borrar symlinks");

        if (choice == 0) DeletePackage(summary);
        else DeleteSystemSymlink(summary);

        summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── Paquete stow ──────────────────────────────────────────────────────────

    private static void DeletePackage(Summary summary)
    {
        string[] packages = Env.GetPackages();
        int pkgIdx = Menu.SelectOne("Seleccioná el paquete a desenlazar", packages);

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
                if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, package, delete: true).Ok)
                    summary.TrackOk($"Symlinks de '{package}' eliminados.");
                else
                    summary.TrackErr($"No se pudieron eliminar los symlinks de '{package}'.");
                break;

            case 1:
                if (!Menu.Confirm($"¿Restaurar archivos de '{package}' a home y eliminar symlinks?")) return;
                Console.WriteLine();

                // ── Copia eficiente: todo de un saque ──
                string pkgDir = Path.Combine(Env.DotfilesDir, package);

                // Listamos los archivos **antes** de borrar los symlinks
                // para después llenar el resumen sin necesidad de otro enumerado
                string[] files = Directory.GetFiles(pkgDir, "*", SearchOption.AllDirectories);

                if (!Shell.Stow(Env.DotfilesDir, Env.HomeDir, package, delete: true).Ok)
                {
                    summary.TrackErr($"stow -D falló para '{package}', abortando restauración.");
                    return;
                }
                summary.TrackOk($"Symlinks de '{package}' eliminados.");

                // Copiamos todo el contenido del paquete a $HOME en un solo comando
                // (cp -a preserva permisos, dueño, grupo, timestamps y enlaces simbólicos)
                bool copyOk = Shell.Copy(pkgDir, Env.HomeDir, recursive: true, contents: true).Ok;
                if (copyOk)
                {
                    foreach (string src in files)
                    {
                        string rel = Path.GetRelativePath(pkgDir, src);
                        summary.TrackOk($"Restaurado: ~/{rel}");
                    }
                }
                else
                    summary.TrackErr($"Falló la copia masiva del paquete '{package}'.");

                break;

            case 2:
                if (!Menu.Confirm($"¿Eliminar symlinks Y archivos del repo de '{package}'? Esto no tiene vuelta.")) return;
                Console.WriteLine();

                if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, package, delete: true).Ok)
                    summary.TrackOk($"Symlinks de '{package}' eliminados.");
                else
                    summary.TrackErr($"stow -D falló para '{package}'.");

                try
                {
                    Directory.Delete(Path.Combine(Env.DotfilesDir, package), recursive: true);
                    summary.TrackOk($"Carpeta del repo eliminada.");
                }
                catch { summary.TrackErr($"No se pudo eliminar la carpeta del repo."); }
                break;
        }
    }

    // ── Symlink de sistema ────────────────────────────────────────────────────

    private static void DeleteSystemSymlink(Summary summary)
    {
        Console.WriteLine();
        Console.Write("  Ruta del symlink a eliminar: ");
        string? path = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(path)) { Printer.Error("Ruta vacía."); return; }

        bool exists = File.Exists(path) || Directory.Exists(path);
        bool isSymlink = exists && new FileInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint);

        if (!isSymlink)
        {
            Printer.Error(exists ? $"'{path}' existe pero no es un symlink." : $"'{path}' no existe.");
            return;
        }

        // LinkTarget retorna la ruta a la que apunta el symlink
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
                if (Shell.Remove(path, true).Ok)
                    summary.TrackOk($"Symlink eliminado: {path}");
                else
                    summary.TrackErr($"No se pudo eliminar: {path}");
                break;

            case 1:
                if (!Menu.Confirm($"¿Restaurar '{repoFile}' a '{path}' y eliminar el symlink?")) return;
                Shell.Remove(path, true);

                bool copied = repoFile is not null && (
                    Directory.Exists(repoFile)
                        ? Shell.Copy(repoFile, path, asSudo: true, recursive: true).Ok
                        : Shell.Copy(repoFile, path, asSudo: true).Ok
                );

                if (copied) summary.TrackOk($"Archivo restaurado: {path}");
                else summary.TrackErr($"Falló la restauración de: {path}");
                break;

            case 2:
                if (!Menu.Confirm($"¿Eliminar symlink Y archivo del repo '{repoFile}'? Sin vuelta atrás.")) return;

                if (Shell.Remove(path, true).Ok)
                    summary.TrackOk($"Symlink eliminado: {path}");
                else
                    summary.TrackErr($"No se pudo eliminar el symlink: {path}");

                if (repoFile is not null && Shell.Remove(repoFile, true).Ok)
                    summary.TrackOk($"Archivo del repo eliminado: {repoFile}");
                else
                    summary.TrackErr($"No se pudo eliminar del repo: {repoFile}");
                break;
        }
    }
}
