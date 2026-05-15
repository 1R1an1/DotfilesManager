namespace DotfilesManager.UI;

// Secuencias de escape ANSI para colorear el texto en la terminal.
// Cada string empieza con \x1b (byte 27 = ESC), que le indica a la terminal
// que lo que sigue es un comando de color, no texto a imprimir.
internal static class Colors
{
    public const string Red = "\x1b[0;31m";
    public const string Green = "\x1b[0;32m";
    public const string Yellow = "\x1b[1;33m";
    public const string Cyan = "\x1b[0;36m";
    public const string Purple = "\x1b[35m";
    public const string Bold = "\x1b[1m";
    public const string Dim = "\x1b[2m";
    public const string Reset = "\x1b[0m"; // vuelve al color y estilo por defecto
}
