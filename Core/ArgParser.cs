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
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta el nombre del perfil después de --profile.");
                            cmd.Profile = args[i];
                            break;
                        case "--home":
                        case "-H":
                            i++;
                            var homePkgs = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                homePkgs.Add(args[i++]);
                            if (homePkgs.Count == 0)
                                return Error("Faltan los nombres de paquetes después de --home.");
                            cmd.Packages = [.. homePkgs];
                            continue;
                        case "--system":
                        case "-s":
                            i++;
                            var sysPaths = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                sysPaths.Add(args[i++]);
                            if (sysPaths.Count == 0)
                                return Error("Faltan las rutas de sistema después de --system.");
                            cmd.SystemPaths = [.. sysPaths];
                            continue;
                        default:
                            return Error($"Opción desconocida para apply: '{args[i]}'.");
                    }
                    i++;
                }

                if (cmd.Profile is null && cmd.Packages.Length == 0 && cmd.SystemPaths.Length == 0)
                    return Error("apply requiere --profile, --home o --system.");
                break;

            // ══════════════════════════════════════════════════════════════
            // ADD
            // ══════════════════════════════════════════════════════════════
            case "add":
                cmd.Type = CommandType.Add;
                bool hasTarget = false;

                while (i < args.Length)
                {
                    switch (args[i])
                    {
                        case "--home":
                        case "-H":
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta la ruta del archivo/carpeta después de --home.");
                            cmd.AddHomePath = args[i];
                            cmd.AddToSystem = false;
                            hasTarget = true;
                            break;
                        case "--system":
                        case "-s":
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta la ruta del archivo/carpeta después de --system.");
                            cmd.SystemPaths = [args[i]];
                            cmd.AddToSystem = true;
                            hasTarget = true;
                            break;
                        case "--package":
                        case "-p":
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta el nombre del paquete después de --package.");
                            cmd.AddHomePackage = args[i];
                            break;
                        default:
                            return Error($"Opción desconocida para add: '{args[i]}'.");
                    }
                    i++;
                }

                if (!hasTarget)
                    return Error("add requiere --home <ruta> o --system <ruta>.");

                if (cmd.AddHomePath is not null && cmd.AddHomePackage is null)
                    return Error("add con --home requiere --package <nombre>.");
                break;

            // ══════════════════════════════════════════════════════════════
            // DELETE
            // ══════════════════════════════════════════════════════════════
            case "delete":
            case "d":
            case "del":
                cmd.Type = CommandType.Delete;
                bool deleteTargetSet = false;

                while (i < args.Length)
                {
                    switch (args[i])
                    {
                        case "--home":
                        case "-H":
                            cmd.DeleteSystem = false;
                            deleteTargetSet = true;
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta el nombre del paquete después de --home.");
                            cmd.Packages = [args[i]];
                            break;
                        case "--system":
                        case "-s":
                            cmd.DeleteSystem = true;
                            deleteTargetSet = true;
                            i++;
                            var sysPaths = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                sysPaths.Add(args[i++]);
                            if (sysPaths.Count == 0)
                                return Error("Faltan las rutas de sistema después de --system.");
                            cmd.SystemPaths = [.. sysPaths];
                            continue;
                        case "--action":
                        case "-A":
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta la acción después de --action.");
                            string action = args[i].ToLower();
                            if (action is not "symlinks" and not "restore" and not "all")
                                return Error($"Acción inválida: '{action}'. Usar: symlinks, restore, all.");
                            cmd.Action = action;
                            break;
                        default:
                            return Error($"Opción desconocida para delete: '{args[i]}'.");
                    }
                    i++;
                }

                if (!deleteTargetSet)
                    return Error("delete requiere --home o --system.");

                if (cmd.Action is null)
                    return Error("delete requiere --action (symlinks, restore, all).");
                break;

            // ══════════════════════════════════════════════════════════════
            // PROFILE
            // ══════════════════════════════════════════════════════════════
            case "profile":
            case "p":
                cmd.Type = CommandType.Profile;
                if (i >= args.Length)
                    return Error("profile requiere un subcomando: create, edit-name, edit-packages, edit-dotfiles, apply.");

                string subCmd = args[i].ToLower();
                i++;

                switch (subCmd)
                {
                    case "create":
                    case "c":
                        cmd.ProfileAction = ProfileAction.Create;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil para create.");
                        cmd.Profile = args[i];
                        i++;

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
                                    return Error($"Opción desconocida para profile create: '{args[i]}'.");
                            }
                            i++;
                        }
                        break;

                    case "edit-name":
                    case "en":
                        cmd.ProfileAction = ProfileAction.EditName;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre actual del perfil.");
                        cmd.Profile = args[i];
                        i++;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nuevo nombre del perfil.");
                        cmd.NewName = args[i];
                        break;

                    case "edit-packages":
                    case "ep":
                        cmd.ProfileAction = ProfileAction.EditPackages;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil.");
                        cmd.Profile = args[i];
                        i++;
                        if (i >= args.Length && (args[i] != "--packages" && args[i] != "-P"))
                            return Error("Falta --packages para edit-packages.");
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
                        if (cmd.Packages.Length == 0)
                            return Error("edit-packages requiere --packages con al menos un paquete.");
                        break;

                    case "edit-dotfiles":
                    case "ed":
                        cmd.ProfileAction = ProfileAction.EditDotfiles;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil.");
                        cmd.Profile = args[i];
                        i++;
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
                        if (cmd.Dotfiles.Length == 0)
                            return Error("edit-dotfiles requiere --dotfiles con al menos un dotfile.");
                        break;

                    case "apply":
                    case "a":
                        cmd.ProfileAction = ProfileAction.Apply;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil para apply.");
                        cmd.Profile = args[i];
                        break;

                    default:
                        return Error($"Subcomando de perfil desconocido: '{subCmd}'.");
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
                if (i >= args.Length || args[i].StartsWith('-'))
                    return Error("Falta el nombre del script.");
                cmd.ScriptName = args[i];
                break;

            // ══════════════════════════════════════════════════════════════
            // SET-DIR
            // ══════════════════════════════════════════════════════════════
            case "set-dir":
            case "sd":
                cmd.Type = CommandType.SetDir;
                if (i >= args.Length || args[i].StartsWith('-'))
                    return Error("Falta la ruta del directorio.");
                cmd.DotfilesDir = args[i];
                break;

            // ══════════════════════════════════════════════════════════════
            // COMANDO DESCONOCIDO
            // ══════════════════════════════════════════════════════════════
            default:
                Printer.Error($"Comando desconocido: '{mainCmd}'");
                Console.WriteLine();
                ShowHelp();
                return new CliCommand { Type = CommandType.Error };
        }

        return cmd;
    }

    private static CliCommand Error(string message)
    {
        Printer.Error(message);
        Console.WriteLine();
        ShowHelp();
        return new CliCommand { Type = CommandType.Error };
    }

    // ── Help (sin cambios) ──────────────────────────────────────────────────
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
        Aplicar un perfil

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
    None, Menu, Help, Apply, Add, Delete, Status, Script, Profile, SetDir, Error
}

internal enum ProfileAction
{
    None, Create, EditName, EditPackages, EditDotfiles, Apply
}