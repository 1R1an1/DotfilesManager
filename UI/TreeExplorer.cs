using static DotfilesManager.UI.Colors;

namespace DotfilesManager.UI;

// Explorador de árbol interactivo para seleccionar archivos y carpetas.
// Permite navegar dentro y fuera de carpetas y marcar items individuales.
internal static class TreeExplorer
{
    // Retorna las rutas absolutas de los items marcados, o array vacío si canceló.
    public static string[] Run(string title, string rootDir)
    {
        var marked = new HashSet<string>();
        var current = rootDir;
        int cursor = 0;

        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                string[] entries = GetEntries(current);
                if (cursor >= entries.Length && entries.Length > 0)
                    cursor = entries.Length - 1;

                Draw(title, rootDir, current, entries, marked, cursor);

                var key = Console.ReadKey(intercept: true);

                switch (key.KeyChar)
                {
                    // Cancelar
                    case 'q':
                    case 'Q':
                        return [];

                    // Confirmar selección
                    case 'c':
                    case 'C':
                        return [.. marked];

                    // Navegar abajo
                    case 'j':
                        if (cursor < entries.Length - 1) cursor++;
                        break;

                    // Navegar arriba
                    case 'k':
                        if (cursor > 0) cursor--;
                        break;

                    // Salir de la carpeta actual (subir un nivel)
                    case 'h':
                        if (current != rootDir)
                        {
                            // Volver a la carpeta padre sin salir del root
                            string parent = Path.GetDirectoryName(current)!;
                            if (parent.StartsWith(rootDir))
                            {
                                // Posicionar el cursor en la carpeta de la que salimos
                                string[] parentEntries = GetEntries(parent);
                                cursor = Array.IndexOf(parentEntries, current);
                                if (cursor < 0) cursor = 0;
                                current = parent;
                            }
                        }
                        break;

                    // Marcar/desmarcar item
                    case ' ':
                        if (entries.Length > 0)
                        {
                            string entry = entries[cursor];
                            if (marked.Contains(entry))
                                marked.Remove(entry);
                            else
                                marked.Add(entry);
                            if (cursor < entries.Length - 1) cursor++;
                        }
                        break;

                    default:
                        // Flechas
                        if (key.Key == ConsoleKey.UpArrow && cursor > 0) cursor--;
                        if (key.Key == ConsoleKey.DownArrow && cursor < entries.Length - 1) cursor++;

                        // Backspace: salir de carpeta (igual que h)
                        if (key.Key == ConsoleKey.Backspace && current != rootDir)
                        {
                            string parent = Path.GetDirectoryName(current)!;
                            if (parent.StartsWith(rootDir))
                            {
                                string[] parentEntries = GetEntries(parent);
                                cursor = Array.IndexOf(parentEntries, current);
                                if (cursor < 0) cursor = 0;
                                current = parent;
                            }
                        }

                        // Enter: entrar a carpeta
                        if (key.Key == ConsoleKey.Enter && entries.Length > 0)
                        {
                            string entry = entries[cursor];
                            if (Directory.Exists(entry))
                            {
                                current = entry;
                                cursor = 0;
                            }
                        }
                        break;
                }
            }
        }
        finally { Console.CursorVisible = true; }
    }

    // Retorna los archivos y carpetas del directorio dado, ordenados: carpetas primero.
    private static string[] GetEntries(string dir)
    {
        if (!Directory.Exists(dir)) return [];

        return Directory.EnumerateFileSystemEntries(dir)
            .OrderBy(e => !Directory.Exists(e)) // carpetas primero (false < true)
            .ThenBy(e => Path.GetFileName(e))
            .ToArray();
    }

    private static void Draw(string title, string rootDir, string current, string[] entries, HashSet<string> marked, int cursor)
    {
        Console.Clear();
        Printer.Header(title);

        // Mostrar la ruta actual relativa al root para orientar al usuario
        string relPath = Path.GetRelativePath(rootDir, current);
        Console.WriteLine($"\n  {Dim}Ubicación: {(relPath == "." ? "/" : "/" + relPath)}{Reset}\n");

        if (entries.Length == 0)
        {
            Console.WriteLine($"  {Dim}Carpeta vacía{Reset}");
        }
        else
        {
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                string name = Path.GetFileName(entry);
                bool isDir = Directory.Exists(entry);
                bool isMark = marked.Contains(entry);
                string check = isMark ? $"{Green}✔{Reset} " : $"{Dim}○{Reset} ";
                // Las carpetas muestran / al final para distinguirlas de archivos
                string label = isDir ? $"{Cyan}{name}/{Reset}" : name;

                if (i == cursor)
                    Console.WriteLine($"  {Cyan}{Bold}▶{Reset} {check}{label}");
                else
                    Console.WriteLine($"    {check}{Dim}{label}{Reset}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"  {Yellow}Space{Reset} marcar  {Yellow}Enter{Reset} abrir carpeta  {Yellow}h/←{Reset} salir  {Yellow}c{Reset} confirmar  {Yellow}q{Reset} cancelar");

        if (marked.Count > 0)
        {
            // Mostrar los marcados como rutas relativas al root
            var markedNames = marked.Select(m => Path.GetRelativePath(rootDir, m)).Take(5);
            string suffix = marked.Count > 5 ? "..." : "";
            Console.WriteLine($"  {Dim}Marcados ({marked.Count}): {string.Join(", ", markedNames)}{suffix}{Reset}");
        }

        Console.WriteLine();
    }
}