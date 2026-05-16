using System.Linq;
using DotfilesManager.Core;
using DotfilesManager.UI;

namespace DotfilesManager.Operations;

internal static class CreateProfileOp
{
    public static void Run(Summary summary)
    {
        Console.Clear();
        Printer.Header("Crear perfil");

        Console.Write("  Nombre del perfil: ");
        string? nombre = Console.ReadLine()?.Trim();

        if (nombre is null)
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

        // Seleccionar dotfiles
        string[] pkgs = Env.GetPackages();
        int[] dotfilesIdx = pkgs.Length > 0
            ? Menu.SelectMulti("Seleccioná los dotfiles del perfil", pkgs)
            : [];

        // Seleccionar paquetes
        Console.Clear();
        Printer.Header("Crear perfil");
        var paquetes = PackageSearch.Run("Seleccioná los paquetes del perfil", []).ToList();

        var perfil = new Profile
        {
            Nombre = nombre,
            Paquetes = paquetes,
            Dotfiles = dotfilesIdx.Select(i => pkgs[i]).ToList(),
        };

        profiles.Add(perfil);
        ProfileStore.Save(profiles);

        Console.Clear();
        Printer.Header("Crear perfil");
        Console.WriteLine();
        Printer.Success($"Perfil '{nombre}' creado.");
        Printer.Info($"Paquetes: {(paquetes.Count > 0 ? string.Join(", ", paquetes) : "ninguno")}");
        Printer.Info($"Dotfiles: {(perfil.Dotfiles.Count > 0 ? string.Join(", ", perfil.Dotfiles) : "ninguno")}");

        summary.TrackOk($"Perfil '{nombre}' creado.");
        summary.Print();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Crea un perfil sin interfaz interactiva.
    /// </summary>
    public static void Create(string name, string[] packages, string[] dotfiles)
    {
        var profiles = ProfileStore.Load();

        if (profiles.Any(p => p.Nombre == name))
        {
            Printer.Error($"Ya existe un perfil con el nombre '{name}'.");
            return;
        }

        var perfil = new Profile
        {
            Nombre = name,
            Paquetes = [.. packages],
            Dotfiles = [.. dotfiles],
        };

        profiles.Add(perfil);
        ProfileStore.Save(profiles);
        Printer.Success($"Perfil '{name}' creado.");
    }
}