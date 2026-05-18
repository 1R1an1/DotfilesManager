using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class AddOp
{
    public static void Run()
    {
        Summary.Reset();

        string[] sources = ["Desde home (~) — paquete stow", "Desde sistema (/etc u otro) — carpeta system/"];
        int choice = Menu.SelectOne("Agregar archivo — ¿desde dónde viene?", sources);

        Console.Clear();
        Printer.Header("Agregar archivo al repo");

        if (choice == -1) return;

        if (choice == 0) AddFromHomeUI();
        else AddFromSystemUI();

        Summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── UI: pide datos y llama al método sin UI ───────────────────────────
    private static void AddFromHomeUI()
    {
        Console.WriteLine();
        Console.Write("  Ruta del archivo o carpeta: ");
        string? input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input)) { Printer.Error("Ruta vacía."); return; }

        string path = input.StartsWith("~/")
            ? Path.Combine(Env.HomeDir, input[2..])
            : input;

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Printer.Error($"No existe: {path}");
            return;
        }

        // Mostrar paquetes existentes con "Crear paquete nuevo" al principio
        string[] packages = Env.GetPackages();
        string[] pkgOptions = ["── Crear paquete nuevo ──", .. packages];
        int pkgIdx = Menu.SelectOne("Seleccioná el paquete destino", pkgOptions);

        Console.Clear();
        Printer.Header("Agregar archivo al repo");

        if (pkgIdx == -1) return;

        string package;
        if (pkgIdx == 0)
        {
            // El usuario eligió "Crear paquete nuevo" (siempre es el primer índice)
            Console.WriteLine();
            Console.Write("  Nombre del nuevo paquete: ");
            string? newPkg = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(newPkg)) { Printer.Error("Nombre vacío."); return; }
            Directory.CreateDirectory(Path.Combine(Env.DotfilesDir, newPkg));
            package = newPkg;
        }
        else
        {
            package = packages[pkgIdx - 1];
        }

        Console.WriteLine();
        Printer.Info($"Moviendo a: {Path.Combine(Env.DotfilesDir, package, Path.GetRelativePath(Env.HomeDir, path))}");
        if (!Menu.Confirm("¿Confirmar?")) return;

        AddToHome(path, package);
    }

    // ── UI: pide ruta y llama al método sin UI ────────────────────────────
    private static void AddFromSystemUI()
    {
        Console.WriteLine();
        Console.Write("  Ruta del archivo o carpeta del sistema: ");
        string? path = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(path)) { Printer.Error("Ruta vacía."); return; }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Printer.Error($"No existe: {path}");
            return;
        }

        Console.WriteLine();
        Printer.Info($"Se moverá a: {Path.Combine(Env.SystemDir, path.TrimStart('/'))}");
        if (!Menu.Confirm("¿Confirmar (requiere sudo)?")) return;

        AddToSystem(path);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Métodos sin UI (usados también por CLI)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Agrega un archivo/carpeta del home a un paquete stow sin UI.
    /// </summary>
    public static void AddToHome(string path, string package)
    {
        if (!path.StartsWith('/'))
            path = Path.Combine(Env.HomeDir, path);

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Summary.TrackErr($"No existe: {path}");
            return;
        }

        string rel = Path.GetRelativePath(Env.HomeDir, path);
        string destInRepo = Path.Combine(Env.DotfilesDir, package, rel);

        if (!Backup.BackupHomePath(path, Env.BackupDir + "_addHomeAction"))
            return;

        if (!Shell.Move(path, destInRepo).Ok)
        {
            Summary.TrackErr($"No se pudo mover: {path}");
            return;
        }
        Summary.TrackOk($"Movido a: {destInRepo}");

        if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, package).Ok)
            Summary.TrackOk($"Symlink creado en: ~/{rel}");

        else

            Summary.TrackErr("stow falló al crear el symlink.");

    }

    /// <summary>
    /// Agrega un archivo/carpeta a system/ sin UI.
    /// </summary>
    public static void AddToSystem(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Summary.TrackErr($"No existe: {path}");
            return;
        }

        string destInRepo = Path.Combine(Env.SystemDir, path.TrimStart('/'));

        if (!Backup.BackupSystemPath(path, Env.BackupDir + "_AddSystemAction"))
            return;

        if (!Shell.Move(path, destInRepo, asSudo: true).Ok)
        {
            Summary.TrackErr($"No se pudo mover: {path}");
            return;
        }
        Summary.TrackOk($"Movido a: {destInRepo}");

        if (Directory.Exists(destInRepo))
        {
            var created = Shell.SymlinkDirectoryContents(destInRepo, path, asSudo: true);
            foreach (string dest in created)

                Summary.TrackOk($"Symlink creado en: {dest}");

        }
        else
        {
            if (Shell.Symlink(destInRepo, path, true).Ok)

                Summary.TrackOk($"Symlink creado en: {path}");

            else

                Summary.TrackErr("No se pudo crear el symlink.");

        }
    }
}
