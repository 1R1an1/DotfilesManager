using DotfilesManager.Core;
using DotfilesManager.UI;
using static DotfilesManager.UI.Colors;

namespace DotfilesManager.Operations;

internal static class ExecuteOp
{
    public static void Run(Summary summary)
    {
        if (!Directory.Exists(Env.ScriptsDir)) Directory.CreateDirectory(Env.ScriptsDir);
        string[] scripts = Directory.GetFiles(Env.ScriptsDir);

        if (scripts.Length == 0)
        {
            Console.Clear();
            Printer.Header("Aplicar perfil");
            Printer.Warn("No hay perfiles guardados.");
            Printer.PressEnterToContinue();
            return;
        }

        int idx = Menu.SelectOne("Selecciona un script a ejecutar", scripts.Select(x => Path.GetFileName(x)).ToArray());

        var script = scripts[idx];

        summary.Reset();
        Printer.Info("Ejecutando el script: " + script);

        if (Shell.Bash(script) == 0)
            summary.TrackOk("Script ejecutado correctamente.");
        else
            summary.TrackErr("Error ejecutando el script");

        summary.Print();
        Printer.PressEnterToContinue();
    }
}
