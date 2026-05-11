using System.Diagnostics;

namespace DotfilesManager.Core;

internal static class Shell
{
    /// Ejecuta un comando y retorna (exitCode, stderr).
    public static (int ExitCode, string Stderr) Run(string command, string args)
    {
        var psi = new ProcessStartInfo(command, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar: {command}");

        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stderr);
    }

    /// Ejecuta via /bin/bash -c "…"
    public static (int ExitCode, string Stderr) Bash(string script)
        => Run("/bin/bash", $"-c \"{script.Replace("\"", "\\\"")}\"");

    public static bool Stow(string dotfilesDir, string homeDir, string package, bool adopt = false)
    {
        string adoptFlag = adopt ? "--adopt" : "";
        var (code, _) = Run("stow", $"--no-folding -d \"{dotfilesDir}\" -t \"{homeDir}\" {adoptFlag} \"{package}\"");
        return code == 0;
    }

    public static bool StowDelete(string dotfilesDir, string homeDir, string package)
    {
        var (code, _) = Run("stow", $"-d \"{dotfilesDir}\" -t \"{homeDir}\" -D \"{package}\"");
        return code == 0;
    }

    public static bool SudoSymlink(string source, string dest)
    {
        string dir = Path.GetDirectoryName(dest) ?? "/";
        Run("sudo", $"mkdir -p \"{dir}\"");
        var (code, _) = Run("sudo", $"ln -sf \"{source}\" \"{dest}\"");
        return code == 0;
    }

    public static bool SudoMove(string source, string dest)
    {
        string dir = Path.GetDirectoryName(dest) ?? "/";
        Run("sudo", $"mkdir -p \"{dir}\"");
        var (code, _) = Run("sudo", $"mv \"{source}\" \"{dest}\"");
        if (code != 0) return false;
        string user = Environment.UserName;
        Run("sudo", $"chown {user}:{user} \"{dest}\"");
        return true;
    }

    public static bool SudoRemove(string path)
    {
        var (code, _) = Run("sudo", $"rm \"{path}\"");
        return code == 0;
    }

    public static bool SudoCopy(string source, string dest)
    {
        string dir = Path.GetDirectoryName(dest) ?? "/";
        Run("sudo", $"mkdir -p \"{dir}\"");
        var (code, _) = Run("sudo", $"cp \"{source}\" \"{dest}\"");
        if (code != 0) return false;
        string user = Environment.UserName;
        Run("sudo", $"chown {user}:{user} \"{dest}\"");
        return true;
    }
}
