using System;
using System.Linq;
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
            Printer.Info($"Paso {perfil.Pasos.IndexOf(paso) + 1}/{perfil.Pasos.Count}: {paso.Tipo} → {paso.Valor}");


        if (!Menu.Confirm("¿Aplicar este perfil?")) return;

        summary.Reset();
        ApplyProfile(perfil.Nombre, summary);
        summary.Print();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Aplica un perfil por nombre sin interfaz interactiva.
    /// </summary>
    public static void ApplyProfile(string name, Summary? summary = null, int startStep = 0)
    {
        var profiles = ProfileStore.Load();
        var perfil = profiles.FirstOrDefault(p => p.Nombre == name);

        if (perfil is null)
        {
            Printer.Error($"No se encontró el perfil '{name}'.");
            return;
        }

        // Validar startStep
        int startIndex = startStep > 0 ? startStep - 1 : 0;
        if (startIndex >= perfil.Pasos.Count)
        {
            Printer.Error($"El paso {startStep} no existe en el perfil (tiene {perfil.Pasos.Count} pasos).");
            return;
        }

        Console.WriteLine();
        Printer.Info($"Aplicando perfil: {perfil.Nombre}" + (startStep > 0 ? $" (desde paso {startStep})" : ""));

        // ── Verificar/instalar yay UNA SOLA VEZ ──────────────────────────
        if (!Shell.YayInstalled())
        {
            Printer.Warn("yay no está instalado. Instalando...");
            if (!Shell.InstallYay())
            {
                Messenger.Error("No se pudo instalar yay. Abortando perfil.", summary);
                return;
            }
            Printer.Success("yay instalado.");
        }
        else
            Printer.Info("yay encontrado.");


        // ── Actualizar sistema UNA SOLA VEZ ──────────────────────────────
        Printer.Info("Actualizando sistema...");
        if (!Shell.UpdateSystem())
            Printer.Warn("Falló la actualización del sistema, continuando...");


        // ── Ejecutar pasos desde startIndex ─────────────────────────────
        for (int i = startIndex; i < perfil.Pasos.Count; i++)
        {
            var paso = perfil.Pasos[i];
            int pasoNum = i + 1;
            Printer.Info($"--- Paso {pasoNum}/{perfil.Pasos.Count}: {paso.Tipo} ---");

            bool ok = paso.Tipo switch
            {
                StepType.Script => ExecuteScriptStep(paso, summary),
                StepType.Dotfile => ApplyOp.ApplyHome(paso.ObtenerItems(), summary),
                StepType.Package => ExecutePackageStep(paso, summary),
                _ => false
            };

            if (!ok)
            {
                Messenger.Error($"Perfil '{name}' cancelado por fallo en el paso {pasoNum}.", summary);
                return;
            }
        }

        Messenger.Success($"Perfil '{name}' aplicado correctamente.", summary);
    }

    // ── Paso: Script ────────────────────────────────────────────────────
    private static bool ExecuteScriptStep(ProfileStep paso, Summary? summary)
    {
        string scriptName = paso.Valor.Trim();
        string scriptPath = Path.Combine(Env.ScriptsDir, scriptName);

        if (!File.Exists(scriptPath))
        {
            Messenger.Error($"Script no encontrado: {scriptPath}", summary);
            return false;
        }

        var (code, _, stderr, _) = Shell.Bash(scriptPath, visible: true);
        if (code != 0)
        {
            Messenger.Error($"Script '{scriptName}' falló: {stderr}", summary);
            return false;
        }

        Messenger.Success($"Script ejecutado: {scriptName}", summary);
        return true;
    }

    // ── Paso: Package (yay) ─────────────────────────────────────────────
    private static bool ExecutePackageStep(ProfileStep paso, Summary? summary)
    {
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
            Messenger.Success($"Paquetes instalados: {string.Join(", ", packages)}", summary);
        else
            Messenger.Error("Falló la instalación de paquetes.", summary);

        return ok;
    }
}