using System.Text;
using DotfilesManager.Core;
using static DotfilesManager.UI.Colors;

namespace DotfilesManager.UI;

internal static class PackageSearch
{
    private const int ReservedRows = 11;

    public static string[] Run(string title, IEnumerable<string> preselected)
    {
        Printer.Info("Cargando paquetes instalados...");
        string[] allPackages = LoadInstalled();

        var marked = new HashSet<string>(preselected);
        var query = new StringBuilder();
        int cursor = 0;
        int scroll = 0;
        string[] filtered = [];

        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                filtered = Filter(allPackages, query.ToString(), marked);

                int visibleRows = Math.Max(1, Console.WindowHeight - ReservedRows);

                if (filtered.Length > 0 && cursor >= filtered.Length)
                    cursor = filtered.Length - 1;
                if (filtered.Length == 0)
                    cursor = 0;

                if (cursor < scroll)
                    scroll = cursor;
                if (cursor >= scroll + visibleRows)
                    scroll = cursor - visibleRows + 1;

                Draw(title, query.ToString(), filtered, marked, cursor, scroll, visibleRows);

                var key = Console.ReadKey(intercept: true);

                // Movimiento del cursor
                if (key.Key == ConsoleKey.UpArrow)
                {
                    if (cursor > 0) cursor--;
                    continue;
                }

                if (key.Key == ConsoleKey.DownArrow)
                {
                    if (cursor < filtered.Length - 1) cursor++;
                    continue;
                }

                if (key.Key == ConsoleKey.Spacebar && filtered.Length > 0)
                {
                    string pkg = filtered[cursor];
                    if (marked.Contains(pkg)) marked.Remove(pkg);
                    else marked.Add(pkg);
                    if (cursor < filtered.Length - 1) cursor++;
                    continue;
                }

                if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Escape)
                    return [.. marked];

                if (key.Key == ConsoleKey.Backspace && query.Length > 0)
                {
                    query.Remove(query.Length - 1, 1);
                    cursor = 0;
                    scroll = 0;
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    query.Append(key.KeyChar);
                    cursor = 0;
                    scroll = 0;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    private static string[] Filter(string[] packages, string query, HashSet<string> marked)
    {
        if (string.IsNullOrEmpty(query))
            return [.. marked.OrderBy(p => p)];

        return packages
            .Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static void Draw(
        string title, string query, string[] filtered,
        HashSet<string> marked, int cursor, int scroll, int visibleRows)
    {
        Console.Clear();
        Printer.Header(title);
        Console.WriteLine();

        Console.WriteLine($"  {Printer.color}>{Reset} Buscar: {Bold}{query}{Reset}_");
        Console.WriteLine();

        if (string.IsNullOrEmpty(query) && filtered.Length == 0)
        {
            Console.WriteLine($"  {Dim}Escribí para buscar entre los paquetes instalados...{Reset}");
        }
        else if (filtered.Length == 0)
        {
            Console.WriteLine($"  {Dim}Sin resultados para \"{query}\"{Reset}");
        }
        else
        {
            int end = Math.Min(scroll + visibleRows, filtered.Length);

            if (scroll > 0)
                Console.WriteLine($"  {Dim}↑ {scroll} más arriba{Reset}");

            for (int i = scroll; i < end; i++)
            {
                string pkg = filtered[i];
                bool isMark = marked.Contains(pkg);
                string check = isMark ? $"{Green}✔{Reset} " : $"{Dim}○{Reset} ";

                if (i == cursor)
                    Console.WriteLine($"  {Printer.color}{Bold}▶{Reset} {check}{pkg}");
                else
                    Console.WriteLine($"    {check}{Dim}{pkg}{Reset}");
            }

            int remaining = filtered.Length - end;
            if (remaining > 0)
                Console.WriteLine($"  {Dim}↓ {remaining} más abajo{Reset}");
        }

        Console.WriteLine();
        Console.WriteLine($"  {Yellow}Space{Reset} marcar  {Yellow}↑↓{Reset} navegar  {Yellow}Enter{Reset} confirmar  {Yellow}Esc{Reset} cancelar");

        if (marked.Count > 0)
            Console.WriteLine($"  {Dim}Marcados ({marked.Count}): {string.Join(", ", marked.Take(5))}{(marked.Count > 5 ? "..." : "")}{Reset}");
    }

    private static string[] LoadInstalled()
    {
        try
        {
            var (code, stdout, stderr, _) = Shell.Run("yay", "-Qq", asUser: true);

            if (code != 0)
                return [];

            return stdout
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .OrderBy(p => p)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }
}