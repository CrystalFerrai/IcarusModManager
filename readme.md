# Icarus Mod Manager

A GUI application for managing game mods for the game Icarus: First Cohort.

## Features

Icarus Mod Manager provides the following key features.

* Intuitive UX for installing and uninstalling game mods.
* One-click to reinstall mods after a game update.
* Support for data table (Json) mods as well as blueprint mods (and mods that do both).
* Asset patching feature means mods do not have to be recreated every time the game updates.
    * Mods must be built specifically to use this feature. If they are not, they will still work, but may need to be updated by the mod author each time the game updates. There is more information for mod authors later in this document.
* No mod loader required. The game will automatically load installed mods.

## Releases

Releases can be found [here](https://github.com/CrystalFerrai/IcarusModManager/releases).

## How to Install

Releases include an installer and a standalone zip. You can use whichever you prefer. With the standalone version, simply extract the contents to any directory and run the program directly from there.

If the app will not start, then you may not have the .NET 6 runtime installed. You can find [downloads here](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). On that page, look under the section ".NET Desktop Runtime" and choose "x64". Typically, you want to get the latest available version that starts with 6.0.

## How to Use

When you run the application, you are presented with an empty mod list. You can add mods to the list by either dragging them into the window or by pressing the "Add Mods" button and locating them.

Mods should be in Zip format. Mods in Pak format are also supported, but with less features. (If you are a mod author, see later in this document for more information about structuring your mod file.)

Once you have one or more mods in your list, press "Install Mods" to add them to the game.
* The game must not be running when you install or uninstall mods.
* If the application was not able to find your game install folder automatically, you will need to press the "Settings" button and enter the path in the dialog that opens. Then, you can try pressing the install mods button again.

Each mod in your list can be enabled or disabled. Only enabled mods will be installed when you press "Install Mods".

Reordering mods in the list affects their load order. This usually does not matter. However, If there is ever a conflict between two mods, the later mod (lower in the list) will win the conflict.

If you want to play unmodded, pressing "Uninstall Mods" will remove all mods from the game.

## How to Uninstall

Before uninstalling the application, you should first run it and tell it to uninstall mods. Uninstalling the application does not automatically uninstall mods you have added to the game. Another option is to remove mods from the game manually by deleting the `Mods` folder located at `Icarus\Content\Paks\Mods` in your game directory.

If you installed via the installer, you should have an uninstall link in your start menu. You can optionally uninstall from "Programs and Features". Either way does the same thing. If you are using the standalone distribution, simply delete the files.

User settings, such as your mod list, are not removed automatically by uninstalling the application. If you want to remove this data, you can delete it from your file system. User settings are stored at `%localappdata%\IcarusModManager`.

# Creating Mods

If you are a mod author that wants to take advantage of the asset patching features offered by this application, take a look at [this guide](https://docs.google.com/document/d/1jxYX6o0YYKZmJQSNuogKRW88MnFo3NHvDx20UVI2T0A/view) for all of the details.

# Building from Source

IcarusModManager is built in Visual Studio 2022. If you want to build from source, here are the basic steps to follow.

1. Clone the repo, including submodules.
    ```
    git clone --recursive https://github.com/CrystalFerrai/IcarusModManager.git
    ```
2. Download and install the [WiX Toolset build tools](https://github.com/wixtoolset/wix3/releases).
3. Download an install the [WiX Toolset Visual Studio 2022 Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2022Extension).
4. Open the file `IcarusModManager.sln` in Visual Studio.
5. Right click the solution in the Solution Explorer panel and select "Restore NuGet Dependencies".
6. Build the solution.

The Debug configuration includes only the mod manager and its dependencies. The Release configuration also includes building the installer.

# Reporting Issues

If you find any problems with the application, you can [open an issue](https://github.com/CrystalFerrai/IcarusModManager/issues). Include as much detail as you can. If the issue requires specific mods to reproduce, be sure to include how to get those mods in the issue description. I will look into reported issues when I find time.
