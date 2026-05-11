using static DotfilesManager.UI.Colors;

namespace DotfilesManager.UI;

internal static class Printer
{
    public static void Info(string msg)    => Console.WriteLine($"  {Cyan}●{Reset} {msg}");
    public static void Success(string msg) => Console.WriteLine($"  {Green}✔{Reset} {msg}");
    public static void Warn(string msg)    => Console.WriteLine($"  {Yellow}!{Reset} {msg}");
    public static void Error(string msg)   => Console.WriteLine($"  {Red}✘{Reset} {msg}");

    public static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"{Cyan}{Bold}{title}{Reset}");
        Console.WriteLine($"{Dim}{new string('─', title.Length)}{Reset}");
    }

    public static void PressEnterToContinue()
    {
        Console.Write($"\n  {Dim}[Enter para volver]{Reset}");
        ReadLineSilent();
        Console.WriteLine();
    }

    // Lee una línea sin eco (para los prompts de "presioná enter")
    private static void ReadLineSilent()
    {
        while (true)
        {
            var k = Console.ReadKey(intercept: true);
            if (k.Key == ConsoleKey.Enter) break;
        }
    }
}
