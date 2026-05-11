namespace DotfilesManager.UI;

internal sealed class Summary
{
    private int _ok;
    private int _err;

    public void Reset() { _ok = 0; _err = 0; }

    public void TrackOk(string msg) { _ok++; Printer.Success(msg); }
    public void TrackErr(string msg) { _err++; Printer.Error(msg); }

    public void Print()
    {
        int okW = _ok.ToString().Length;
        int errW = _err.ToString().Length;
        int pad = 18;

        Console.WriteLine();
        Console.WriteLine($"  {Colors.Dim}┌─────────────────────────────┐{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}│{Colors.Reset}  Resumen de la operación    {Colors.Dim}│{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}├─────────────────────────────┤{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}│{Colors.Reset}  {Colors.Green}✔ OK   :{Colors.Reset} {Colors.Bold}{_ok}{Colors.Reset}{new string(' ', pad - okW)}{Colors.Dim}│{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}│{Colors.Reset}  {Colors.Red}✘ Error:{Colors.Reset} {Colors.Bold}{_err}{Colors.Reset}{new string(' ', pad - errW)}{Colors.Dim}│{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}└─────────────────────────────┘{Colors.Reset}");
        Console.WriteLine();
    }
}
