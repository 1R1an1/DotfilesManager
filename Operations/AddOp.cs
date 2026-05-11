using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class AddOp
{
    public static void Run(Summary summary)
    {
        summary.Reset();

        string[] sources = ["Desde home (~) — paquete stow", "Desde sistema (/etc u otro) — carpeta system/"];
        int choice = Menu.SelectOne("Agregar archivo — ¿desde dónde viene?", sources);

        Console.Clear();
        Printer.Header("Agregar archivo al repo");

        if (choice == -1) return;

        if (choice == 0)
            AddFromHome(summary);
        else
            AddFromSystem(summary);

        summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── Desde home ────────────────────────────────────────────────────────────
    private static void AddFromHome(Summary summary)
    {
        Console.WriteLine();
        Console.Write("  Ruta del archivo o carpeta: ");
        string? input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input)) { Printer.Error("Ruta vacía."); return; }

        // Expandir ~
        string path = input.StartsWith("~/")
            ? Path.Combine(Env.HomeDir, input[2..])
            : input;

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Printer.Error($"No existe: {path}");
            return;
        }

        // Seleccionar o crear paquete
        string[] packages   = Env.GetPackages();
        string[] pkgOptions = [.. packages, "── Crear paquete nuevo ──"];
        int pkgIdx = Menu.SelectOne("Seleccioná el paquete destino", pkgOptions);

        Console.Clear();
        Printer.Header("Agregar archivo al repo");

        if (pkgIdx == -1) return;

        string package;
        if (pkgIdx == packages.Length)
        {
            Console.WriteLine();
            Console.Write("  Nombre del nuevo paquete: ");
            string? newPkg = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(newPkg)) { Printer.Error("Nombre vacío."); return; }
            Directory.CreateDirectory(Path.Combine(Env.DotfilesDir, newPkg));
            package = newPkg;
        }
        else
        {
            package = packages[pkgIdx];
        }

        string rel           = Path.GetRelativePath(Env.HomeDir, path);
        string destInRepo    = Path.Combine(Env.DotfilesDir, package, rel);

        Console.WriteLine();
        Printer.Info($"Moviendo a: {destInRepo}");
        if (!Menu.Confirm("¿Confirmar?")) return;

        Console.WriteLine();
        Printer.Info("Haciendo backup del archivo original...");
        string backupDir = Env.BackupDir;
        Backup.BackupHomeFile(path, backupDir);

        Directory.CreateDirectory(Path.GetDirectoryName(destInRepo)!);
        try
        {
            if (File.Exists(path))
                File.Move(path, destInRepo);
            else
                MoveDirectory(path, destInRepo);

            summary.TrackOk($"Movido a: {destInRepo}");
        }
        catch (Exception ex)
        {
            summary.TrackErr($"No se pudo mover el archivo: {ex.Message}");
            return;
        }

        if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, package))
            summary.TrackOk($"Symlink creado en: ~/{rel}");
        else
            summary.TrackErr("stow falló al crear el symlink.");
    }

    // ── Desde sistema ─────────────────────────────────────────────────────────
    private static void AddFromSystem(Summary summary)
    {
        Console.WriteLine();
        Console.Write("  Ruta del archivo de sistema: ");
        string? path = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(path)) { Printer.Error("Ruta vacía."); return; }

        if (!File.Exists(path))
        {
            Printer.Error($"No existe: {path}");
            return;
        }

        string destInRepo = Path.Combine(Env.SystemDir, path.TrimStart('/'));

        Console.WriteLine();
        Printer.Info($"Se moverá a: {destInRepo}");
        if (!Menu.Confirm("¿Confirmar (requiere sudo)?")) return;

        Console.WriteLine();
        Printer.Info("Haciendo backup del archivo original...");
        Backup.BackupSystemFile(path, Env.BackupDir);

        if (!Shell.SudoMove(path, destInRepo))
        {
            summary.TrackErr("No se pudo mover el archivo.");
            return;
        }
        summary.TrackOk($"Movido a: {destInRepo}");

        if (Shell.SudoSymlink(destInRepo, path))
            summary.TrackOk($"Symlink creado en: {path}");
        else
            summary.TrackErr("No se pudo crear el symlink.");
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private static void MoveDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (string file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
        {
            string rel     = Path.GetRelativePath(src, file);
            string destFile = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Move(file, destFile);
        }
        Directory.Delete(src, recursive: true);
    }
}
