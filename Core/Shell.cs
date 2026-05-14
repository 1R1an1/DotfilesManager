using System.Diagnostics;

namespace DotfilesManager.Core;

internal static class Shell
{
    // Ejecuta un comando sin mostrar output en pantalla.
    // Úsalo para operaciones silenciosas donde solo te importa si funcionó o no.
    // Retorna el código de salida (0 = éxito) y el stderr.
    public static (int ExitCode, string Stderr) Run(string command, string args)
    {
        var psi = new ProcessStartInfo(command, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar: {command}");

        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stderr);
    }

    // Ejecuta un comando desde un directorio específico.
    // Útil cuando el comando necesita usar rutas relativas.
    public static (int ExitCode, string Stderr) Run(string workingDir, string command, string args)
    {
        var psi = new ProcessStartInfo(command, args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar: {command}");

        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stderr);
    }

    // Ejecuta un comando mostrando el output en pantalla en tiempo real.
    // Úsalo para comandos largos o interactivos donde el usuario necesita ver el progreso.
    public static int RunVisible(string command, string args)
    {
        var psi = new ProcessStartInfo(command, args)
        {
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        return proc.ExitCode;
    }

    // Ejecuta un script de bash mostrando el output en pantalla.
    // bash <ruta> ejecuta el archivo directamente sin necesitar -c
    public static int Bash(string scriptPath)
    {
        string? sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrEmpty(sudoUser))
            return RunVisible("runuser", $"-u {sudoUser} -- /bin/bash \"{scriptPath}\"");

        return RunVisible("/bin/bash", $"\"{scriptPath}\"");
    }

    // Aplica un paquete stow creando symlinks en homeDir.
    // adopt: si un archivo real choca con el symlink que stow quiere crear,
    // lo mueve al repo y crea el symlink en su lugar.
    public static bool Stow(string dotfilesDir, string homeDir, string package, bool adopt = false)
    {
        string adoptFlag = adopt ? "--adopt" : "";
        var (code, _) = Run("stow", $"--no-folding -d \"{dotfilesDir}\" -t \"{homeDir}\" {adoptFlag} \"{package}\"");
        return code == 0;
    }

    // Elimina los symlinks de un paquete stow sin tocar los archivos del repo.
    public static bool StowDelete(string dotfilesDir, string homeDir, string package)
    {
        var (code, _) = Run("stow", $"--no-folding -d \"{dotfilesDir}\" -t \"{homeDir}\" -D \"{package}\"");
        return code == 0;
    }

    // Crea un symlink en dest apuntando a source, con sudo.
    // Si en dest ya existe algo que no es symlink (archivo o carpeta real),
    // lo borra primero porque ln -sf no puede reemplazar carpetas reales.
    public static bool SudoSymlink(string source, string dest)
    {
        Run("sudo", $"mkdir -p \"{Path.GetDirectoryName(dest) ?? "/"}\"");

        bool exists = File.Exists(dest) || Directory.Exists(dest);
        bool isSymlink = exists && new FileInfo(dest).Attributes.HasFlag(FileAttributes.ReparsePoint);
        if (exists && !isSymlink)
            Run("sudo", $"rm -rf \"{dest}\"");

        var (code, _) = Run("sudo", $"ln -sf \"{source}\" \"{dest}\"");
        return code == 0;
    }

    // Mueve un archivo o carpeta con sudo.
    // Borra el destino antes si ya existe, porque mv falla con carpetas no vacías.
    public static bool SudoMove(string source, string dest)
    {
        Run("sudo", $"mkdir -p \"{Path.GetDirectoryName(dest) ?? "/"}\"");

        if (File.Exists(dest) || Directory.Exists(dest))
            Run("sudo", $"rm -rf \"{dest}\"");

        var (code, stderr) = Run("sudo", $"mv \"{source}\" \"{dest}\"");
        if (code != 0) Console.WriteLine($"mv error: {stderr}");
        return code == 0;
    }

    // Elimina un archivo, symlink o carpeta con sudo. -rf para no fallar con carpetas.
    public static bool SudoRemove(string path)
    {
        var (code, _) = Run("sudo", $"rm -rf \"{path}\"");
        return code == 0;
    }

    // Copia un archivo con sudo preservando permisos y timestamps originales.
    public static bool SudoCopy(string source, string dest)
    {
        Run("sudo", $"mkdir -p \"{Path.GetDirectoryName(dest) ?? "/"}\"");
        var (code, _) = Run("sudo", $"cp --preserve=all \"{source}\" \"{dest}\"");
        return code == 0;
    }

    //Copia un archivo preservando todos sus atributos
    public static bool Copy(string source, string dest)
        => Run("cp", $"-f --preserve=all \"{source}\" \"{dest}\"").ExitCode == 0;

    //Copia una carpeta preservando todos sus atributos
    public static bool CopyDir(string source, string dest)
    {
        Run("mkdir", $"-p \"{dest}\"");
        var (code, _) = Run("cp", $"-a \"{source}\"/. \"{dest}\"");
        return code == 0;
    }

    // Copia una carpeta completa con sudo. -a equivale a -r --preserve=all.
    public static bool SudoCopyDir(string source, string dest)
    {
        Run("sudo", $"mkdir -p \"{Path.GetDirectoryName(dest) ?? "/"}\"");
        var (code, _) = Run("sudo", $"cp -a \"{source}\" \"{dest}\"");
        return code == 0;
    }
}
