namespace DotfilesManager.Core;

internal static class ArgParser
{
    public static CliCommand Parse(string[] args)
    {
        if (args.Length == 0)
            return new CliCommand { Type = CommandType.Menu };

        var cmd = new CliCommand();
        int i = 0;

        while (i < args.Length)
        {
            switch (args[i])
            {
                case "-a":
                case "--apply":
                    cmd.Type = CommandType.Apply;
                    i++;
                    if (i < args.Length)
                    {
                        if (args[i] == "--profile" || args[i] == "-p")
                        {
                            i++;
                            if (i < args.Length) cmd.Profile = args[i];
                        }
                        else if (args[i] == "--system" || args[i] == "-s")
                        {
                            cmd.ApplySystem = true;
                            cmd.SystemPaths = args.Skip(i + 1).ToArray();
                            i = args.Length;
                        }
                        else if (args[i] == "--home" || args[i] == "-H")
                        {
                            cmd.Packages = args.Skip(i + 1).ToArray();
                            i = args.Length;
                        }
                    }
                    break;

                case "--add":
                    cmd.Type = CommandType.Add;
                    i++;
                    if (i < args.Length && (args[i] == "--system" || args[i] == "-s"))
                    {
                        cmd.AddToSystem = true;
                        cmd.SystemPaths = args.Skip(i + 1).ToArray();
                        i = args.Length;
                    }
                    else if (i < args.Length)
                    {
                        cmd.AddHomePath = args[i];
                        i++;
                        if (i < args.Length && (args[i] == "--package" || args[i] == "-p"))
                        {
                            i++;
                            if (i < args.Length) cmd.AddHomePackage = args[i];
                        }
                    }
                    break;

                case "-d":
                case "--delete":
                    cmd.Type = CommandType.Delete;
                    i++;
                    if (i < args.Length)
                    {
                        if (args[i] == "--package" || args[i] == "-p")
                        {
                            i++;
                            if (i < args.Length) cmd.Packages = [args[i]];
                            i++;
                            if (i < args.Length && (args[i] == "--action" || args[i] == "-A"))
                            {
                                i++;
                                if (i < args.Length) cmd.Action = args[i];
                            }
                        }
                        else if (args[i] == "--system" || args[i] == "-s")
                        {
                            cmd.DeleteSystem = true;
                            cmd.SystemPaths = args.Skip(i + 1).ToArray();
                            i = args.Length;
                        }
                    }
                    break;

                case "--status":
                    cmd.Type = CommandType.Status;
                    break;

                case "--script":
                case "-S":
                    cmd.Type = CommandType.Script;
                    i++;
                    if (i < args.Length) cmd.ScriptName = args[i];
                    break;

                case "--profile":
                case "-p":
                    if (cmd.Type == CommandType.None)
                        cmd.Type = CommandType.Profile;
                    i++;
                    if (i < args.Length) cmd.Profile = args[i];
                    break;

                case "--create-profile":
                    cmd.Type = CommandType.CreateProfile;
                    i++;
                    if (i < args.Length) cmd.Profile = args[i];
                    i++;
                    if (i < args.Length && (args[i] == "--packages" || args[i] == "-P"))
                    {
                        i++;
                        var pkgs = new List<string>();
                        while (i < args.Length && !args[i].StartsWith('-'))
                            pkgs.Add(args[i++]);
                        cmd.Packages = [.. pkgs];
                    }
                    if (i < args.Length && (args[i] == "--dotfiles" || args[i] == "-D"))
                    {
                        i++;
                        var dots = new List<string>();
                        while (i < args.Length && !args[i].StartsWith('-'))
                            dots.Add(args[i++]);
                        cmd.Dotfiles = [.. dots];
                    }
                    break;

                case "--set-dir":
                    cmd.Type = CommandType.SetDir;
                    i++;
                    if (i < args.Length) cmd.DotfilesDir = args[i];
                    break;

                default:
                    i++;
                    break;
            }
        }

        return cmd;
    }
}

internal class CliCommand
{
    public CommandType Type { get; set; } = CommandType.None;
    public string? Profile { get; set; }
    public string[] Packages { get; set; } = [];
    public string[] SystemPaths { get; set; } = [];
    public string? Action { get; set; }
    public string? AddHomePath { get; set; }
    public string? AddHomePackage { get; set; }
    public bool AddToSystem { get; set; }
    public bool ApplySystem { get; set; }
    public bool DeleteSystem { get; set; }
    public string? ScriptName { get; set; }
    public string[] Dotfiles { get; set; } = [];
    public string? DotfilesDir { get; set; }
}

internal enum CommandType
{
    None,
    Menu,
    Apply,
    Add,
    Delete,
    Status,
    Script,
    Profile,
    CreateProfile,
    SetDir
}