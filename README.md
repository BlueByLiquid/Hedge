# Hedge

Hedge is a Windows 11 file browser with drawers on the left and right screen edges. Each drawer supports one or two tabbed detail views, search with regular expressions, subfolder search, Places, and file drag and drop with Windows File Explorer.

## Install

Run `Hedge-Setup-0.4.2.exe`. Setup compiles the app using the .NET Framework installed with Windows. It creates Start menu and desktop shortcuts and offers to launch Hedge. An installation failure is shown and recorded in `%LOCALAPPDATA%\Hedge\install.log`. Runtime errors are recorded in `%LOCALAPPDATA%\Hedge\startup.log`.

## Build

Run `makensis Installer.nsi` from this directory. The installer contains the C# source and builds it on the destination Windows PC. Explicit absolute .NET Framework references and `/noconfig` prevent the installer plugin `System.dll` from being mistaken for the framework assembly.

A Windows workflow builds the installer, installs it silently, and checks that the app reaches the visible drawer on startup. Windows-specific behavior such as Explorer drag and drop still needs interactive testing.

See README.txt for usage. EdgeFiles settings are imported on first launch when Hedge settings do not exist.

Interface icons are from [Lucide](https://lucide.dev), licensed under ISC and MIT; see LUCIDE-LICENSE.txt.
