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
}
