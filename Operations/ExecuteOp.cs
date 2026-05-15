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
}
