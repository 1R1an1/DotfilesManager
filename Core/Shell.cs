using System.Diagnostics;

namespace DotfilesManager.Core;

internal static class Shell
{
    /// <summary>
    /// Ejecuta un comando. Método ÚNICO para todo.
    /// </summary>
    /// <param name="command">El binario a ejecutar (ej: "cp", "ln", "stow")</param>
    /// <param name="args">Los argumentos del comando</param>
    /// <param name="workingDir">Directorio de trabajo (null = actual)</param>
    /// <param name="asSudo">Si true, antepone "sudo" al comando</param>
    /// <param name="asUser">Si true, ejecuta como el usuario real (útil cuando la app corre con sudo)</param>
    /// <param name="visible">Si true, muestra el output en pantalla en tiempo real</param>
    /// <param name="timeout">Tiempo máximo en segundos (0 = sin límite)</param>
    /// <returns>Tupla con ExitCode, Stdout, Stderr y TimedOut (true si se mató por timeout)</returns>
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

    /// <summary>
    /// Copia un archivo o carpeta, con o sin sudo.
    /// </summary>
    /// <param name="source">Ruta de origen</param>
    /// <param name="dest">Ruta de destino</param>
    /// <param name="asSudo">Si true, usa sudo</param>
    /// <param name="recursive">Si true, copia recursivamente (para carpetas)</param>
    /// <param name="contents">Si true, copia el contenido de source dentro de dest (no source en sí)</param>
    /// <param name="useParents">
    /// Si true, usa cp --parents para recrear la estructura de directorios desde workingDir.
    /// Útil para copiar archivos sueltos manteniendo su ruta relativa.
    /// Ej: Copy(".config/nvim/init.vim", "/backup/", useParents: true, workingDir: "/home/user")
    ///     → copia a /backup/.config/nvim/init.vim
    /// </param>
    /// <param name="workingDir">
    /// Directorio base cuando useParents es true. Las rutas en source se interpretan relativas a este directorio.
    /// Si es null, se usa el directorio actual.
    /// </param>
    /// <returns>Tupla con Ok (éxito), Stdout y Stderr</returns>
    public static (bool Ok, string Stdout, string Stderr) Copy(
        string source,
        string dest,
        bool asSudo = false,
        bool recursive = false,
        bool contents = false,
        bool useParents = false,
        string? workingDir = null)
    {
        if (useParents)
        {
            // Con --parents, cp necesita las rutas relativas desde workingDir
            string flags = "-a --parents";
            var (code, stdout, stderr, _) = Run("cp", $"{flags} {source} \"{dest}\"",
                workingDir: workingDir, asSudo: asSudo);
            return (code == 0, stdout, stderr);
        }

        // Modo normal (sin --parents)
        string? destDir = Path.GetDirectoryName(dest);
        if (destDir is not null)
            Run("mkdir", $"-p \"{destDir}\"", asSudo: asSudo);

        string normalFlags = "--preserve=all";
        if (recursive || contents)
            normalFlags = "-a";  // -a = -dR --preserve=all

        string srcPath = contents ? $"\"{source}\"/." : $"\"{source}\"";
        var (normalCode, normalStdout, normalStderr, _) = Run("cp", $"{normalFlags} {srcPath} \"{dest}\"", asSudo: asSudo);
        return (normalCode == 0, normalStdout, normalStderr);
    }

    /// <summary>
    /// Mueve un archivo o carpeta, con o sin sudo.
    /// </summary>
    /// <param name="source">Ruta de origen</param>
    /// <param name="dest">Ruta de destino</param>
    /// <param name="asSudo">Si true, usa sudo</param>
    /// <returns>Tupla con Ok (éxito), Stdout y Stderr</returns>
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
            Run("rm", $"-rf \"{dest}\"", asSudo: false);

        var (code, stdout, stderr, _) = Run("mv", $"\"{source}\" \"{dest}\"", asSudo: asSudo);
        return (code == 0, stdout, stderr);
    }

    /// <summary>
    /// Elimina un archivo o carpeta, con o sin sudo. Usa rm -rf.
    /// </summary>
    /// <param name="path">Ruta a eliminar</param>
    /// <param name="asSudo">Si true, usa sudo</param>
    /// <returns>Tupla con Ok (éxito), Stdout y Stderr</returns>
    public static (bool Ok, string Stdout, string Stderr) Remove(
        string path,
        bool asSudo = false)
    {
        var (code, stdout, stderr, _) = Run("rm", $"-rf \"{path}\"", asSudo: asSudo);
        return (code == 0, stdout, stderr);
    }

    /// <summary>
    /// Crea un symlink en dest apuntando a source.
    /// </summary>
    /// <param name="source">Ruta a la que apunta el symlink</param>
    /// <param name="dest">Ruta donde se crea el symlink</param>
    /// <param name="asSudo">Si true, usa sudo</param>
    /// <param name="force">Si true, borra el destino si ya existe y no es symlink</param>
    /// <returns>Tupla con Ok (éxito), Stdout y Stderr</returns>
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

    /// <summary>
    /// Crea symlinks individuales para cada archivo dentro de sourceDir en targetDir,
    /// recreando la estructura de subdirectorios. No symlinkea la carpeta en sí.
    /// </summary>
    /// <param name="sourceDir">Carpeta origen (ej: repo/system/boot/grub/themes)</param>
    /// <param name="targetDir">Carpeta destino donde se crearán los symlinks (ej: /boot/grub/themes)</param>
    /// <param name="asSudo">Si true, usa sudo para crear los symlinks</param>
    /// <returns>Lista de rutas de destino donde se crearon symlinks</returns>
    public static List<string> SymlinkDirectoryContents(string sourceDir, string targetDir, bool asSudo = false)
    {
        List<string> created = new();

        if (!Directory.Exists(sourceDir))
            return created;

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(sourceDir, file);
            string dest = Path.Combine(targetDir, rel);

            // Asegurar que existe el directorio contenedor
            string? destDir = Path.GetDirectoryName(dest);
            if (destDir is not null)
                Run("mkdir", $"-p \"{destDir}\"", asSudo: asSudo);

            // Crear el symlink del archivo
            var (ok, _, _) = Symlink(file, dest, asSudo: asSudo);
            if (ok)
                created.Add(dest);
        }

        return created;
    }

    /// <summary>
    /// Aplica o elimina un paquete stow.
    /// </summary>
    /// <param name="dotfilesDir">Directorio del repo de dotfiles</param>
    /// <param name="targetDir">Directorio donde se crean/eliminan los symlinks</param>
    /// <param name="package">Nombre del paquete</param>
    /// <param name="delete">Si true, elimina symlinks (-D) en vez de crearlos</param>
    /// <param name="adopt">Si true, adopta archivos existentes al repo (--adopt)</param>
    /// <returns>Tupla con Ok (éxito), Stdout y Stderr</returns>
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

    /// <summary>
    /// Ejecuta un script .sh con bash.
    /// </summary>
    /// <param name="scriptPath">Ruta absoluta al script</param>
    /// <param name="visible">Si true, muestra el output en tiempo real</param>
    /// <param name="timeout">Tiempo máximo en segundos (0 = sin límite)</param>
    /// <returns>Tupla con ExitCode, Stdout, Stderr y TimedOut</returns>
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