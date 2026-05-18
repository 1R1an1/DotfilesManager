using System;
using System.IO;
using System.Linq;
using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class ExportProfileOp
{
    public static void Run()
    {
        var profiles = ProfileStore.Load();
        if (profiles.Count == 0)
        {
            Console.Clear();
            Printer.Header("Exportar perfil");
            Printer.Warn("No hay perfiles guardados.");
            Printer.PressEnterToContinue();
            return;
        }

        int idx = Menu.SelectOne("Seleccioná perfil a exportar", profiles.Select(p => p.Nombre).ToArray());
        if (idx == -1) return;

        var perfil = profiles[idx];
        string scriptPath = Path.Combine(Env.ScriptsDir, $"{perfil.Nombre}.sh");
        string contenido = GenerarScript(perfil);
        File.WriteAllText(scriptPath, contenido);
        Summary.TrackOk($"Perfil '{perfil.Nombre}' exportado al script {scriptPath}");
        Summary.Print();
        Printer.PressEnterToContinue();
    }

    public static string GenerarScript(Profile perfil)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("#!/bin/bash");
        sb.AppendLine($"# Perfil: {perfil.Nombre}");
        sb.AppendLine("set -e");
        sb.AppendLine($"DOTFILES_DIR=\"{Env.DotfilesDir}\"");
        sb.AppendLine($"HOME_DIR=\"{Env.HomeDir}\"");
        sb.AppendLine();

        foreach (var paso in perfil.Pasos)
        {
            switch (paso.Tipo)
            {
                case StepType.Script:
                    sb.AppendLine($"echo 'Ejecutando script: {paso.Valor}'");
                    sb.AppendLine($"bash \"$DOTFILES_DIR/.scripts/{paso.Valor}\"");
                    break;
                case StepType.Dotfile:
                    var dotfiles = paso.ObtenerItems();
                    sb.AppendLine($"echo 'Aplicando dotfiles: {string.Join(", ", dotfiles)}'");
                    foreach (var d in dotfiles)
                        sb.AppendLine($"stow --no-folding -d \"$DOTFILES_DIR\" -t \"$HOME_DIR\" \"{d}\"");
                    break;
                case StepType.Package:
                    var paquetes = paso.ObtenerItems();
                    sb.AppendLine($"echo 'Instalando paquetes: {string.Join(", ", paquetes)}'");
                    sb.AppendLine($"yay -S --noconfirm {string.Join(" ", paquetes)}");
                    break;
            }
            sb.AppendLine();
        }

        sb.AppendLine("echo 'Perfil aplicado correctamente.'");
        return sb.ToString();
    }

    // Método CLI
    public static void Export(string name)
    {
        var profiles = ProfileStore.Load();
        var perfil = profiles.FirstOrDefault(p => p.Nombre == name);
        if (perfil is null)
        {
            Printer.Error($"Perfil '{name}' no encontrado.");
            return;
        }
        string scriptPath = Path.Combine(Env.HomeDir, $"{perfil.Nombre}.sh");
        File.WriteAllText(scriptPath, GenerarScript(perfil));
        Printer.Success($"Perfil exportado a: {scriptPath}");
    }
}
