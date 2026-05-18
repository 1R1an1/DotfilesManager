internal class CliCommand
{
    public CommandType Type { get; set; } = CommandType.None;
    public ProfileAction ProfileAction { get; set; } = ProfileAction.None;
    public int StartStep { get; set; } = 0; // 0 = desde el principio, base 1 en CLI
    public BackupTarget BackupTarget { get; set; } = BackupTarget.None;
    public string[] ScriptArgs { get; set; } = [];
    public bool BackupAll { get; set; } = false;
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

internal enum BackupTarget { None, Packages, System }

internal enum CommandType
{
    None, Menu, Help, Apply, Add, Backup, Delete, Status, Script, Profile, SetDir, Error
}

internal enum ProfileAction
{
    None, Create, EditName, EditPackages, EditDotfiles, Apply, Export
}