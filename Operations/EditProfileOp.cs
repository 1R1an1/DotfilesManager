using System;
using System.Collections.Generic;
using System.Linq;
using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class EditProfileOp
{
    public static void Run()
    {
        var profiles = ProfileStore.Load();
        if (profiles.Count == 0)
        {
            Console.Clear();
            Printer.Header("Editar perfil");
            Printer.Warn("No hay perfiles guardados.");
            Printer.PressEnterToContinue();
            return;
        }

        int idx = Menu.SelectOne("Seleccioná el perfil a editar", profiles.Select(p => p.Nombre).ToArray());
        if (idx == -1) return;

        var perfil = profiles[idx];
        EditarPerfil(perfil, profiles);
    }

    private static void EditarPerfil(Profile perfil, List<Profile> allProfiles)
    {
        while (true)
        {
            string[] options = ["Ver pasos", "Agregar paso", "Editar paso", "Eliminar paso", "Mover paso", "Cambiar nombre", "── Volver ──"];
            int choice = Menu.SelectOne($"Editando: {perfil.Nombre}", options);
            if (choice == -1 || choice == options.Length - 1) break;

            switch (choice)
            {
                case 0: VerPasos(perfil); break;
                case 1: AgregarPaso(perfil); break;
                case 2: EditarPaso(perfil); break;
                case 3: EliminarPaso(perfil); break;
                case 4: MoverPaso(perfil); break;
                case 5: CambiarNombre(perfil, allProfiles); break;
            }
            ProfileStore.Save(allProfiles);
        }
    }

    private static void VerPasos(Profile perfil)
    {
        Console.Clear();
        Printer.Header($"Pasos de {perfil.Nombre}");
        if (perfil.Pasos.Count == 0)
        {
            Console.WriteLine("  No hay pasos.");
        }
        else
        {
            for (int i = 0; i < perfil.Pasos.Count; i++)
            {
                var paso = perfil.Pasos[i];
                Console.WriteLine($"  {i + 1}. [{paso.Tipo}] {paso.Valor}");
            }
        }
        Printer.PressEnterToContinue();
    }

    private static void AgregarPaso(Profile perfil)
    {
        string[] tipos = ["Script", "Dotfile (stow)", "Package (yay)"];
        int tipoIdx = Menu.SelectOne("Tipo de paso", tipos);
        if (tipoIdx == -1) return;

        StepType tipo = tipoIdx switch { 0 => StepType.Script, 1 => StepType.Dotfile, 2 => StepType.Package, _ => StepType.Script };
        string valor = "";

        switch (tipo)
        {
            case StepType.Script:
                Console.Write("Nombre del script: ");
                valor = Console.ReadLine()?.Trim() ?? "";
                break;
            case StepType.Dotfile:
                string[] dotfiles = Env.GetPackages();
                if (dotfiles.Length == 0) { Printer.Warn("No hay paquetes stow."); return; }
                int[] sel = Menu.SelectMulti("Seleccioná dotfiles", dotfiles);
                valor = string.Join(",", sel.Select(i => dotfiles[i]));
                break;
            case StepType.Package:
                var pkgs = PackageSearch.Run("Seleccioná paquetes", []);
                valor = string.Join(",", pkgs);
                break;
        }

        if (!string.IsNullOrWhiteSpace(valor))
            perfil.Pasos.Add(new ProfileStep { Tipo = tipo, Valor = valor });
    }

    private static void EditarPaso(Profile perfil)
    {
        if (perfil.Pasos.Count == 0) { Printer.Warn("No hay pasos."); return; }
        string[] nombres = perfil.Pasos.Select((p, i) => $"{i + 1}. [{p.Tipo}] {p.Valor}").ToArray();
        int idx = Menu.SelectOne("Seleccioná paso a editar", nombres);
        if (idx == -1) return;

        var paso = perfil.Pasos[idx];
        // Editar valor según tipo
        switch (paso.Tipo)
        {
            case StepType.Script:
                string[] scripts = ExecuteOp.GetScripts();
                if (scripts.Length == 0)
                {
                    Printer.Warn("No hay scripts.");
                    break;
                }
                int sIdx = Menu.SelectOne($"Nuevo script (actual: {paso.Valor})", scripts);
                if (sIdx != -1)
                    paso.Valor = scripts[sIdx];
                break;
            case StepType.Dotfile:
                string[] dotfiles = Env.GetPackages();
                bool[] presel = dotfiles.Select(d => paso.ObtenerItems().Contains(d)).ToArray();
                int[] sel = Menu.SelectMulti("Seleccioná dotfiles", dotfiles, presel);
                paso.Valor = string.Join(",", sel.Select(i => dotfiles[i]));
                break;
            case StepType.Package:
                var pkgs = PackageSearch.Run("Seleccioná paquetes", paso.ObtenerItems());
                paso.Valor = string.Join(",", pkgs);
                break;
        }
    }

    private static void EliminarPaso(Profile perfil)
    {
        if (perfil.Pasos.Count == 0) { Printer.Warn("No hay pasos."); return; }
        string[] nombres = perfil.Pasos.Select((p, i) => $"{i + 1}. [{p.Tipo}] {p.Valor}").ToArray();
        int idx = Menu.SelectOne("Seleccioná paso a eliminar", nombres);
        if (idx != -1 && Menu.Confirm($"¿Eliminar paso {idx + 1}?"))
            perfil.Pasos.RemoveAt(idx);
    }

    private static void MoverPaso(Profile perfil)
    {
        if (perfil.Pasos.Count < 2) { Printer.Warn("Se necesitan al menos 2 pasos."); return; }

        string[] nombres = perfil.Pasos.Select((p, i) => $"{i + 1}. [{p.Tipo}] {p.Valor}").ToArray();
        int idx = Menu.SelectOne("Seleccioná paso a mover", nombres);
        if (idx == -1) return;

        // Mostrar posible rango de destino
        Console.WriteLine();
        Console.Write($"  Mover al número (1-{perfil.Pasos.Count}): ");
        string? input = Console.ReadLine()?.Trim();
        if (!int.TryParse(input, out int newPos) || newPos < 1 || newPos > perfil.Pasos.Count)
        {
            Printer.Error("Número inválido.");
            Printer.PressEnterToContinue();
            return;
        }

        // Si el destino es la misma posición, no hacer nada
        if (newPos - 1 == idx)
        {
            Printer.Warn("El paso ya está en esa posición.");
            Printer.PressEnterToContinue();
            return;
        }

        var paso = perfil.Pasos[idx];

        // Caso especial: mover al final de la lista
        if (newPos == perfil.Pasos.Count)
        {
            perfil.Pasos.RemoveAt(idx);
            perfil.Pasos.Add(paso);
        }
        else
        {
            int newIdx = newPos - 1;
            perfil.Pasos.RemoveAt(idx);
            // Ajustar índice si la inserción es después del elemento eliminado
            if (newIdx > idx) newIdx--;
            perfil.Pasos.Insert(newIdx, paso);
        }

        Printer.Success($"Paso movido de posición {idx + 1} a {newPos}.");
        Printer.PressEnterToContinue();
    }

    private static void CambiarNombre(Profile perfil, List<Profile> allProfiles)
    {
        Console.Write($"  Nuevo nombre [{perfil.Nombre}]: ");
        string? nuevo = Console.ReadLine()?.Trim();

        if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != perfil.Nombre)
        {
            if (allProfiles.Any(p => p.Nombre == nuevo))
            {
                Printer.Error($"Ya existe un perfil con el nombre '{nuevo}'.");
                return;
            }
            perfil.Nombre = nuevo;
            Printer.Success($"Nombre cambiado a '{nuevo}'.");
        }
    }

    // Métodos CLI sin UI (solo los necesarios)
    public static void EditName(string oldName, string newName)
    {
        var profiles = ProfileStore.Load();
        var perfil = profiles.FirstOrDefault(p => p.Nombre == oldName);
        if (perfil is null) { Printer.Error($"Perfil '{oldName}' no encontrado."); return; }
        if (profiles.Any(p => p.Nombre == newName)) { Printer.Error($"Ya existe '{newName}'."); return; }
        perfil.Nombre = newName;
        ProfileStore.Save(profiles);
        Printer.Success($"Nombre cambiado a '{newName}'.");
    }

    public static void EditPackages(string name, string[] packages)
    {
        // Buscar primer paso Package y reemplazar
        var profiles = ProfileStore.Load();
        var perfil = profiles.FirstOrDefault(p => p.Nombre == name);
        if (perfil is null) { Printer.Error($"Perfil '{name}' no encontrado."); return; }
        var paso = perfil.Pasos.FirstOrDefault(p => p.Tipo == StepType.Package);
        if (paso is null)
            perfil.Pasos.Add(new ProfileStep { Tipo = StepType.Package, Valor = string.Join(",", packages) });
        else
            paso.Valor = string.Join(",", packages);
        ProfileStore.Save(profiles);
        Printer.Success("Paquetes actualizados.");
    }

    public static void EditDotfiles(string name, string[] dotfiles)
    {
        var profiles = ProfileStore.Load();
        var perfil = profiles.FirstOrDefault(p => p.Nombre == name);
        if (perfil is null) { Printer.Error($"Perfil '{name}' no encontrado."); return; }
        var paso = perfil.Pasos.FirstOrDefault(p => p.Tipo == StepType.Dotfile);
        if (paso is null)
            perfil.Pasos.Add(new ProfileStep { Tipo = StepType.Dotfile, Valor = string.Join(",", dotfiles) });
        else
            paso.Valor = string.Join(",", dotfiles);
        ProfileStore.Save(profiles);
        Printer.Success("Dotfiles actualizados.");
    }
}
