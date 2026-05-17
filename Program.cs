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

        // Watcher para recargar DotfilesDir si cambia config.json
        using var watcher = new FileSystemWatcher(
            Path.GetDirectoryName(Env.ConfigFile)!,
            Path.GetFileName(Env.ConfigFile))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, _) =>
        {
            Thread.Sleep(50); // Pequeña pausa para que termine de escribirse
            Env.ReloadConfig();
            Printer.Info($"DotfilesDir actualizado a: {Env.DotfilesDir}");
        };

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
            case CommandType.Error:
                // El error ya se mostró en ArgParser
                break;

            case CommandType.Help:
                ArgParser.ShowHelp();
                break;

            case CommandType.Apply:
                if (cmd.SystemPaths.Length > 0)
                    ApplyOp.ApplySystem(cmd.SystemPaths);
                else if (cmd.Packages.Length > 0)
                    ApplyOp.ApplyHome(cmd.Packages);
                break;

            case CommandType.Add:
                if (cmd.AddToSystem && cmd.SystemPaths.Length > 0)
                    AddOp.AddToSystem(cmd.SystemPaths[0]);
                else if (cmd.AddHomePath is not null && cmd.AddHomePackage is not null)
                    AddOp.AddToHome(cmd.AddHomePath, cmd.AddHomePackage);
                break;

            case CommandType.Backup:
                if (cmd.Packages.Length > 0)
                {
                    string[]? backedUp = Backup.BackupHomePackage(cmd.Packages[0], Env.BackupDir + "_manualBackupAction");
                    if (backedUp is null) return;
                    Printer.Success($"Backup completado: {backedUp.Length} archivo(s)");
                }
                else if (cmd.AddHomePath is not null)
                {
                    bool ok = Backup.BackupHomePath(cmd.AddHomePath, Env.BackupDir + "_manualBackupAction");
                    if (ok) Printer.Success($"Backup completado: {cmd.AddHomePath}");
                }
                else if (cmd.SystemPaths.Length > 0)
                {
                    bool ok = Backup.BackupSystemPath(cmd.SystemPaths[0], Env.BackupDir + "_manualBackupAction");
                    if (ok) Printer.Success($"Backup completado: {cmd.SystemPaths[0]}");
                }
                break;

            case CommandType.Delete:
                if (cmd.DeleteSystem && cmd.SystemPaths.Length > 0)
                    DeleteOp.DeleteSystem(cmd.SystemPaths, cmd.Action!);
                else if (cmd.Packages.Length > 0)
                    DeleteOp.DeleteHome(cmd.Packages[0], cmd.Action!);
                break;

            case CommandType.Status:
                StatusOp.Check();
                break;

            case CommandType.Script:
                ExecuteOp.RunScript(cmd.ScriptName!);
                break;

            case CommandType.Profile:
                switch (cmd.ProfileAction)
                {
                    case ProfileAction.Create:
                        CreateProfileOp.Create(cmd.Profile!, cmd.Packages, cmd.Dotfiles);
                        break;
                    case ProfileAction.EditName:
                        EditProfileOp.EditName(cmd.Profile!, cmd.NewName!);
                        break;
                    case ProfileAction.EditPackages:
                        EditProfileOp.EditPackages(cmd.Profile!, cmd.Packages);
                        break;
                    case ProfileAction.EditDotfiles:
                        EditProfileOp.EditDotfiles(cmd.Profile!, cmd.Dotfiles);
                        break;
                    case ProfileAction.Apply:
                        ApplyProfileOp.ApplyProfile(cmd.Profile!, startStep: cmd.StartStep);
                        break;
                    case ProfileAction.Export:
                        ExportProfileOp.Export(cmd.Profile!);
                        break;
                }
                break;

            case CommandType.SetDir:
                Env.SetDotfilesDir(cmd.DotfilesDir!);
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
            "Exportar perfil",
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
                case 3: ExportProfileOp.Run(_summary); break;
            }
            Printer.color = Printer.defaultColor;
        }
    }
}