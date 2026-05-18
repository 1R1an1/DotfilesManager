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

        if (choice == 0) AddFromHomeUI(summary);
        else AddFromSystemUI(summary);

        summary.Print();
        Printer.PressEnterToContinue();
    }

    // ── UI: pide datos y llama al método sin UI ───────────────────────────
    private static void AddFromHomeUI(Summary summary)
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

        AddToHome(path, package, summary);
    }

    // ── UI: pide ruta y llama al método sin UI ────────────────────────────
    private static void AddFromSystemUI(Summary summary)
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

        AddToSystem(path, summary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Métodos sin UI (usados también por CLI)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Agrega un archivo/carpeta del home a un paquete stow sin UI.
    /// </summary>
    public static void AddToHome(string path, string package, Summary? summary = null)
    {
        if (!path.StartsWith('/'))
            path = Path.Combine(Env.HomeDir, path);

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Messenger.Error($"No existe: {path}", summary);
            return;
        }

        string rel = Path.GetRelativePath(Env.HomeDir, path);
        string destInRepo = Path.Combine(Env.DotfilesDir, package, rel);

        if (!Backup.BackupHomePath(path, Env.BackupDir + "_addHomeAction", summary))
            return;

        if (!Shell.Move(path, destInRepo).Ok)
        {
            Messenger.Error($"No se pudo mover: {path}", summary);
            return;
        }
        Messenger.Success($"Movido a: {destInRepo}", summary);

        if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, package).Ok)
            Messenger.Success($"Symlink creado en: ~/{rel}", summary);

        else

            Messenger.Error("stow falló al crear el symlink.", summary);

    }

    /// <summary>
    /// Agrega un archivo/carpeta a system/ sin UI.
    /// </summary>
    public static void AddToSystem(string path, Summary? summary = null)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Messenger.Error($"No existe: {path}", summary);
            return;
        }

        string destInRepo = Path.Combine(Env.SystemDir, path.TrimStart('/'));

        if (!Backup.BackupSystemPath(path, Env.BackupDir + "_AddSystemAction", summary))
            return;

        if (!Shell.Move(path, destInRepo, asSudo: true).Ok)
        {
            Messenger.Error($"No se pudo mover: {path}", summary);
            return;
        }
        Messenger.Success($"Movido a: {destInRepo}", summary);

        if (Directory.Exists(destInRepo))
        {
            var created = Shell.SymlinkDirectoryContents(destInRepo, path, asSudo: true);
            foreach (string dest in created)

                Messenger.Success($"Symlink creado en: {dest}", summary);

        }
        else
        {
            if (Shell.Symlink(destInRepo, path, true).Ok)

                Messenger.Success($"Symlink creado en: {path}", summary);

            else

                Messenger.Error("No se pudo crear el symlink.", summary);

        }
    }
}
