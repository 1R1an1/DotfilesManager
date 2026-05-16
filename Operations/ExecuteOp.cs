using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class ExecuteOp
{
    public static void Run(Summary summary)
    {
        if (!Directory.Exists(Env.ScriptsDir))
            Directory.CreateDirectory(Env.ScriptsDir);

        string[] scripts = Directory.GetFiles(Env.ScriptsDir);

        if (scripts.Length == 0)
        {
            Console.Clear();
            Printer.Header("Ejecutar script");
            Printer.Warn($"No hay scripts en {Env.ScriptsDir}");
            Printer.PressEnterToContinue();
            return;
        }

        // Mostrar solo el nombre del archivo en el menú, no la ruta completa
        string?[] scriptNames = scripts.Select(Path.GetFileName!).ToArray();
        int idx = Menu.SelectOne("Seleccioná un script a ejecutar", scriptNames!);

        if (idx == -1) return;

        summary.Reset();
        Console.Clear();
        Printer.Header("Ejecutar script");
        Console.WriteLine();
        Printer.Info($"Ejecutando: {scriptNames[idx]}");
        Console.WriteLine();

        if (Shell.Bash(scripts[idx]).ExitCode == 0)
            summary.TrackOk("Script ejecutado correctamente.");
        else
            summary.TrackErr("El script terminó con error.");

        summary.Print();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Ejecuta un script por nombre sin interfaz interactiva.
    /// </summary>
    public static void RunScript(string name)
    {
        string scriptPath = Path.Combine(Env.ScriptsDir, name);

        if (!File.Exists(scriptPath))
        {
            Printer.Error($"Script no encontrado: {scriptPath}");
            return;
        }

        var (code, stdout, stderr, _) = Shell.Bash(scriptPath, visible: true);
        if (code == 0)
            Printer.Success($"Script ejecutado: {name}");
        else
            Printer.Error($"Script falló ({code}): {stderr}");
    }

    public static string[] GetScripts() =>
    Directory.Exists(Env.ScriptsDir)
        ? Directory.GetFiles(Env.ScriptsDir)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Cast<string>()
            .OrderBy(n => n)
            .ToArray()
        : [];
}
