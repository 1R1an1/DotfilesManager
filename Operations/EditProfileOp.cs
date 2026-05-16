using System.Collections.Generic;
using System.Linq;
using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class EditProfileOp
{
    public static void Run(Summary summary)
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

        string[] editOptions = ["Cambiar nombre", "Editar paquetes", "Editar dotfiles"];

        while (true)
        {
            int action = Menu.SelectOne($"Editando: {perfil.Nombre}", editOptions);
            if (action == -1) break;

            switch (action)
            {
                case 0: EditarNombre(perfil, profiles, summary); break;
                case 1: EditarPaquetes(perfil, profiles, summary); break;
                case 2: EditarDotfiles(perfil, profiles, summary); break;
            }
        }
    }

    private static void EditarNombre(Profile perfil, List<Profile> profiles, Summary summary)
    {
        Console.Clear();
        Printer.Header("Cambiar nombre");

        Console.Write("  Nuevo nombre: ");
        string? nuevo = Console.ReadLine()?.Trim();

        if (nuevo is null || nuevo == perfil.Nombre) return;

        if (profiles.Any(p => p.Nombre == nuevo))
        {
            Printer.Error($"Ya existe un perfil con el nombre '{nuevo}'.");
            Printer.PressEnterToContinue();
            return;
        }

        perfil.Nombre = nuevo;
        ProfileStore.Save(profiles);
        summary.TrackOk($"Nombre cambiado a '{nuevo}'.");
        summary.Print();
        Printer.PressEnterToContinue();
    }

    private static void EditarPaquetes(Profile perfil, List<Profile> profiles, Summary summary)
    {
        string[] result = PackageSearch.Run(
            $"Paquetes — {perfil.Nombre}",
            perfil.Paquetes
        );

        perfil.Paquetes = result.ToList();
        ProfileStore.Save(profiles);
        summary.TrackOk("Paquetes actualizados.");
        summary.Print();
        Printer.PressEnterToContinue();
    }

    private static void EditarDotfiles(Profile perfil, List<Profile> profiles, Summary summary)
    {
        string[] allPkgs = Env.GetPackages();
        if (allPkgs.Length == 0)
        {
            Console.Clear();
            Printer.Header("Editar dotfiles");
            Printer.Warn("No hay paquetes stow en el repo.");
            Printer.PressEnterToContinue();
            return;
        }

        bool[] presel = allPkgs.Select(p => perfil.Dotfiles.Contains(p)).ToArray();
        int[] selected = Menu.SelectMulti(
            $"Dotfiles — {perfil.Nombre}",
            allPkgs,
            presel
        );

        perfil.Dotfiles = selected.Select(i => allPkgs[i]).ToList();
        ProfileStore.Save(profiles);
        summary.TrackOk("Dotfiles actualizados.");
        summary.Print();
        Printer.PressEnterToContinue();
    }
}