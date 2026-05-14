namespace DotfilesManager.Core;

internal enum LinkStatus { Ok, Broken, Conflict, NotApplied, Empty }

// Resultado del chequeo de un paquete stow.
// record es como una clase pero inmutable y con igualdad por valor automática.
internal sealed record PackageStatus(
    string Package,
    int    Ok,
    int    Broken,
    int    Conflict,
    int    NotApplied
)
{
    // El estado general del paquete se determina por prioridad:
    // cualquier roto o conflicto es problema, parcial si hay mezcla, etc.
    public LinkStatus Overall =>
        (Broken > 0 || Conflict > 0) ? LinkStatus.Broken     :
        (NotApplied > 0 && Ok > 0)   ? LinkStatus.Conflict   :
        NotApplied > 0               ? LinkStatus.NotApplied  :
        Ok > 0                       ? LinkStatus.Ok          :
                                       LinkStatus.Empty;
}

internal static class StatusChecker
{
    public static PackageStatus Check(string package)
    {
        string pkgDir = Path.Combine(Env.DotfilesDir, package);
        if (!Directory.Exists(pkgDir))
            return new PackageStatus(package, 0, 0, 0, 0);

        int ok = 0, broken = 0, conflict = 0, notApplied = 0;

        // Parallel.ForEach procesa múltiples archivos al mismo tiempo usando varios threads.
        // Para paquetes con miles de archivos esto reduce el tiempo considerablemente.
        // Interlocked.Increment es necesario para incrementar los contadores de forma segura
        // cuando varios threads los modifican al mismo tiempo.
        Parallel.ForEach(
            Directory.EnumerateFiles(pkgDir, "*", SearchOption.AllDirectories),
            src =>
            {
                string rel  = Path.GetRelativePath(pkgDir, src);
                string dest = Path.Combine(Env.HomeDir, rel);

                // Stow puede enlazar carpetas enteras en vez de archivos individuales,
                // así que revisamos cada componente del path buscando un symlink
                // Ej: si .config/hypr es un symlink, todos los archivos adentro están ok
                bool   symlinkFound = false;
                string check        = Env.HomeDir;

                foreach (string component in rel.Split(Path.DirectorySeparatorChar))
                {
                    check = Path.Combine(check, component);
                    if (!IsSymlink(check)) continue;

                    symlinkFound = true;
                    // El symlink existe pero puede apuntar a algo que ya no existe (roto)
                    if (File.Exists(check) || Directory.Exists(check))
                        Interlocked.Increment(ref ok);
                    else
                        Interlocked.Increment(ref broken);
                    break;
                }

                if (!symlinkFound)
                {
                    if (File.Exists(dest) || Directory.Exists(dest))
                        Interlocked.Increment(ref conflict);
                    else
                        Interlocked.Increment(ref notApplied);
                }
            });

        return new PackageStatus(package, ok, broken, conflict, notApplied);
    }

    private static bool IsSymlink(string path)
    {
        try   { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch { return false; }
    }
}
