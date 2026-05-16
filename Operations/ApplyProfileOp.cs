using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class ApplyProfileOp
{
    public static void Run(Summary summary)
    {
        var profiles = ProfileStore.Load();

        if (profiles.Count == 0)
        {
            Console.Clear();
            Printer.Header("Aplicar perfil");
            Printer.Warn("No hay perfiles guardados.");
            Printer.PressEnterToContinue();
            return;
        }

        int idx = Menu.SelectOne("Seleccioná el perfil a aplicar", profiles.Select(p => p.Nombre).ToArray());
        if (idx == -1) return;

        var perfil = profiles[idx];

        Console.Clear();
        Printer.Header($"Aplicar: {perfil.Nombre}");
        Console.WriteLine();

        foreach (var paso in perfil.Pasos)
        {
            Printer.Info($"Paso {perfil.Pasos.IndexOf(paso) + 1}/{perfil.Pasos.Count}: {paso.Tipo} → {paso.Valor}");
        }

        if (!Menu.Confirm("¿Aplicar este perfil?")) return;

        summary.Reset();
        ApplyProfile(perfil.Nombre, summary);
        summary.Print();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Aplica un perfil por nombre sin interfaz interactiva.
    /// </summary>
    public static void ApplyProfile(string name, Summary? summary = null)
    {
        var profiles = ProfileStore.Load();
        var perfil = profiles.FirstOrDefault(p => p.Nombre == name);

        if (perfil is null)
        {
            Printer.Error($"No se encontró el perfil '{name}'.");
            return;
        }

        Console.WriteLine();
        Printer.Info($"Aplicando perfil: {perfil.Nombre}");

        for (int i = 0; i < perfil.Pasos.Count; i++)
        {
            var paso = perfil.Pasos[i];
            Printer.Info($"--- Paso {i + 1}/{perfil.Pasos.Count}: {paso.Tipo} ---");

            bool ok = paso.Tipo switch
            {
                StepType.Script => ExecuteScriptStep(paso, summary),
                StepType.Dotfile => ApplyOp.ApplyHome(paso.ObtenerItems(), summary),
                StepType.Package => ExecutePackageStep(paso, summary),
                _ => false
            };

            if (!ok)
            {
                Printer.Error($"Perfil '{name}' cancelado por fallo en el paso {i + 1}.");
                summary?.TrackErr($"Perfil cancelado en paso {i + 1}.");
                return;
            }
        }

        Printer.Success($"Perfil '{name}' aplicado correctamente.");
        summary?.TrackOk($"Perfil '{name}' aplicado.");
    }

    private static bool ExecuteScriptStep(ProfileStep paso, Summary? summary)
    {
        string scriptName = paso.Valor.Trim();
        string scriptPath = Path.Combine(Env.ScriptsDir, scriptName);

        if (!File.Exists(scriptPath))
        {
            Printer.Error($"Script no encontrado: {scriptPath}");
            summary?.TrackErr($"Script no encontrado: {scriptName}");
            return false;
        }

        var (code, _, stderr, _) = Shell.Bash(scriptPath, visible: true);
        if (code != 0)
        {
            Printer.Error($"Script falló: {stderr}");
            summary?.TrackErr($"Script falló: {scriptName}");
            return false;
        }

        Printer.Success($"Script ejecutado: {scriptName}");
        summary?.TrackOk($"Script: {scriptName}");
        return true;
    }

    private static bool ExecutePackageStep(ProfileStep paso, Summary? summary)
    {
        // Verificar yay
        if (!Shell.YayInstalled())
        {
            Printer.Warn("yay no está instalado. Instalando...");
            if (!Shell.InstallYay())
            {
                Printer.Error("No se pudo instalar yay.");
                summary?.TrackErr("No se pudo instalar yay.");
                return false;
            }
            Printer.Success("yay instalado.");
        }

        // Actualizar sistema (como antes)
        Printer.Info("Actualizando sistema...");
        if (!Shell.UpdateSystem())
        {
            Printer.Warn("Falló la actualización del sistema, continuando...");
            // no detenemos, solo warning
        }

        string[] packages = paso.ObtenerItems();
        if (packages.Length == 0)
        {
            Printer.Error("No se especificaron paquetes en el paso.");
            summary?.TrackErr("Paso Package sin paquetes.");
            return false;
        }

        Printer.Info($"Instalando paquetes: {string.Join(", ", packages)}");
        bool ok = Shell.InstallPackages(packages);
        if (ok)
        {
            Printer.Success($"Paquetes instalados: {string.Join(", ", packages)}");
            summary?.TrackOk($"Paquetes: {string.Join(", ", packages)}");
        }
        else
        {
            Printer.Error("Falló la instalación de paquetes.");
            summary?.TrackErr("Falló instalación de paquetes.");
        }
        return ok;
    }
}