using DotfilesManager.Core;
using DotfilesManager.Operations;
using DotfilesManager.UI;

namespace DotfilesManager;

internal static class Program
{
    private static Summary _summary = null!;

    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Relanzar con sudo si no somos root, para que las operaciones de sistema funcionen
        if (!Environment.IsPrivilegedProcess)
        {
            System.Diagnostics.Process.Start("sudo", Environment.ProcessPath!)?.WaitForExit();
            return;
        }

        Env.LoadOrInit();
        _summary = new Summary();

        while (true)
        {
            string[] options =
            [
                "Symlinks",
                "Ejecutar script",
                "Perfiles",
            ];

            int choice = Menu.SelectOne("Dotfiles Manager", options);

            switch (choice)
            {
                case -1: Console.WriteLine(); return;
                case 0:
                    Printer.color = Colors.Red;
                    SymlinksMenu();
                    Printer.color = Printer.defaultColor;
                    break;
                case 1:
                    Printer.color = Colors.Yellow;
                    ExecuteOp.Run(_summary);
                    Printer.color = Printer.defaultColor;
                    break;
                case 2:
                    Printer.color = Colors.Purple;
                    ProfilesMenu();
                    Printer.color = Printer.defaultColor;
                    break;
            }
        }
    }

    // ── Menú de Symlinks ──────────────────────────────────────────────────

    private static void SymlinksMenu()
    {
        string[] options =
        [
            "Aplicar / instalar dotfiles",
            "Ver estado de symlinks",
            "Borrar symlinks",
            "Agregar archivo al repo",
        ];

        bool running = true;
        while (running)
        {
            int choice = Menu.SelectOne("Symlinks Manager", options);
            switch (choice)
            {
                case -1: running = false; break;
                case 0: ApplyOp.Run(_summary); break;
                case 1: StatusOp.Run(); break;
                case 2: DeleteOp.Run(_summary); break;
                case 3: AddOp.Run(_summary); break;
            }
        }
    }

    // ── Menú de Perfiles ──────────────────────────────────────────────────

    private static void ProfilesMenu()
    {
        string[] options =
        [
            "Crear perfil",
            "Editar perfil",
            "Aplicar perfil",
        ];

        bool running = true;
        while (running)
        {
            Printer.color = Colors.Purple;
            int choice = Menu.SelectOne("Perfiles", options);
            switch (choice)
            {
                case -1: running = false; break;
                case 0: CreateProfileOp.Run(_summary); break;
                case 1: EditProfileOp.Run(_summary); break;
                case 2: ApplyProfileOp.Run(_summary); break;
            }
            Printer.color = Printer.defaultColor;
        }
    }
}