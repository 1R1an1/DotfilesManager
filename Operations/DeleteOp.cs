using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class DeleteOp
{
    public static void Run()
    {
        Summary.Reset();

        string[] topOptions = ["Symlinks de un paquete stow (home)", "Symlink de sistema (/etc u otro)"];
        int choice = Menu.SelectOne("Borrar symlinks — ¿qué querés eliminar?", topOptions);

        if (choice == -1) return;

        Console.Clear();
        Printer.Header("Borrar symlinks");

        if (choice == 0) DeletePackageUI();
        else DeleteSystemSymlinksUI();

        Summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── UI: selecciona paquete y acción, delega en método sin UI ──────────
    private static void DeletePackageUI()
    {
        string[] packages = Env.GetPackages();
        int pkgIdx = Menu.SelectOne("Seleccioná el paquete a desenlazar", packages);
        if (pkgIdx == -1) return;
        string package = packages[pkgIdx];

        string[] actions = [
            "Solo eliminar symlinks (archivos quedan en el repo)",
            "Restaurar archivos a su ubicación original (y eliminar symlinks)",
            "Eliminar todo (symlinks + archivos del repo)"
        ];

        int actionIdx = Menu.SelectOne($"¿Qué querés hacer con '{package}'?", actions);
        Console.Clear();
        Printer.Header("Borrar symlinks");
        if (actionIdx == -1) return;

        string action = actionIdx switch { 0 => "symlinks", 1 => "restore", 2 => "all", _ => "symlinks" };

        if (!Menu.Confirm($"¿Realizar acción '{actions[actionIdx]}' en '{package}'?"))
            return;

        DeleteHome(package, action);
    }

    // ── UI: selecciona archivos de system/ y acción, delega en sin UI ─────
    private static void DeleteSystemSymlinksUI()
    {
        string[] selected = TreeExplorer.Run("Seleccioná archivos/carpetas de sistema a eliminar", Env.SystemDir);

        Console.Clear();
        Printer.Header("Borrar symlinks");

        if (selected.Length == 0)
        {
            Printer.Warn("No seleccionaste ningún symlink de sistema.");
            return;
        }

        string[] actions = [
            "Solo eliminar el symlink (archivo queda en el repo)",
            "Restaurar archivo a su ubicación original (y eliminar symlink)",
            "Eliminar todo (symlink + archivo del repo)"
        ];

        int actionIdx = Menu.SelectOne($"¿Qué querés hacer con {selected.Length} elemento(s)?", actions);
        Console.Clear();
        Printer.Header("Borrar symlinks");
        if (actionIdx == -1) return;

        string action = actionIdx switch { 0 => "symlinks", 1 => "restore", 2 => "all", _ => "symlinks" };

        if (!Menu.Confirm($"¿Realizar acción '{actions[actionIdx]}'?")) return;

        DeleteSystem(selected, action);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Métodos sin UI (usados también por CLI)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Elimina symlinks de un paquete stow.
    /// action: "symlinks" (default), "restore", "all"
    /// </summary>
    public static void DeleteHome(string package, string action = "symlinks")
    {
        string pkgDir = Path.Combine(Env.DotfilesDir, package);

        switch (action)
        {
            case "symlinks":
                if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, package, delete: true).Ok)
                    Summary.TrackOk($"Symlinks de '{package}' eliminados.");
                else
                    Summary.TrackErr($"No se pudieron eliminar los symlinks de '{package}'.");
                break;

            case "restore":
                string[] files = Directory.GetFiles(pkgDir, "*", SearchOption.AllDirectories);
                if (!Shell.Stow(Env.DotfilesDir, Env.HomeDir, package, delete: true).Ok)
                {
                    Printer.Error($"stow -D falló para '{package}', abortando.");
                    Summary.TrackErr($"stow -D falló para '{package}'.");
                    return;
                }
                Summary.TrackOk($"Symlinks de '{package}' eliminados.");

                if (Shell.Copy(pkgDir, Env.HomeDir, recursive: true, contents: true).Ok)
                {
                    foreach (string src in files)
                        Summary.TrackOk($"Restaurado: ~/{Path.GetRelativePath(pkgDir, src)}");

                }
                else
                    Summary.TrackErr($"Falló la copia masiva del paquete '{package}'.");
                break;

            case "all":
                Shell.Stow(Env.DotfilesDir, Env.HomeDir, package, delete: true);
                try
                {
                    Directory.Delete(pkgDir, recursive: true);
                    Summary.TrackOk($"Carpeta del repo eliminada.");
                }
                catch { Summary.TrackErr($"No se pudo eliminar la carpeta del repo."); }
                break;

            default:
                Summary.TrackErr($"Acción desconocida: {action}");
                break;
        }
    }

    /// <summary>
    /// Elimina symlinks de sistema.
    /// action: "symlinks" (default), "restore", "all"
    /// </summary>
    public static void DeleteSystem(string[] repoPaths, string action = "symlinks")
    {
        foreach (string entry in repoPaths)
        {
            string systemPath = "/" + Path.GetRelativePath(Env.SystemDir, entry);

            switch (action)
            {
                case "symlinks":
                    if (Shell.Remove(systemPath, true).Ok)
                        Summary.TrackOk($"Symlink eliminado: {systemPath}");
                    else
                        Summary.TrackErr($"No se pudo eliminar: {systemPath}");
                    break;

                case "restore":
                    Backup.BackupSystemPath(systemPath, Env.BackupDir + "_deleteAction");
                    Shell.Remove(systemPath, true);
                    bool copied = Directory.Exists(entry)
                        ? Shell.Copy(entry, systemPath, asSudo: true, recursive: true).Ok
                        : Shell.Copy(entry, systemPath, asSudo: true).Ok;
                    if (copied)
                        Summary.TrackOk($"Archivo restaurado: {systemPath}");
                    else
                        Summary.TrackErr($"Falló la restauración de: {systemPath}");
                    break;

                case "all":
                    Shell.Remove(systemPath, true);
                    if (Shell.Remove(entry, true).Ok)
                        Summary.TrackOk($"Archivo del repo eliminado: {entry}");
                    else
                        Summary.TrackErr($"No se pudo eliminar del repo: {entry}");
                    break;

                default:
                    Summary.TrackErr($"Acción desconocida: {action}");
                    break;
            }
        }
    }
}
