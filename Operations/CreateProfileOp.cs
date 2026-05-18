using System;
using System.Collections.Generic;
using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class CreateProfileOp
{
    public static void Run()
    {
        Console.Clear();
        Printer.Header("Crear perfil");

        Console.Write("  Nombre del perfil: ");
        string? nombre = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(nombre))
        {
            Printer.Warn("Nombre vacío, cancelado.");
            Printer.PressEnterToContinue();
            return;
        }

        var profiles = ProfileStore.Load();
        if (profiles.Any(p => p.Nombre == nombre))
        {
            Printer.Error($"Ya existe un perfil con el nombre '{nombre}'.");
            Printer.PressEnterToContinue();
            return;
        }

        var pasos = new List<ProfileStep>();

        while (true)
        {
            string[] stepTypeOptions = ["Script", "Dotfile (stow)", "Package (yay)", "── Finalizar y guardar ──"];
            int tipoIdx = Menu.SelectOne("Agregar paso — tipo", stepTypeOptions);
            if (tipoIdx == -1 || tipoIdx == 3) break; // cancelar o finalizar

            StepType tipo = tipoIdx switch { 0 => StepType.Script, 1 => StepType.Dotfile, 2 => StepType.Package, _ => StepType.Script };

            string valor = "";
            switch (tipo)
            {
                case StepType.Script:
                    string[] scripts = ExecuteOp.GetScripts();
                    if (scripts.Length == 0)
                    {
                        Printer.Warn($"No hay scripts en: {Env.ScriptsDir}.");
                        return;
                    }
                    int sIdx = Menu.SelectOne("Seleccioná script", scripts);
                    if (sIdx != -1)
                        valor = scripts[sIdx];
                    break;
                case StepType.Dotfile:
                    string[] dotfiles = Env.GetPackages();
                    if (dotfiles.Length == 0)
                    {
                        Printer.Warn("No hay paquetes stow disponibles.");
                        continue;
                    }
                    int[] selected = Menu.SelectMulti("Seleccioná dotfiles", dotfiles);
                    valor = string.Join(",", selected.Select(i => dotfiles[i]));
                    break;
                case StepType.Package:
                    var paquetes = PackageSearch.Run("Seleccioná paquetes", []);
                    valor = string.Join(",", paquetes);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(valor))
            {
                pasos.Add(new ProfileStep { Tipo = tipo, Valor = valor });
                Printer.Success($"Paso agregado: {tipo} → {valor}");
            }
        }

        if (pasos.Count == 0)
        {
            Printer.Warn("No se agregaron pasos, perfil cancelado.");
            Printer.PressEnterToContinue();
            return;
        }

        var perfil = new Profile { Nombre = nombre, Pasos = pasos };
        profiles.Add(perfil);
        ProfileStore.Save(profiles);

        Console.Clear();
        Printer.Header("Crear perfil");
        Console.WriteLine();
        Printer.Success($"Perfil '{nombre}' creado con {pasos.Count} paso(s).");
        Summary.TrackOk($"Perfil '{nombre}' creado.");
        Summary.Print();
        Printer.PressEnterToContinue();
    }

    // Método para CLI
    public static void Create(string name, string[] packages, string[] dotfiles)
    {
        var profiles = ProfileStore.Load();
        if (profiles.Any(p => p.Nombre == name))
        {
            Printer.Error($"Ya existe un perfil con el nombre '{name}'.");
            return;
        }

        var pasos = new List<ProfileStep>();
        if (packages.Length > 0)
            pasos.Add(new ProfileStep { Tipo = StepType.Package, Valor = string.Join(",", packages) });
        if (dotfiles.Length > 0)
            pasos.Add(new ProfileStep { Tipo = StepType.Dotfile, Valor = string.Join(",", dotfiles) });

        if (pasos.Count == 0)
        {
            Printer.Error("Se necesita al menos un paquete o dotfile.");
            return;
        }

        var perfil = new Profile { Nombre = name, Pasos = pasos };
        profiles.Add(perfil);
        ProfileStore.Save(profiles);
        Printer.Success($"Perfil '{name}' creado.");
    }
}
