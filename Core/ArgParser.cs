using DotfilesManager.UI;

namespace DotfilesManager.Core;

internal static class ArgParser
{
    public static CliCommand Parse(string[] args)
    {
        if (args.Length == 0)
            return new CliCommand { Type = CommandType.Menu };

        var cmd = new CliCommand();
        int i = 0;

        // ── Comando principal ────────────────────────────────────────────
        string mainCmd = args[0].ToLower();
        i = 1;

        switch (mainCmd)
        {
            // ══════════════════════════════════════════════════════════════
            // HELP
            // ══════════════════════════════════════════════════════════════
            case "-h":
            case "--help":
            case "help":
                return new CliCommand { Type = CommandType.Help };

            // ══════════════════════════════════════════════════════════════
            // APPLY
            // ══════════════════════════════════════════════════════════════
            case "apply":
            case "a":
                cmd.Type = CommandType.Apply;
                while (i < args.Length)
                {
                    switch (args[i])
                    {
                        case "--profile":
                        case "-p":
                            i++;
                            if (i < args.Length) cmd.Profile = args[i];
                            break;
                        case "--home":
                        case "-H":
                            i++;
                            var homePkgs = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                homePkgs.Add(args[i++]);
                            cmd.Packages = [.. homePkgs];
                            continue;
                        case "--system":
                        case "-s":
                            i++;
                            var sysPaths = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                sysPaths.Add(args[i++]);
                            cmd.SystemPaths = [.. sysPaths];
                            continue;
                        default:
                            i++;
                            break;
                    }
                    i++;
                }
                break;

            // ══════════════════════════════════════════════════════════════
            // ADD
            // ══════════════════════════════════════════════════════════════
            case "add":
                cmd.Type = CommandType.Add;
                while (i < args.Length)
                {
                    switch (args[i])
                    {
                        case "--home":
                        case "-H":
                            i++;
                            if (i < args.Length)
                            {
                                cmd.AddHomePath = args[i];
                                cmd.AddToSystem = false;
                            }
                            break;
                        case "--system":
                        case "-s":
                            i++;
                            if (i < args.Length)
                            {
                                cmd.SystemPaths = [args[i]];
                                cmd.AddToSystem = true;
                            }
                            break;
                        case "--package":
                        case "-p":
                            i++;
                            if (i < args.Length) cmd.AddHomePackage = args[i];
                            break;
                        default:
                            i++;
                            break;
                    }
                    i++;
                }
                break;

            // ══════════════════════════════════════════════════════════════
            // DELETE
            // ══════════════════════════════════════════════════════════════
            case "delete":
            case "d":
            case "del":
                cmd.Type = CommandType.Delete;
                while (i < args.Length)
                {
                    switch (args[i])
                    {
                        case "--home":
                        case "-H":
                            cmd.DeleteSystem = false;
                            i++;
                            if (i < args.Length && !args[i].StartsWith('-'))
                                cmd.Packages = [args[i]];
                            break;
                        case "--system":
                        case "-s":
                            cmd.DeleteSystem = true;
                            i++;
                            var sysPaths = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                sysPaths.Add(args[i++]);
                            cmd.SystemPaths = [.. sysPaths];
                            continue;
                        case "--action":
                        case "-A":
                            i++;
                            if (i < args.Length) cmd.Action = args[i];
                            break;
                        default:
                            i++;
                            break;
                    }
                    i++;
                }
                break;

            // ══════════════════════════════════════════════════════════════
            // PROFILE
            // ══════════════════════════════════════════════════════════════
            case "profile":
            case "p":
                cmd.Type = CommandType.Profile;
                if (i < args.Length)
                {
                    string subCmd = args[i].ToLower();
                    i++;

                    switch (subCmd)
                    {
                        case "create":
                        case "c":
                            cmd.ProfileAction = ProfileAction.Create;
                            if (i < args.Length) cmd.Profile = args[i++];
                            while (i < args.Length)
                            {
                                switch (args[i])
                                {
                                    case "--packages":
                                    case "-P":
                                        i++;
                                        var pkgs = new List<string>();
                                        while (i < args.Length && !args[i].StartsWith('-'))
                                            pkgs.Add(args[i++]);
                                        cmd.Packages = [.. pkgs];
                                        continue;
                                    case "--dotfiles":
                                    case "-D":
                                        i++;
                                        var dots = new List<string>();
                                        while (i < args.Length && !args[i].StartsWith('-'))
                                            dots.Add(args[i++]);
                                        cmd.Dotfiles = [.. dots];
                                        continue;
                                    default:
                                        i++;
                                        break;
                                }
                                i++;
                            }
                            break;

                        case "edit-name":
                        case "en":
                            cmd.ProfileAction = ProfileAction.EditName;
                            if (i < args.Length) cmd.Profile = args[i++];
                            if (i < args.Length) cmd.NewName = args[i];
                            break;

                        case "edit-packages":
                        case "ep":
                            cmd.ProfileAction = ProfileAction.EditPackages;
                            if (i < args.Length) cmd.Profile = args[i++];
                            while (i < args.Length)
                            {
                                if (args[i] == "--packages" || args[i] == "-P")
                                {
                                    i++;
                                    var pkgs = new List<string>();
                                    while (i < args.Length && !args[i].StartsWith('-'))
                                        pkgs.Add(args[i++]);
                                    cmd.Packages = [.. pkgs];
                                    continue;
                                }
                                i++;
                            }
                            break;

                        case "edit-dotfiles":
                        case "ed":
                            cmd.ProfileAction = ProfileAction.EditDotfiles;
                            if (i < args.Length) cmd.Profile = args[i++];
                            while (i < args.Length)
                            {
                                if (args[i] == "--dotfiles" || args[i] == "-D")
                                {
                                    i++;
                                    var dots = new List<string>();
                                    while (i < args.Length && !args[i].StartsWith('-'))
                                        dots.Add(args[i++]);
                                    cmd.Dotfiles = [.. dots];
                                    continue;
                                }
                                i++;
                            }
                            break;

                        case "apply":
                        case "a":
                            cmd.ProfileAction = ProfileAction.Apply;
                            if (i < args.Length) cmd.Profile = args[i];
                            break;

                        default:
                            // profile <nombre> = apply implícito
                            cmd.ProfileAction = ProfileAction.Apply;
                            cmd.Profile = subCmd;
                            break;
                    }
                }
                break;

            // ══════════════════════════════════════════════════════════════
            // STATUS
            // ══════════════════════════════════════════════════════════════
            case "status":
            case "st":
                cmd.Type = CommandType.Status;
                break;

            // ══════════════════════════════════════════════════════════════
            // SCRIPT
            // ══════════════════════════════════════════════════════════════
            case "script":
            case "S":
            case "run":
                cmd.Type = CommandType.Script;
                if (i < args.Length) cmd.ScriptName = args[i];
                break;

            // ══════════════════════════════════════════════════════════════
            // SET-DIR
            // ══════════════════════════════════════════════════════════════
            case "set-dir":
            case "sd":
                cmd.Type = CommandType.SetDir;
                if (i < args.Length) cmd.DotfilesDir = args[i];
                break;

            // ══════════════════════════════════════════════════════════════
            // DEFAULT: si no matchea, mostrar error
            // ══════════════════════════════════════════════════════════════
            default:
                Printer.Error($"Comando desconocido: '{mainCmd}', utilize -h para obtener ayuda");
                Console.WriteLine();
                break;
        }

        return cmd;
    }

    // ── Help ──────────────────────────────────────────────────────────────

    public static void ShowHelp()
    {
        Console.WriteLine(@"
Dotfiles Manager — CLI

Uso: dotfiles-manager <comando> [opciones]

╔══════════════════════════════════════════════════════════════╗
║  APLICAR                                                     ║
╚══════════════════════════════════════════════════════════════╝

  apply, a --profile, -p <nombre>
        Aplicar un perfil

  apply, a --home, -H <paquetes...>
        Aplicar paquetes stow del home

  apply, a --system, -s <rutas...>
        Aplicar symlinks de sistema

╔══════════════════════════════════════════════════════════════╗
║  AGREGAR                                                     ║
╚══════════════════════════════════════════════════════════════╝

  add --home, -H <ruta> --package, -p <paquete>
        Agregar archivo/carpeta del home a un paquete stow

  add --system, -s <ruta>
        Agregar archivo/carpeta al sistema

╔══════════════════════════════════════════════════════════════╗
║  ELIMINAR                                                    ║
╚══════════════════════════════════════════════════════════════╝

  delete, d, del --home, -H <paquete> --action, -A <accion>
        Eliminar symlinks de un paquete stow
        Acciones: symlinks | restore | all

  delete, d, del --system, -s <rutas...> --action, -A <accion>
        Eliminar symlinks de sistema

╔══════════════════════════════════════════════════════════════╗
║  PERFILES                                                    ║
╚══════════════════════════════════════════════════════════════╝

  profile, p create, c <nombre> --packages, -P <...> --dotfiles, -D <...>
        Crear un perfil nuevo

  profile, p edit-name, en <viejo> <nuevo>
        Cambiar nombre de un perfil

  profile, p edit-packages, ep <nombre> --packages, -P <...>
        Editar paquetes de un perfil

  profile, p edit-dotfiles, ed <nombre> --dotfiles, -D <...>
        Editar dotfiles de un perfil

  profile, p apply, a <nombre>
        Aplicar un perfil (atajo: solo el nombre)

╔══════════════════════════════════════════════════════════════╗
║  OTROS                                                       ║
╚══════════════════════════════════════════════════════════════╝

  status, st
        Ver estado de symlinks

  script, S, run <nombre>
        Ejecutar un script del repo

  set-dir, sd <ruta>
        Cambiar el directorio del repo de dotfiles

  -h, --help, help
        Mostrar esta ayuda

╔══════════════════════════════════════════════════════════════╗
║  EJEMPLOS                                                    ║
╚══════════════════════════════════════════════════════════════╝

  dm a -p gaming
  dm apply --system /etc/hosts /etc/mkinitcpio.conf
  dm a -H nvim bash
  dm add -H ~/.config/hypr -p hyprland
  dm add -s /etc/grub/grub.cfg
  dm d -H nvim -A restore
  dm delete -s /etc/hosts -A all
  dm p create servidor -P nginx docker -D bash
  dm profile en viejo nuevo
  dm p apply gaming
  dm status
  dm S mi-script
  dm sd /home/user/mis-dotfiles
");
    }
}

internal class CliCommand
{
    public CommandType Type { get; set; } = CommandType.None;
    public ProfileAction ProfileAction { get; set; } = ProfileAction.None;
    public string? Profile { get; set; }
    public string? NewName { get; set; }
    public string[] Packages { get; set; } = [];
    public string[] SystemPaths { get; set; } = [];
    public string? Action { get; set; }
    public string? AddHomePath { get; set; }
    public string? AddHomePackage { get; set; }
    public bool AddToSystem { get; set; }
    public bool DeleteSystem { get; set; }
    public string? ScriptName { get; set; }
    public string[] Dotfiles { get; set; } = [];
    public string? DotfilesDir { get; set; }
}

internal enum CommandType
{
    None, Menu, Help, Apply, Add, Delete, Status, Script, Profile, SetDir
}

internal enum ProfileAction
{
    None, Create, EditName, EditPackages, EditDotfiles, Apply
}