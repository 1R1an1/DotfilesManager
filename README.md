# 🛠️ dManager (Dotfiles Manager)

Un gestor de dotfiles y perfiles rápido e interactivo exclusivo para **Arch Linux** y sus derivados, desarrollado en .NET 10.

---

## 🚀 Requisitos

Asegurate de tener instalado en tu sistema:
* **Bash**
* **GNU Stow**
* **Yay**

---

## 📥 Instalación (Compilar desde el código fuente)

Requiere tener instalado el SDK de .NET 10 (`sudo pacman -S dotnet-sdk`).

```bash
git clone https://github.com/1R1an1/DotfilesManager.git
cd DotfilesManager
dotnet publish -c Release
sudo cp bin/Release/net10.0/linux-x64/publish/dManager /usr/local/bin/dmanager

```

----------

## 🛠️ Modo de Uso

### 1. Interfaz Interactiva (TUI)

Para abrir la aplicación con todos sus menús visuales, simplemente ejecutá el comando sin argumentos:

Bash

```
dmanager
```

### 2. Ver comandos disponibles (CLI)

Si querés ver la lista completa de comandos para usar directamente desde la consola o meter en scripts, ejecutá:

Bash

```
dmanager -h
```
----------

## 🐛 Reporte de Errores e Issues
⚠️ **Importante:** Antes de abrir un reporte, asegurate de hacer un `git pull` y volver a compilar para verificar que estás usando la **última versión** disponible del proyecto.

Si el error persiste, encontrás un bug o querés proponer una mejora, abrí un [Issue](https://github.com/1R1an1/DotfilesManager/issues)

---

## 📄 Licencia
Este proyecto está bajo la licencia **Mozilla Public License 2.0 (MPL-2.0)**.
