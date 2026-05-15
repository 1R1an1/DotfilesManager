using static DotfilesManager.UI.Colors;

namespace DotfilesManager.UI;

// Métodos de impresión con formato consistente en toda la app.
// Todos los mensajes tienen un ícono de color a la izquierda para identificar el tipo.
internal static class Printer
{
    public static string color = Cyan;
    public static void Info(string msg) => Console.WriteLine($"  {Cyan}●{Reset} {msg}");
    public static void Success(string msg) => Console.WriteLine($"  {Green}✔{Reset} {msg}");
    public static void Warn(string msg) => Console.WriteLine($"  {Yellow}!{Reset} {msg}");
    public static void Error(string msg) => Console.WriteLine($"  {Red}✘{Reset} {msg}");

    public static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"{color}{Bold}{title}{Reset}");
        // La línea de guiones tiene el mismo largo que el título
        Console.WriteLine($"{Dim}{new string('─', title.Length)}{Reset}");
    }

    public static void PressEnterToContinue()
    {
        Console.Write($"\n  {Dim}[Enter para volver]{Reset}");
        // Leer teclas hasta Enter sin mostrarlas en pantalla (intercept: true)
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.Enter) { }
        Console.WriteLine();
    }
}
