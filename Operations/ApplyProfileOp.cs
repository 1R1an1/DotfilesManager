using System;
using System.Linq;
using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class ApplyProfileOp
{
    public static void Run()
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

        Summary.Reset();
        ApplyProfile(perfil.Nombre);
        Summary.Print();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Aplica un perfil por nombre sin interfaz interactiva.
    /// </summary>
    public static void ApplyProfile(string name, int startStep = 0)
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
                Summary.TrackErr("No se pudo instalar yay. Abortando perfil.");
                return;
            }
            Printer.Success("yay instalado.");
        }
        else
            Printer.Info("yay encontrado.");


        // ── Actualizar sistema UNA SOLA VEZ ──────────────────────────────
        if (startIndex == 0)
        {
            Printer.Info("Actualizando sistema...");
            if (!Shell.UpdateSystem())
                Printer.Warn("Falló la actualización del sistema, continuando...");
        }


        // ── Ejecutar pasos desde startIndex ─────────────────────────────
        for (int i = startIndex; i < perfil.Pasos.Count; i++)
        {
            var paso = perfil.Pasos[i];
            int pasoNum = i + 1;
            Printer.Info($"--- Paso {pasoNum}/{perfil.Pasos.Count}: {paso.Tipo} ---");

            bool ok = paso.Tipo switch
            {
                StepType.Script => ExecuteOp.RunScript(paso.Valor),
                StepType.Dotfile => ApplyOp.ApplyHome(paso.ObtenerItems()),
                StepType.Package => ExecutePackageStep(paso),
                _ => false
            };

            if (!ok)
            {
                Summary.TrackErr($"Perfil '{name}' cancelado por fallo en el paso {pasoNum}.");
                return;
            }
        }

        Summary.TrackOk($"Perfil '{name}' aplicado correctamente.");
    }

    // ── Paso: Package (yay) ─────────────────────────────────────────────
    private static bool ExecutePackageStep(ProfileStep paso)
    {
        string[] packages = paso.ObtenerItems();
        if (packages.Length == 0)
        {
            Printer.Error("No se especificaron paquetes en el paso.");
            Summary.TrackErr("Paso Package sin paquetes.");
            return false;
        }

        Printer.Info($"Instalando paquetes: {string.Join(", ", packages)}");
        bool ok = Shell.InstallPackages(packages);
        if (ok)
            Summary.TrackOk($"Paquetes instalados: {string.Join(", ", packages)}");
        else
            Summary.TrackErr("Falló la instalación de paquetes.");

        return ok;
    }
}
