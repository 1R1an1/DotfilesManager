namespace DotfilesManager.UI;

// Lleva la cuenta de operaciones exitosas y fallidas durante una operación,
// y las muestra en una tabla al final.
internal sealed class Summary
{
    private int _ok;
    private int _err;

    public void Reset() { _ok = 0; _err = 0; }

    // TrackOk y TrackErr incrementan el contador e imprimen el mensaje en pantalla
    public void TrackOk(string msg) { _ok++; Printer.Success(msg); }
    public void TrackErr(string msg) { _err++; Printer.Error(msg); }

    public void Print()
    {
        // El padding alinea el borde derecho de la tabla
        // independientemente de cuántos dígitos tengan los números
        int pad = 18;
        Console.WriteLine();
        Console.WriteLine($"  {Colors.Dim}┌─────────────────────────────┐{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}│{Colors.Reset}  Resumen de la operación    {Colors.Dim}│{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}├─────────────────────────────┤{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}│{Colors.Reset}  {Colors.Green}✔ OK   :{Colors.Reset} {Colors.Bold}{_ok}{Colors.Reset}{new string(' ', pad - _ok.ToString().Length)}{Colors.Dim}│{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}│{Colors.Reset}  {Colors.Red}✘ Error:{Colors.Reset} {Colors.Bold}{_err}{Colors.Reset}{new string(' ', pad - _err.ToString().Length)}{Colors.Dim}│{Colors.Reset}");
        Console.WriteLine($"  {Colors.Dim}└─────────────────────────────┘{Colors.Reset}");
        Console.WriteLine();
    }
}
