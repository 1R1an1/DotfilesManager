using static DotfilesManager.UI.Colors;

namespace DotfilesManager.UI;

// Componentes interactivos de la TUI: menú de selección simple, múltiple y confirmación.
// La navegación usa vim keys (j/k/gg/G) y flechas del teclado.
// El "render" es simple: Console.Clear() + redibujar todo en cada tecla presionada.
internal static class Menu
{
    // Muestra una lista y retorna el índice elegido.
    // Retorna -1 si el usuario cancela con q.
    public static int SelectOne(string title, string[] items)
    {
        int cursor = 0;
        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                Draw(title, items, cursor, selected: null);
                Console.WriteLine($"  {Dim}j/↓ abajo  k/↑ arriba  gg inicio  G fin  Enter confirmar  q cancelar{Reset}");
                Console.WriteLine();

                var action = ReadKey(items.Length, ref cursor, out int directJump);

                if (action == KeyAction.Cancel) return -1;
                if (action == KeyAction.Confirm) return cursor;
                if (action == KeyAction.DirectJump) return directJump;
            }
        }
        finally { Console.CursorVisible = true; }
    }

    // Muestra una lista con checkboxes y retorna los índices marcados.
    // Retorna array vacío si el usuario cancela con q.
    public static int[] SelectMulti(string title, string[] items, bool[]? preselected = null)
    {
        int cursor = 0;
        bool[] marked = new bool[items.Length];

        if (preselected is not null)
            for (int i = 0; i < Math.Min(marked.Length, preselected.Length); i++)
                marked[i] = preselected[i];

        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                Draw(title, items, cursor, marked);
                Console.WriteLine($"  {Yellow}a{Reset} todos  {Yellow}Space{Reset} marcar  {Yellow}Enter{Reset} confirmar  {Dim}j/k/gg/G/q{Reset}");
                Console.WriteLine();

                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                switch (key.KeyChar)
                {
                    case 'q': case 'Q': return [];
                    case 'j': if (cursor < items.Length - 1) cursor++; break;
                    case 'k': if (cursor > 0) cursor--; break;
                    case 'G': cursor = items.Length - 1; break;
                    case 'g': if (TryReadSecondG()) cursor = 0; break;

                    case 'a':
                    case 'A':
                        // Si todos están marcados, desmarcar todos; si no, marcar todos
                        bool allMarked = Array.TrueForAll(marked, m => m);
                        for (int i = 0; i < items.Length; i++) marked[i] = !allMarked;
                        break;

                    case ' ':
                        marked[cursor] = !marked[cursor];
                        if (cursor < items.Length - 1) cursor++;
                        break;

                    case '\r':
                    case '\n':
                        return Enumerable.Range(0, items.Length).Where(i => marked[i]).ToArray();

                    default:
                        if (key.Key == ConsoleKey.UpArrow && cursor > 0) cursor--;
                        if (key.Key == ConsoleKey.DownArrow && cursor < items.Length - 1) cursor++;
                        if (key.KeyChar >= '1' && key.KeyChar <= '9')
                        {
                            int n = key.KeyChar - '1';
                            if (n < items.Length) marked[n] = !marked[n];
                        }
                        break;
                }
            }
        }
        finally { Console.CursorVisible = true; }
    }

    // Muestra una pregunta s/N y retorna true si el usuario escribe "s"
    public static bool Confirm(string msg = "¿Seguro?")
    {
        Console.WriteLine();
        Console.Write($"  {Yellow}?{Reset} {msg} [s/N]: ");
        return Console.ReadLine()?.Trim().ToLower() == "s";
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private enum KeyAction { Move, Confirm, Cancel, DirectJump }

    // Lee una tecla y actualiza el cursor. Retorna la acción correspondiente.
    private static KeyAction ReadKey(int total, ref int cursor, out int directJump)
    {
        directJump = -1;
        ConsoleKeyInfo key = Console.ReadKey(intercept: true);

        switch (key.KeyChar)
        {
            case 'q': case 'Q': return KeyAction.Cancel;
            case 'j': if (cursor < total - 1) cursor++; return KeyAction.Move;
            case 'k': if (cursor > 0) cursor--; return KeyAction.Move;
            case 'G': cursor = total - 1; return KeyAction.Move;
            case 'g': if (TryReadSecondG()) cursor = 0; return KeyAction.Move;
            case '\r': case '\n': return KeyAction.Confirm;
            default:
                if (key.Key == ConsoleKey.UpArrow && cursor > 0) cursor--;
                if (key.Key == ConsoleKey.DownArrow && cursor < total - 1) cursor++;
                if (key.KeyChar >= '1' && key.KeyChar <= '9')
                {
                    int n = key.KeyChar - '1';
                    if (n < total) { directJump = n; return KeyAction.DirectJump; }
                }
                return KeyAction.Move;
        }
    }

    // Espera hasta 300ms a ver si viene una segunda 'g' para implementar 'gg' (ir al inicio)
    private static bool TryReadSecondG()
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(300);
        while (!Console.KeyAvailable && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        return Console.KeyAvailable && Console.ReadKey(intercept: true).KeyChar == 'g';
    }

    // Dibuja la lista completa. selected == null significa modo simple (sin checkboxes).
    private static void Draw(string title, string[] items, int cursor, bool[]? selected)
    {
        Console.Clear();
        Printer.Header(title);
        Console.WriteLine();

        for (int i = 0; i < items.Length; i++)
        {
            string check = selected is null ? "  "
                : selected[i] ? $"{Green}✔{Reset} "
                              : $"{Dim}○{Reset} ";

            if (i == cursor)
                Console.WriteLine($"  {Printer.color}{Bold}▶{Reset} {check}{i + 1}) {items[i]}");
            else
                Console.WriteLine($"    {check}{Dim}{i + 1}) {items[i]}{Reset}");
        }

        Console.WriteLine();
    }
}
