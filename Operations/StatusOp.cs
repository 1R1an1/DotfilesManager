using DotfilesManager.Core;
using DotfilesManager.UI;
using static DotfilesManager.UI.Colors;

namespace DotfilesManager.Operations;

internal static class StatusOp
{
    public static void Run()
    {
        Console.Clear();
        Printer.Header("Estado de symlinks");

        string[] packages = Env.GetPackages();
        if (packages.Length == 0)
        {
            Printer.Warn("No hay paquetes stow.");
            Printer.PressEnterToContinue();
            return;
        }

        Console.WriteLine();

        foreach (string pkg in packages)
        {
            var status = StatusChecker.Check(pkg);

            string icon = status.Overall switch
            {
                LinkStatus.Broken => $"{Red}✘ problema{Reset}",
                LinkStatus.Conflict => $"{Yellow}○ parcial{Reset}",
                LinkStatus.NotApplied => $"{Dim}— no aplicado{Reset}",
                LinkStatus.Ok => $"{Green}✔ activo{Reset}",
                _ => $"{Dim}— vacío{Reset}",
            };

            // %-30s alinea el nombre del paquete a 30 caracteres para que los iconos queden en columna
            Console.WriteLine($"  {pkg,-30}{icon}");

            if (status.Ok > 0) Console.WriteLine($"    {Dim}symlinks ok:  {status.Ok}{Reset}");
            if (status.Broken > 0) Console.WriteLine($"    {Red}rotos:        {status.Broken}{Reset}");
            if (status.Conflict > 0) Console.WriteLine($"    {Red}conflictos:   {status.Conflict}{Reset}");
            if (status.NotApplied > 0) Console.WriteLine($"    {Yellow}no aplicados: {status.NotApplied}{Reset}");
        }

        Console.WriteLine();
        Printer.PressEnterToContinue();
    }

    /// <summary>
    /// Muestra el estado de los symlinks sin interfaz interactiva.
    /// </summary>
    public static void Check()
    {
        Printer.Info("Verificando estado de symlinks...");

        if (Directory.Exists(Env.DotfilesDir))
        {
            foreach (string pkg in Env.GetPackages())
            {
                var status = StatusChecker.Check(pkg);
                string estado = status.Overall switch
                {
                    LinkStatus.Ok => "OK",
                    LinkStatus.Conflict => "Parcial",
                    LinkStatus.NotApplied => "No aplicado",
                    LinkStatus.Broken => "Rotos",
                    _ => "Vacío"
                };
                List<string> detalles = new();
                if (status.Ok > 0) detalles.Add($"{Dim}ok:{status.Ok}{Reset}");
                if (status.Conflict > 0) detalles.Add($"{Red}conflictos:{status.Conflict}{Reset}");
                if (status.NotApplied > 0) detalles.Add($"{Yellow}sin aplicar:{status.NotApplied}{Reset}");
                if (status.Broken > 0) detalles.Add($"{Red}rotos:{status.Broken}{Reset}");

                string detalle = detalles.Count > 0 ? $" | {string.Join(" ", detalles)}" : "";
                Printer.Info($"  {pkg}: {estado}{detalle}");
            }
        }

        // if (Directory.Exists(Env.SystemDir))
        // {
        //     Printer.Info("Symlinks de sistema:");
        //     foreach (string file in Directory.GetFiles(Env.SystemDir, "*", SearchOption.AllDirectories))
        //     {
        //         string dest = "/" + Path.GetRelativePath(Env.SystemDir, file);
        //         bool exists = File.Exists(dest) || Directory.Exists(dest);
        //         bool isSymlink = exists && new FileInfo(dest).Attributes.HasFlag(FileAttributes.ReparsePoint);
        //         Printer.Info($"  {dest}: {(isSymlink ? "OK" : "NO APLICADO")}");
        //     }
        // }
    }
}
