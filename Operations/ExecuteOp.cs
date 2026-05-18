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

        RunScript(scriptNames[idx]!, summary);

        summary.Print();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Ejecuta un script por nombre sin interfaz interactiva.
    /// </summary>
    public static bool RunScript(string name, Summary? summary = null)
    {
        string scriptPath = Path.Combine(Env.ScriptsDir, name);

        if (!File.Exists(scriptPath))
        {
            Messenger.Error($"Script no encontrado: {scriptPath}", summary);
            return false;
        }

        var (code, _, stderr, _) = Shell.Bash(scriptPath, visible: true);
        if (code != 0)
        {
            Messenger.Error($"Script falló ({code}): {stderr}", summary);
            return false;
        }

        Messenger.Success($"Script ejecutado: {name}", summary);
        return true;
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
