namespace DotfilesManager.UI;

internal static class Messenger
{
    public static void Error(string msg, Summary? summary = null)
    {
        if (summary != null) summary.TrackErr(msg);
        else Printer.Error(msg);
    }

    public static void Success(string msg, Summary? summary = null)
    {
        if (summary != null) summary.TrackOk(msg);
        else Printer.Success(msg);
    }
}