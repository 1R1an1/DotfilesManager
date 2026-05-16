using DotfilesManager.Core;
using DotfilesManager.Operations;
using DotfilesManager.UI;

namespace DotfilesManager;

internal static class Program
{
    private static Summary _summary = null!;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Env.LoadOrInit();
        _summary = new Summary();

        if (args.Length == 0)
        {
            RunInteractive();
        }
        else
        {
            RunCli(ArgParser.Parse(args));
        }
    }

    private static void RunInteractive()
    {
        while (true)
        {
            string[] options = ["Symlinks", "Ejecutar script", "Perfiles"];
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

    private static void RunCli(CliCommand cmd)
    {
        switch (cmd.Type)
        {
            case CommandType.Apply:
                if (cmd.Profile is not null)
                    ApplyProfileOp.ApplyProfile(cmd.Profile);
                else if (cmd.ApplySystem)
                    ApplyOp.ApplySystem(cmd.SystemPaths);
                else if (cmd.Packages.Length > 0)
                    ApplyOp.ApplyHome(cmd.Packages);
                break;

            case CommandType.Add:
                if (cmd.AddToSystem)
                    AddOp.AddToSystem(cmd.SystemPaths[0]);
                else if (cmd.AddHomePath is not null)
                    AddOp.AddToHome(cmd.AddHomePath, cmd.AddHomePackage ?? "default");
                break;

            case CommandType.Delete:
                if (cmd.DeleteSystem)
                    DeleteOp.DeleteSystem(cmd.SystemPaths, cmd.Action ?? "symlinks");
                else if (cmd.Packages.Length > 0)
                    DeleteOp.DeleteHome(cmd.Packages[0], cmd.Action ?? "symlinks");
                break;

            case CommandType.Status:
                StatusOp.Check();
                break;

            case CommandType.Script:
                ExecuteOp.RunScript(cmd.ScriptName ?? "");
                break;

            case CommandType.Profile:
                ApplyProfileOp.ApplyProfile(cmd.Profile ?? "");
                break;

            case CommandType.CreateProfile:
                CreateProfileOp.Create(cmd.Profile ?? "", cmd.Packages, cmd.Dotfiles);
                break;

            case CommandType.SetDir:
                Env.SetDotfilesDir(cmd.DotfilesDir ?? "");
                break;

            case CommandType.Help:
                ArgParser.ShowHelp();
                break;
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