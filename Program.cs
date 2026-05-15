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

string[] mainOptions =
[
    "Aplicar / instalar dotfiles",
    "Ver estado de symlinks",
    "Borrar symlinks",
    "Agregar archivo al repo",
    "Ejecutar script",
];

while (true)
{
    int choice = Menu.SelectOne("Dotfiles Manager", mainOptions);

    switch (choice)
    {
        case -1: Console.WriteLine(); return;
        case 0: ApplyOp.Run(summary); break;
        case 1: StatusOp.Run(); break;
        case 2: DeleteOp.Run(summary); break;
        case 3: AddOp.Run(summary); break;
        case 4: ExecuteOp.Run(summary); break;
    }
}
