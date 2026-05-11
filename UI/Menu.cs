using static DotfilesManager.UI.Colors;

namespace DotfilesManager.UI;

internal static class Menu
{
    // ── Selección simple ──────────────────────────────────────────────────────
    // Retorna el índice elegido, o -1 si el usuario canceló con q.
    public static int SelectOne(string title, string[] items)
    {
        int cursor = 0;
        int total  = items.Length;

        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                Draw(title, items, cursor, selected: null);
                Console.WriteLine($"  {Dim}j/↓ abajo  k/↑ arriba  gg inicio  G fin  Enter confirmar  q cancelar{Reset}");
                Console.WriteLine();

                var action = ReadKey(total, ref cursor, out int directJump);

                if (action == KeyAction.Cancel)  return -1;
                if (action == KeyAction.Confirm) return cursor;
                if (action == KeyAction.DirectJump) { cursor = directJump; return cursor; }
                // Move: cursor ya fue actualizado dentro de ReadKey
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    // ── Selección múltiple ────────────────────────────────────────────────────
    // Retorna los índices marcados, o array vacío si canceló.
    public static int[] SelectMulti(string title, string[] items)
    {
        int    cursor  = 0;
        int    total   = items.Length;
        bool[] marked  = new bool[total];

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
                    case 'q':
                    case 'Q':
                        return [];

                    case 'j':
                        if (cursor < total - 1) cursor++;
                        break;

                    case 'k':
                        if (cursor > 0) cursor--;
                        break;

                    case 'G':
                        cursor = total - 1;
                        break;

                    case 'g':
                        if (TryReadSecondG()) cursor = 0;
                        break;

                    case 'a':
                    case 'A':
                        bool allMarked = Array.TrueForAll(marked, m => m);
                        for (int i = 0; i < total; i++) marked[i] = !allMarked;
                        break;

                    case ' ':
                        marked[cursor] = !marked[cursor];
                        if (cursor < total - 1) cursor++;
                        break;

                    case '\r':
                    case '\n':
                        var result = new List<int>();
                        for (int i = 0; i < total; i++)
                            if (marked[i]) result.Add(i);
                        return [.. result];

                    default:
                        if (key.Key == ConsoleKey.UpArrow   && cursor > 0)        cursor--;
                        if (key.Key == ConsoleKey.DownArrow && cursor < total - 1) cursor++;

                        // Teclas numéricas 1-9
                        if (key.KeyChar >= '1' && key.KeyChar <= '9')
                        {
                            int n = key.KeyChar - '1';
                            if (n < total) marked[n] = !marked[n];
                        }
                        break;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    // ── Confirmación s/N ──────────────────────────────────────────────────────
    public static bool Confirm(string msg = "¿Seguro?")
    {
        Console.WriteLine();
        Console.Write($"  {Yellow}?{Reset} {msg} [s/N]: ");
        string? resp = Console.ReadLine();
        return resp?.ToLower() == "s";
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private enum KeyAction { Move, Confirm, Cancel, DirectJump }

    private static KeyAction ReadKey(int total, ref int cursor, out int directJump)
    {
        directJump = -1;
        ConsoleKeyInfo key = Console.ReadKey(intercept: true);

        switch (key.KeyChar)
        {
            case 'q':
            case 'Q':
                return KeyAction.Cancel;

            case 'j':
                if (cursor < total - 1) cursor++;
                return KeyAction.Move;

            case 'k':
                if (cursor > 0) cursor--;
                return KeyAction.Move;

            case 'G':
                cursor = total - 1;
                return KeyAction.Move;

            case 'g':
                if (TryReadSecondG()) cursor = 0;
                return KeyAction.Move;

            case '\r':
            case '\n':
                return KeyAction.Confirm;

            default:
                if (key.Key == ConsoleKey.UpArrow   && cursor > 0)        cursor--;
                if (key.Key == ConsoleKey.DownArrow && cursor < total - 1) cursor++;

                if (key.KeyChar >= '1' && key.KeyChar <= '9')
                {
                    int n = key.KeyChar - '1';
                    if (n < total) { directJump = n; return KeyAction.DirectJump; }
                }
                return KeyAction.Move;
        }
    }

    // Lee un segundo 'g' con timeout para implementar 'gg'
    private static bool TryReadSecondG()
    {
        if (!Console.KeyAvailable)
        {
            // Esperar hasta ~300ms
            var deadline = DateTime.UtcNow.AddMilliseconds(300);
            while (!Console.KeyAvailable && DateTime.UtcNow < deadline)
                Thread.Sleep(10);
        }
        if (!Console.KeyAvailable) return false;
        var k = Console.ReadKey(intercept: true);
        return k.KeyChar == 'g';
    }

    // Dibuja la lista (modo simple o multi)
    private static void Draw(string title, string[] items, int cursor, bool[]? selected)
    {
        Console.Clear();
        Printer.Header(title);
        Console.WriteLine();

        for (int i = 0; i < items.Length; i++)
        {
            bool isMulti = selected is not null;
            bool isCursor = i == cursor;

            string checkmark = isMulti
                ? (selected![i] ? $"{Green}✔{Reset} " : $"{Dim}○{Reset} ")
                : "  ";

            if (isCursor)
                Console.WriteLine($"  {Cyan}{Bold}▶{Reset} {checkmark}{i + 1}) {items[i]}");
            else
                Console.WriteLine($"    {checkmark}{Dim}{i + 1}) {items[i]}{Reset}");
        }

        Console.WriteLine();
    }
}
