using DotfilesManager.Core;
using DotfilesManager.Operations;
using DotfilesManager.UI;

// Asegurar que la terminal soporte UTF-8
Console.OutputEncoding = System.Text.Encoding.UTF8;

Env.LoadOrInit();

var summary = new Summary();

string[] mainOptions =
[
    "Aplicar / instalar dotfiles",
    "Ver estado de symlinks",
    "Borrar symlinks",
    "Agregar archivo al repo",
];

while (true)
{
    int choice = Menu.SelectOne("Dotfiles Manager", mainOptions);

    switch (choice)
    {
        case -1:
            Console.WriteLine();
            return;
        case 0:
            ApplyOp.Run(summary);
            break;
        case 1:
            StatusOp.Run();
            break;
        case 2:
            DeleteOp.Run(summary);
            break;
        case 3:
            AddOp.Run(summary);
            break;
    }
}
