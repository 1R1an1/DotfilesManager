using System.Diagnostics;

namespace DotfilesManager.Core;

internal static class Shell
{
    // ═══════════════════════════════════════════════════════════════════════
    // Método ÚNICO de ejecución — hace todo
    // ═══════════════════════════════════════════════════════════════════════
    //
    // Parámetros:
    //   command    : el binario (ej: "cp", "ln", "stow")
    //   args       : los argumentos
    //   workingDir : directorio de trabajo (null = actual)
    //   asSudo     : antepone "sudo" al comando
    //   asUser     : ejecuta como el usuario real (útil cuando la app corre con sudo)
    //   visible    : muestra output en pantalla en tiempo real
    //   timeout    : tiempo máximo en segundos (0 = sin límite)
    //
    // Retorna:
    //   ExitCode   : 0 = éxito
    //   Stdout     : salida estándar completa
    //   Stderr     : salida de error completa
    //   TimedOut   : true si se mató por timeout
    // ═══════════════════════════════════════════════════════════════════════
    public static (int ExitCode, string Stdout, string Stderr, bool TimedOut) Run(
        string command,
        string args,
        string? workingDir = null,
        bool asSudo = false,
        bool asUser = false,
        bool visible = false,
        int timeout = 0)
    {
        // Construir el comando final según las flags
        string finalCommand = command;
        string finalArgs = args;

        if (asSudo)
        {
            finalArgs = $"{command} {args}";
            finalCommand = "sudo";
        }

        if (asUser)
        {
            string? sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
            if (!string.IsNullOrEmpty(sudoUser))
            {
                finalArgs = $"-u {sudoUser} -- {finalCommand} {finalArgs}";
                finalCommand = "runuser";
            }
        }

        var psi = new ProcessStartInfo(finalCommand, finalArgs)
        {
            UseShellExecute = false,
            RedirectStandardOutput = !visible,
            RedirectStandardError = !visible,
        };

        if (workingDir is not null)
            psi.WorkingDirectory = workingDir;

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar: {finalCommand}");

        string stdout = "", stderr = "";
        bool timedOut = false;

        if (!visible)
        {
            // Modo silencioso: leer todo al final
            stdout = proc.StandardOutput.ReadToEnd();
            stderr = proc.StandardError.ReadToEnd();
        }

        if (timeout > 0)
        {
            timedOut = !proc.WaitForExit(timeout * 1000);
            if (timedOut) proc.Kill();
        }
        else
        {
            proc.WaitForExit();
        }

        return (proc.ExitCode, stdout, stderr, timedOut);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Copia — archivo o carpeta, con o sin sudo
    // ═══════════════════════════════════════════════════════════════════════
    //
    // source    : origen
    // dest      : destino
    // asSudo    : usar sudo
    // recursive : si es carpeta, copiar contenido recursivamente
    // contents  : si true, copia el contenido de source dentro de dest (no source en sí)
    //
    // Retorna: (éxito, stdout, stderr)
    // ═══════════════════════════════════════════════════════════════════════
    public static (bool Ok, string Stdout, string Stderr) Copy(
        string source,
        string dest,
        bool asSudo = false,
        bool recursive = false,
        bool contents = false)
    {
        // Asegurar que el directorio padre del destino existe
        string? destDir = Path.GetDirectoryName(dest);
        if (destDir is not null)
            Run("mkdir", $"-p \"{destDir}\"", asSudo: asSudo);

        string flags = "--preserve=all";
        if (recursive || contents)
            flags = "-a";  // -a = -dR --preserve=all

        string srcPath = contents ? $"\"{source}\"/." : $"\"{source}\"";
        var (code, stdout, stderr, _) = Run("cp", $"{flags} {srcPath} \"{dest}\"", asSudo: asSudo);
        return (code == 0, stdout, stderr);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Move — archivo o carpeta, con o sin sudo
    // ═══════════════════════════════════════════════════════════════════════
    public static (bool Ok, string Stdout, string Stderr) Move(
        string source,
        string dest,
        bool asSudo = false)
    {
        // Asegurar que el directorio padre del destino existe
        string? destDir = Path.GetDirectoryName(dest);
        if (destDir is not null)
            Run("mkdir", $"-p \"{destDir}\"", asSudo: asSudo);

        // Si el destino existe, borrarlo antes (mv no sobrescribe carpetas)
        if (File.Exists(dest) || Directory.Exists(dest))
            Run("rm", $"-rf \"{dest}\"", asSudo: asSudo);

        var (code, stdout, stderr, _) = Run("mv", $"\"{source}\" \"{dest}\"", asSudo: asSudo);
        return (code == 0, stdout, stderr);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Remove — archivo o carpeta, con o sin sudo
    // ═══════════════════════════════════════════════════════════════════════
    public static (bool Ok, string Stdout, string Stderr) Remove(
        string path,
        bool asSudo = false)
    {
        var (code, stdout, stderr, _) = Run("rm", $"-rf \"{path}\"", asSudo: asSudo);
        return (code == 0, stdout, stderr);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Symlink — crea un symlink en dest apuntando a source
    // ═══════════════════════════════════════════════════════════════════════
    //
    // force: si true, borra el destino si ya existe (archivo o carpeta real)
    // ═══════════════════════════════════════════════════════════════════════
    public static (bool Ok, string Stdout, string Stderr) Symlink(
        string source,
        string dest,
        bool asSudo = false,
        bool force = true)
    {
        // Crear directorio padre del symlink
        string? destDir = Path.GetDirectoryName(dest);
        if (destDir is not null)
            Run("mkdir", $"-p \"{destDir}\"", asSudo: asSudo);

        // Si force y existe algo que no es symlink, borrarlo
        if (force)
        {
            bool exists = File.Exists(dest) || Directory.Exists(dest);
            bool isSymlink = exists && new FileInfo(dest).Attributes.HasFlag(FileAttributes.ReparsePoint);
            if (exists && !isSymlink)
                Run("rm", $"-rf \"{dest}\"", asSudo: asSudo);
        }

        var (code, stdout, stderr, _) = Run("ln", $"-sf \"{source}\" \"{dest}\"", asSudo: asSudo);
        return (code == 0, stdout, stderr);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Stow — aplica un paquete stow
    // ═══════════════════════════════════════════════════════════════════════
    //
    // delete: si true, elimina symlinks en vez de crearlos (-D)
    // adopt : si true, adopta archivos existentes al repo (--adopt)
    // ═══════════════════════════════════════════════════════════════════════
    public static (bool Ok, string Stdout, string Stderr) Stow(
        string dotfilesDir,
        string targetDir,
        string package,
        bool delete = false,
        bool adopt = false)
    {
        string action = delete ? "-D" : "";
        string adoptFlag = adopt ? "--adopt" : "";
        var (code, stdout, stderr, _) = Run("stow",
            $"--no-folding -d \"{dotfilesDir}\" -t \"{targetDir}\" {action} {adoptFlag} \"{package}\"");
        return (code == 0, stdout, stderr);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Bash — ejecuta un script .sh
    // ═══════════════════════════════════════════════════════════════════════
    public static (int ExitCode, string Stdout, string Stderr, bool TimedOut) Bash(
        string scriptPath,
        bool visible = true,
        int timeout = 0)
    {
        string? sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrEmpty(sudoUser))
            return Run("runuser", $"-u {sudoUser} -- /bin/bash \"{scriptPath}\"", visible: visible, timeout: timeout);

        return Run("/bin/bash", $"\"{scriptPath}\"", visible: visible, timeout: timeout);
    }
}