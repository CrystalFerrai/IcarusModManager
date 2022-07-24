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

There is currently no installer for Icarus Mod Manager. Simply extract the contents of the release zip file somewhere on your PC and run IcarusModManager.exe to use the application. (An installer might be added in the future.)

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

Simply delete the folder containing the application files to remove it from your system. You can optionally also delete the application's user settings which are stored at `%localappdata%\IcarusModManager`. If you still have mods isntalled, you can either remove them using the app before you uninstall it, or you can manually remove them from your game's install folder by deleting the folder `Icarus\Content\Paks\Mods`.

# Creating Mods

If you are a mod author that wants to take advantage of the asset patching features offered by this application, take a look at [this guide](https://docs.google.com/document/d/1jxYX6o0YYKZmJQSNuogKRW88MnFo3NHvDx20UVI2T0A/view) for all of the details.

# Current State

This application is still under development. While the core functionality is in place, there are some extra features that are not yet implemented. It is also likely that there are bugs I don't know about. If you find any, please open an issue on the Github repo, and I will look into it when I get a chance. Include as much detail as you can. If the issue requires specific mods to reproduce, be sure to include how to get those mods in the issue description.