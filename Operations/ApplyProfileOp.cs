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

        Printer.Info($"Paquetes : {(perfil.Paquetes.Count > 0 ? string.Join(", ", perfil.Paquetes) : "ninguno")}");
        Printer.Info($"Dotfiles : {(perfil.Dotfiles.Count > 0 ? string.Join(", ", perfil.Dotfiles) : "ninguno")}");

        if (!Menu.Confirm("¿Aplicar este perfil?")) return;

        summary.Reset();

        // ── Verificar/instalar yay ────────────────────────────────────────
        if (perfil.Paquetes.Count > 0)
        {
            Console.WriteLine();
            Printer.Info("Verificando yay...");

            if (!Shell.YayInstalled())
            {
                Printer.Warn("yay no está instalado. Instalando...");
                Printer.PressEnterToContinue();

                if (Shell.InstallYay())
                    summary.TrackOk("yay instalado.");
                else
                {
                    summary.TrackErr("No se pudo instalar yay. Abortando instalación de paquetes.");
                    goto applyDotfiles;
                }
            }
            else
            {
                Printer.Info("yay encontrado.");
            }

            // Actualizar sistema
            Console.WriteLine();
            Printer.Info("Actualizando el sistema...");

            if (Shell.UpdateSystem())
                summary.TrackOk("Sistema actualizado.");
            else
                summary.TrackErr("Falló la actualización del sistema.");

            // Instalar paquetes
            Console.WriteLine();
            Printer.Info($"Instalando {perfil.Paquetes.Count} paquete(s)...");
            Printer.PressEnterToContinue();

            if (Shell.InstallPackages([.. perfil.Paquetes]))
                summary.TrackOk($"Paquetes instalados: {string.Join(", ", perfil.Paquetes)}");
            else
                summary.TrackErr("Falló la instalación de algunos paquetes.");
        }

    applyDotfiles:

        // ── Aplicar dotfiles ──────────────────────────────────────────────
        if (perfil.Dotfiles.Count > 0)
        {
            Console.WriteLine();
            Printer.Info($"Aplicando {perfil.Dotfiles.Count} paquete(s) de dotfiles...");
            Printer.PressEnterToContinue();
            Console.WriteLine();

            foreach (string pkg in perfil.Dotfiles)
            {
                if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg).Ok)
                    summary.TrackOk($"stow: {pkg}");
                else
                    summary.TrackErr($"stow falló: {pkg}");
            }
        }

        summary.Print();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Aplica un perfil por nombre sin interfaz interactiva.
    /// </summary>
    public static void ApplyProfile(string name)
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

        if (perfil.Paquetes.Count > 0)
        {
            Printer.Info("Verificando yay...");
            if (!Shell.YayInstalled())
            {
                Printer.Warn("yay no está instalado. Instalando...");
                if (!Shell.InstallYay())
                {
                    Printer.Error("No se pudo instalar yay. Abortando paquetes.");
                    goto applyDotfiles;
                }
            }

            Printer.Info("Actualizando sistema...");
            Shell.UpdateSystem();

            Printer.Info($"Instalando {perfil.Paquetes.Count} paquete(s): {string.Join(", ", perfil.Paquetes)}");
            Shell.InstallPackages([.. perfil.Paquetes]);
        }

    applyDotfiles:
        if (perfil.Dotfiles.Count > 0)
        {
            Printer.Info($"Aplicando {perfil.Dotfiles.Count} dotfile(s): {string.Join(", ", perfil.Dotfiles)}");
            foreach (string pkg in perfil.Dotfiles)
            {
                if (Shell.Stow(Env.DotfilesDir, Env.HomeDir, pkg).Ok)
                    Printer.Success($"stow: {pkg}");
                else
                    Printer.Error($"stow falló: {pkg}");
            }
        }

        Printer.Success($"Perfil '{name}' aplicado.");
    }
}