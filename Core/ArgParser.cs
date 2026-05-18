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
                        case "--home":
                        case "-H":
                            i++;
                            var homePkgs = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                homePkgs.Add(args[i++]);
                            if (homePkgs.Count == 0)
                                return Error("Faltan los nombres de paquetes después de --home.", mainCmd);
                            cmd.Packages = [.. homePkgs];
                            continue;
                        case "--system":
                        case "-s":
                            i++;
                            var sysPaths = new List<string>();
                            while (i < args.Length && !args[i].StartsWith('-'))
                                sysPaths.Add(args[i++]);
                            if (sysPaths.Count == 0)
                                return Error("Faltan las rutas de sistema después de --system.", mainCmd);
                            cmd.SystemPaths = [.. sysPaths];
                            continue;
                        default:
                            return Error($"Opción desconocida para apply: '{args[i]}'.", mainCmd);
                    }
                }

                if (cmd.Packages.Length == 0 && cmd.SystemPaths.Length == 0)
                    return Error("apply requiere --home o --system.", mainCmd);
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
                                return Error("Falta la ruta del archivo/carpeta después de --home.", mainCmd);
                            cmd.AddHomePath = args[i];
                            cmd.AddToSystem = false;
                            hasTarget = true;
                            break;
                        case "--system":
                        case "-s":
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta la ruta del archivo/carpeta después de --system.", mainCmd);
                            cmd.SystemPaths = [args[i]];
                            cmd.AddToSystem = true;
                            hasTarget = true;
                            break;
                        case "--package":
                        case "-p":
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta el nombre del paquete después de --package.", mainCmd);
                            cmd.AddHomePackage = args[i];
                            break;
                        default:
                            return Error($"Opción desconocida para add: '{args[i]}'.", mainCmd);
                    }
                    i++;
                }

                if (!hasTarget)
                    return Error("add requiere --home <ruta> o --system <ruta>.", mainCmd);

                if (cmd.AddHomePath is not null && cmd.AddHomePackage is null)
                    return Error("add con --home requiere --package <nombre>.", mainCmd);
                break;

            // ══════════════════════════════════════════════════════════════
            // BACKUP
            // ══════════════════════════════════════════════════════════════
            case "backup":
            case "bkp":
                cmd.Type = CommandType.Backup;
                while (i < args.Length)
                {
                    switch (args[i])
                    {
                        case "--package":
                        case "-p":
                            cmd.BackupTarget = BackupTarget.Packages;
                            i++;
                            if (i >= args.Length)
                                return Error("Falta --all o al menos un paquete después de --package.", mainCmd);

                            if (args[i] == "--all" || args[i] == "-a")
                            {
                                cmd.BackupAll = true;
                            }
                            else
                            {
                                var pkgs = new List<string>();
                                while (i < args.Length && !args[i].StartsWith('-'))
                                {
                                    pkgs.Add(args[i].Trim('\'', '"')); // quitar comillas
                                    i++;
                                }
                                if (pkgs.Count == 0)
                                    return Error("Se esperaba al menos un nombre de paquete.", mainCmd);
                                cmd.Packages = pkgs.ToArray();
                                continue; // ya incrementamos i dentro del while
                            }
                            break;

                        case "--system":
                        case "-s":
                            cmd.BackupTarget = BackupTarget.System;
                            i++;
                            if (i >= args.Length)
                                return Error("Falta --all o al menos una ruta después de --system.", mainCmd);

                            if (args[i] == "--all" || args[i] == "-a")
                            {
                                cmd.BackupAll = true;
                            }
                            else
                            {
                                var paths = new List<string>();
                                while (i < args.Length && !args[i].StartsWith('-'))
                                {
                                    paths.Add(args[i].Trim('\'', '"')); // quitar comillas
                                    i++;
                                }
                                if (paths.Count == 0)
                                    return Error("Se esperaba al menos una ruta de sistema.", mainCmd);
                                cmd.SystemPaths = paths.ToArray();
                                continue;
                            }
                            break;

                        default:
                            return Error($"Opción desconocida para backup: '{args[i]}'.", mainCmd);
                    }
                    i++;
                }

                if (cmd.BackupTarget == BackupTarget.None)
                    return Error("backup requiere --package o --system.", mainCmd);

                if (!cmd.BackupAll && cmd.BackupTarget == BackupTarget.Packages && cmd.Packages.Length == 0)
                    return Error("backup --package requiere --all o al menos un nombre de paquete.", mainCmd);

                if (!cmd.BackupAll && cmd.BackupTarget == BackupTarget.System && cmd.SystemPaths.Length == 0)
                    return Error("backup --system requiere --all o al menos una ruta.", mainCmd);
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
                                return Error("Falta el nombre del paquete después de --home.", mainCmd);
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
                                return Error("Faltan las rutas de sistema después de --system.", mainCmd);
                            cmd.SystemPaths = [.. sysPaths];
                            continue;
                        case "--action":
                        case "-A":
                            i++;
                            if (i >= args.Length || args[i].StartsWith('-'))
                                return Error("Falta la acción después de --action.", mainCmd);
                            string action = args[i].ToLower();
                            if (action is not "symlinks" and not "restore" and not "all")
                                return Error($"Acción inválida: '{action}'. Usar: symlinks, restore, all.", mainCmd);
                            cmd.Action = action;
                            break;
                        default:
                            return Error($"Opción desconocida para delete: '{args[i]}'.", mainCmd);
                    }
                    i++;
                }

                if (!deleteTargetSet)
                    return Error("delete requiere --home o --system.", mainCmd);

                if (cmd.Action is null)
                    return Error("delete requiere --action (symlinks, restore, all).", mainCmd);
                break;

            // ══════════════════════════════════════════════════════════════
            // PROFILE
            // ══════════════════════════════════════════════════════════════
            case "profile":
            case "p":
                cmd.Type = CommandType.Profile;
                if (i >= args.Length)
                    return Error("profile requiere un subcomando: create, edit-name, edit-packages, edit-dotfiles, apply.", mainCmd);

                string subCmd = args[i].ToLower();
                i++;

                switch (subCmd)
                {
                    case "create":
                    case "c":
                        cmd.ProfileAction = ProfileAction.Create;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil para create.", mainCmd);
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
                                    return Error($"Opción desconocida para profile create: '{args[i]}'.", mainCmd);
                            }
                        }
                        break;

                    case "edit-name":
                    case "en":
                        cmd.ProfileAction = ProfileAction.EditName;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre actual del perfil.", mainCmd);
                        cmd.Profile = args[i];
                        i++;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nuevo nombre del perfil.", mainCmd);
                        cmd.NewName = args[i];
                        break;

                    case "edit-packages":
                    case "ep":
                        cmd.ProfileAction = ProfileAction.EditPackages;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil.", mainCmd);
                        cmd.Profile = args[i];
                        i++;
                        if (i >= args.Length && (args[i] != "--packages" && args[i] != "-P"))
                            return Error("Falta --packages para edit-packages.", mainCmd);
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
                            return Error("edit-packages requiere --packages con al menos un paquete.", mainCmd);
                        break;

                    case "edit-dotfiles":
                    case "ed":
                        cmd.ProfileAction = ProfileAction.EditDotfiles;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil.", mainCmd);
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
                            return Error("edit-dotfiles requiere --dotfiles con al menos un dotfile.", mainCmd);
                        break;

                    case "apply":
                    case "a":
                        cmd.ProfileAction = ProfileAction.Apply;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil para apply.", mainCmd);
                        cmd.Profile = args[i];
                        i++;
                        // Buscar --from-step
                        while (i < args.Length)
                        {
                            if (args[i] == "--from-step" || args[i] == "-f")
                            {
                                i++;
                                if (i >= args.Length || !int.TryParse(args[i], out int step) || step < 1)
                                    return Error("Número de paso inválido para --from-step (debe ser ≥ 1).", mainCmd);
                                cmd.StartStep = step;
                            }
                            else return Error($"Opción desconocida: '{args[i]}'", mainCmd);
                            i++;
                        }
                        break;

                    case "export":
                    case "x":
                        cmd.ProfileAction = ProfileAction.Export;
                        if (i >= args.Length || args[i].StartsWith('-'))
                            return Error("Falta el nombre del perfil para export.", mainCmd);
                        cmd.Profile = args[i];
                        break;

                    default:
                        return Error($"Subcomando de perfil desconocido: '{subCmd}'.", mainCmd);
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
                    return Error("Falta el nombre del script.", mainCmd);
                cmd.ScriptName = args[i];
                i++;
                // Todo lo que sigue son argumentos para el script
                if (i < args.Length)
                    cmd.ScriptArgs = args[i..];
                break;

            // ══════════════════════════════════════════════════════════════
            // SET-DIR
            // ══════════════════════════════════════════════════════════════
            case "set-dir":
            case "sd":
                cmd.Type = CommandType.SetDir;
                if (i >= args.Length || args[i].StartsWith('-'))
                    return Error("Falta la ruta del directorio.", mainCmd);
                cmd.DotfilesDir = args[i];
                break;

            // ══════════════════════════════════════════════════════════════
            // COMANDO DESCONOCIDO
            // ══════════════════════════════════════════════════════════════
            default:
                Printer.Error($"Comando desconocido: '{mainCmd}'");
                Console.WriteLine();
                ShowHelp(); // ayuda completa porque no sabemos qué sección
                return new CliCommand { Type = CommandType.Error };
        }

        return cmd;
    }

    private static CliCommand Error(string message, string? command = null)
    {
        Printer.Error(message);
        Console.WriteLine();
        ShowHelp(command);
        return new CliCommand { Type = CommandType.Error };
    }

    // ── Help ────────────────────────────────────────────────────────────────
    public static void ShowHelp(string? command = null)
    {
        if (command is not null)
        {
            ShowHelpSection(command);
            return;
        }

        Console.WriteLine(@"
Dotfiles Manager — CLI

Uso: dotfiles-manager <comando> [opciones]
");

        ShowHelpSection("apply");
        ShowHelpSection("add");
        ShowHelpSection("backup");
        ShowHelpSection("delete");
        ShowHelpSection("profile");
        ShowHelpSection("status");
        ShowHelpSection("script");
        ShowHelpSection("set-dir");

        Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  AYUDA
═══════════════════════════════════════════════════════════════

  Comando: -h, --help, help
      Mostrar esta ayuda
");
    }

    /*
    ═══════════════════════════════════════════════════════════════
      EJEMPLOS
    ═══════════════════════════════════════════════════════════════

      dm a -H nvim bash
      dm apply --system /etc/hosts /etc/mkinitcpio.conf
      dm add -H ~/.config/hypr -p hyprland
      dm add -s /etc/grub/grub.cfg
      dm d -H nvim -A restore
      dm delete -s /etc/hosts -A all
      dm p create servidor -P nginx docker -D bash
      dm profile en viejo nuevo
      dm p apply gaming
      dm p apply gaming -f 2
      dm p export gaming
      dm status
      dm S mi-script
      dm sd /home/user/mis-dotfiles
      */

    private static void ShowHelpSection(string command)
    {
        switch (command)
        {
            case "apply":
            case "a":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  APLICAR
═══════════════════════════════════════════════════════════════

  Comando: apply, a
    -H, --home <paquetes...>      Aplicar paquetes stow del home
    -s, --system <rutas...>       Aplicar symlinks de sistema
");
                break;

            case "add":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  AGREGAR
═══════════════════════════════════════════════════════════════

  Comando: add
    -H, --home <ruta> -p, --package <paquete>
        Agregar archivo/carpeta del home a un paquete stow

    -s, --system <ruta>
        Agregar archivo/carpeta al sistema
");
                break;

            case "backup":
            case "bkp":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  BACKUP
═══════════════════════════════════════════════════════════════

  Comando: backup, bkp
    -p, --package <paquetes...>   Backup de paquetes del repo
    -p, --package --all, -a       Backup de todos los paquetes
    -s, --system <rutas...>       Backup de carpetas y archivos de system/
    -s, --system --all, -a        Backup de todo system/
");
                break;

            case "delete":
            case "d":
            case "del":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  ELIMINAR
═══════════════════════════════════════════════════════════════

  Comando: delete, d, del
    -H, --home <paquete> -A, --action <accion>
        Eliminar symlinks de un paquete stow

    -s, --system <rutas...> -A, --action <accion>
        Eliminar symlinks de sistema

    Acciones: symlinks | restore | all
");
                break;

            case "profile":
            case "p":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  PERFILES
═══════════════════════════════════════════════════════════════

  Comando: profile, p

    Subcomando: create, c <nombre>
      -P, --packages <paquetes...>   Paquetes a incluir
      -D, --dotfiles <dotfiles...>   Dotfiles a incluir

    Subcomando: edit-name, en <viejo> <nuevo>
      Cambiar nombre de un perfil

    Subcomando: edit-packages, ep <nombre>
      -P, --packages <paquetes...>   Nuevos paquetes del perfil

    Subcomando: edit-dotfiles, ed <nombre>
      -D, --dotfiles <dotfiles...>   Nuevos dotfiles del perfil

    Subcomando: apply, a <nombre>
      -f, --from-step <número>       Aplicar desde un paso concreto (opcional)

    Subcomando: export, x <nombre>
      Exportar un perfil a un script .sh
");
                break;

            case "status":
            case "st":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  STATUS
═══════════════════════════════════════════════════════════════

  Comando: status, st
      Ver estado de symlinks
");
                break;

            case "script":
            case "S":
            case "run":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  SCRIPT
═══════════════════════════════════════════════════════════════

  Comando: script, S, run <nombre> [args...]
      Ejecutar un script del repo con argumentos opcionales
");
                break;

            case "set-dir":
            case "sd":
                Console.WriteLine(@"
═══════════════════════════════════════════════════════════════
  SET-DIR
═══════════════════════════════════════════════════════════════

  Comando: set-dir, sd <ruta>
      Cambiar el directorio del repo de dotfiles
");
                break;
        }
    }
}