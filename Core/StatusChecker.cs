namespace DotfilesManager.Core;

internal enum LinkStatus { Ok, Broken, Conflict, NotApplied, Empty }

internal sealed record PackageStatus(
    string      Package,
    int         Ok,
    int         Broken,
    int         Conflict,
    int         NotApplied
)
{
    public LinkStatus Overall =>
        (Broken > 0 || Conflict > 0)        ? LinkStatus.Broken    :
        (NotApplied > 0 && Ok > 0)           ? LinkStatus.Conflict  :
        NotApplied > 0                        ? LinkStatus.NotApplied:
        Ok > 0                                ? LinkStatus.Ok        :
                                               LinkStatus.Empty;
}

internal static class StatusChecker
{
    public static PackageStatus Check(string package)
    {
        string pkgDir = Path.Combine(Env.DotfilesDir, package);
        int ok = 0, broken = 0, conflict = 0, notApplied = 0;

        if (!Directory.Exists(pkgDir))
            return new PackageStatus(package, 0, 0, 0, 0);

        foreach (string src in Directory.EnumerateFiles(pkgDir, "*", SearchOption.AllDirectories))
        {
            string rel  = Path.GetRelativePath(pkgDir, src);
            string dest = Path.Combine(Env.HomeDir, rel);

            // Buscar si algún componente del path es un symlink (stow puede enlazar carpetas enteras)
            bool symlinkFound = false;
            string check = Env.HomeDir;
            foreach (string component in rel.Split(Path.DirectorySeparatorChar))
            {
                check = Path.Combine(check, component);
                if (IsSymlink(check))
                {
                    symlinkFound = true;
                    if (File.Exists(check) || Directory.Exists(check))
                        ok++;
                    else
                        broken++;
                    break;
                }
            }

            if (!symlinkFound)
            {
                if (File.Exists(dest) || Directory.Exists(dest))
                    conflict++;
                else
                    notApplied++;
            }
        }

        return new PackageStatus(package, ok, broken, conflict, notApplied);
    }

    private static bool IsSymlink(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch { return false; }
    }
}
