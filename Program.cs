using DotfilesManager.Core;
using DotfilesManager.Operations;
using DotfilesManager.UI;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Relanzar con sudo si no somos root, para que las operaciones de sistema funcionen
if (!Environment.IsPrivilegedProcess)
{
    System.Diagnostics.Process.Start("sudo", Environment.ProcessPath!)?.WaitForExit();
    return;
}

Env.LoadOrInit();

var summary = new Summary();



while (true)
{
    string[] options =
    [
        "Symlinks",
        "Ejecutar script"
    ];

    int choice = Menu.SelectOne("Dotfiles Manager", options);

    switch (choice)
    {
        case -1: Console.WriteLine(); return;
        case 0:
            bool Lbreak = true;
            while (Lbreak)
            {
                options =
                [
                    "Aplicar / instalar dotfiles",
                    "Ver estado de symlinks",
                    "Borrar symlinks",
                    "Agregar archivo al repo"
                ];
                Printer.color = Colors.Red;
                choice = Menu.SelectOne("Symlinks Manager", options);
                switch (choice)
                {
                    case -1: Lbreak = false; break;
                    case 0: ApplyOp.Run(summary); break;
                    case 1: StatusOp.Run(); break;
                    case 2: DeleteOp.Run(summary); break;
                    case 3: AddOp.Run(summary); break;
                }
                Printer.color = Printer.defaultColor;
            }
            break;
        case 1:
            Printer.color = Colors.Yellow;
            ExecuteOp.Run(summary);
            Printer.color = Printer.defaultColor;
            break;
    }
}
