# Creating Mods

This document is intended for authors of Icarus mods who want to make use of the features of Icarus Mod Manager.

# Mod Structure
A mod should be a single Zip file containing all of your mod data. Mod data consists of the following types of files:
* mod.info - A file containing metadata about your mod such as name, version, author, etc.
* *.patch - Patch files allow you to modify game assets without completely replacing them.
* *.pak - Pak files will be installed as-is, allowing you to add new assets or overwrite existing game assets.

## mod.info

This file has the following format:

```json
{
  "schema_version": 1,
  "id": "Crystal/MyMod",
  "name": "My Mod",
  "author": "Crystal",
  "description": "I can has description.",
  "version": "0.1.0",
  "web": "https://nowhere.com"
}
```

#### schema_version

This is a required property. Currently the only valid value is 1. The purpose of this is to support changes to the format in the future in a backwards compatible manner.

#### id
A unique identifier for your mod. This can be any arbitrary string, but it should be globally unique. If two mods have the same ID, the mod manager will not allow them to both be installed at the same time because it will think they are the same mod.

#### name
The name of the mod as presented to the user. It does not need to be unique.

#### author
The name of the mod author (person, group, company, whatever)..

#### description
A short description of the mod. Ideally a paragraph or less.

#### version
A version number for the mod. This is used for informational purposes only and can be any string.

#### web
A link to a website associated with the mod. This could be a Github, a documentation site, whatever makes sense.

## Patch files (*.patch)

Patch files allow a mod to modify game files without completely overwriting them. The patch will always be applied to the current version of the game asset from the user’s game installation. This means multiple mods can patch the same file and also means it is far less likely that a mod will have to be manually updated every time the game updates.

There are currently two types of patch files: data table patches and actor patches. All patches use the .patch file extension.

### Data Table Patch

A data table patch allows you to modify one of the game’s Json data tables from Data.pak. There are three different operations that can be performed within a patch.

#### schema_version

This is a required property. Currently the only valid value is 1. The purpose of this is to support changes to the format in the future in a backwards compatible manner.

#### type

Specifies the type of patch. Always “Json” for Json patches.

#### target

The path to the asset that the patch should modify.

#### data/patches

An array of patches to apply. The format varies depending on the opration being performed.

#### Add Operation

Adds a new row to the table. Expects the following properties:

* `row`: The name of the row to add
* `value`: The content of the row to add

Note: The value should not contain a "Name" property. The "Name" property will be added by the mod manager based on the specified row name when installing the mod.

The following example adds a custom map icon to the game for use by the mod.

```json
{
	"schema_version": 1,
	"type": "DataTable",
	"target": "UI/D_MapIcons.json",
	"data":
	{
		"patches":
        [
			{
				"op": "add",
				"row": "InfoModWaypoint",
				"value":
				{
					"WidgetClass": "/Game/BP/Objects/World/Items/Deployables/Radar/UMG_RadarIcon.UMG_RadarIcon_C",
					"MapIcon": "Texture2D'/Game/Assets/2DArt/UI/Icons/MapIcons/Icon_Map_Location4.Icon_Map_Location4'",
					"ZOrder": 50,
					"ScaleFactor": 0.5,
					"Color": {
						"R": 255,
						"G": 96,
						"B": 255,
						"A": 255
					}
				}
			}
        ]
	}
}
```

#### Remove Operation

Removes a row from the table. Expects the following properties:

* `row`: The name of the row to remove

The following example removes the recipe for concrete mix from the game (which would break the game if it is the only change made).

```json
{
	"schema_version": 1,
	"type": "DataTable",
	"target": "Crafting/D_ProcessorRecipes.json",
	"data":
	{
		"patches":
        [
			{ "op": "remove", "row": "Concrete_Mix" }
        ]
	}
}

```

#### Alter Operation

Alters the data within a row or within the table defaults. Expects the following properties:

* `row`: The name of the row to alter. Specify `null` to alter table defaults.
* `patches`: A list of json patches to apply to the row. This is an array of operations to perform, executed in order. Each operation contains an array of individual steps. If any step fails, the operation aborts (no changes are made) and the next operation starts.

Patch operations utilize the JSON Patch specification allowing you to do things such as replace, add, move and remove tokens. See [https://jsonpatch.com](https://jsonpatch.com/) for an overview of the specification.

Note: Paths within patch operation steps are evaluated relative to the row being operated on.

The following example alters the elecric deep ore mining drill, removing all additional stats, then adding a specific one. The steps are separated into different operations so that if the remove fails (because the property does not exist), the add will still run.

```json
{
	"schema_version": 1,
	"type": "DataTable",
	"target": "Items/D_ItemsStatic.json",
	"data":
	{
		"patches":
        [
			{
				"op": "alter",
				"row": "Deep_Mining_Drill_Electric",
				"patches":
				[
					[
						{ "op": "remove", "path": "/AdditionalStats" }
					],
					[
						{
							"op": "add", "path": "/AdditionalStats", "value":
							{
								"(Value=\"BaseDeepMiningDrillSpeed_+%\")": 100
							}
						}
					]
				]
			}
        ]
	}
}
```

### Actor Patch

An actor patch allows you to add actor components to an actor blueprint. This provides an entry point for blueprint based mods to function. You add a custom component to an existing game actor, and it will be instantiated by the game whenever that actor type is instantiated. The file has the following format:

```json
{
  "schema_version": 1,
  "type": "Actor",
  "target": "Icarus/Content/BP/Player/BP_IcarusPlayerControllerSurvival.uasset",
  "data":
  {
    "components":
    [
      "/Game/Mods/TestMod/TestModComponent"
    ]
  }
}
```

The example above adds a component called `TestModComponent` to the player controller actor. So each player will have an instance of the component running.

#### schema_version

This is a required property. Currently the only valid value is 1. The purpose of this is to support changes to the format in the future in a backwards compatible manner.

#### type

Specifies the type of patch. Always “Actor” for actor patches.

#### target

The path to the asset that the patch should modify.

#### data/components

An array of components to add to the target actor. Usually, your mod would also include a .pak file containing the component you wish to add.

### Asset Copy Patch

As asset copy patch allows you to copy an existing game asset to a new location. This updates names within the asset so that it will function at the new location. However, it does not update any external references to the asset. External references will continue to point to the original asset.

The purpose of an asset copy is to allow a mod to replace the original asset with a custom override that extends from the copied original asset. This is primarily for blueprint assets, allowing the mod to override functionality of the original blueprint without fully replacing it.

When authoring a mod with an asset copy, you should include an empty/stripped version of the renamed original in your UProject and set it as the base class for your replacement asset. Do not include the stripped original in your shipped mod.

The patch file has the following format:

```json
{
	"schema_version": 1,
	"type": "AssetCopy",
	"target": "Icarus/Content/BP/Player/BP_IcarusPlayerControllerSurvival.uasset",
	"data":
	{
		"path": "Icarus/Content/BP/Player/BP_IcarusPlayerControllerSurvival_Original.uasset"
	}
}
```

#### schema_version

This is a required property. Currently the only valid value is 1. The purpose of this is to support changes to the format in the future in a backwards compatible manner.

#### type

Specifies the type of patch. Always “AssetCopy” for asset copy patches.

#### target

The path to the asset that the patch should copy.

#### data/path

The path where the asset will be copied when the patch is installed.

### Json Patch (Deprecated)

_NOTE: You should now use data table patches instead of Json patches. Data table patches are more resilient to game updates because they reference rows by name rather than by index._

A Json patch allows you to modify one of the game’s Json based data tables from Data.pak. This file has the following format:

```json
{
  "schema_version": 1,
  "type": "Json",
  "target": "Prospects/D_ProspectList.json",
  "data":
  {
    "patches":
    [
      [
        { "op": "replace", "path": "/Rows/65/MetaDepositSpawns/1/MinMetaAmount", "value": 400 },
        { "op": "replace", "path": "/Rows/65/MetaDepositSpawns/1/MaxMetaAmount", "value": 600 }
      ]
    ]
  }
}
```

The example above modifies one specific exotics node on one specific mission to contain a different amount of exotics.

#### schema_version

This is a required property. Currently the only valid value is 1. The purpose of this is to support changes to the format in the future in a backwards compatible manner.

#### type

Specifies the type of patch. Always “Json” for Json patches.

#### target

The path to the asset that the patch should modify.

#### data/patches

An array of Json patches to apply. Uses the JSON Patch specification allowing you to do things such as replace, add, move and remove tokens. See [https://jsonpatch.com](https://jsonpatch.com/) for an overview of the specification.

## Pak Files
Your mod’s zip file can have any number of pak files in it, and they will be directly installed into the game when the user installs the mod. It is important to remember that the file name must end with _P or the game will not load it.

# Getting Started with Blueprint Modding
If you want to make a blueprint mod, you will want to start by installing the Unreal Engine version that Icarus is built on (4.27.2 at the time of writing this). The easiest way to get Unreal Engine is using the Epic Games Launcher ([more info here](https://www.unrealengine.com/download)).

## Make Content

Once you have Unreal installed, run it and  create a new C++ based project named “Icarus” (name needs to match the project name used by the game itself).  It will take a long time for Unreal to start for the first time, and a long time for your project to build and start for the first time, but it will go faster after the first time.

At this point, you need to know how blueprints and Unreal in general work. If you don’t, go do some research, watch tutorials etc. Then come back to your mod project.

In your mod project, create a folder called “Mods” (not required, but strongly recommended). Inside of that folder create a folder with the name of your mod. Within that folder, create whatever content you want to add to the game. You will most likely want at least one actor component that you can inject using an Actor Patch (see above).

## Build and Package

Once you have created some content, go to the file menu and choose “Cook Content for Windows”. This will build your content into a game-ready format. You can find the output on your disk within your project folder at Saved\Cooked\WindowsNoEditor\Icarus\Content. Make a note of the full path to this location. You will need it soon.

The next step is packaging your content into a .pak file. Included with the Unreal engine is a command line utility called UnrealPak which can be found in your engine install directory at Engine\Binaries\Win64\UnrealPak.exe. This will allow you to package your content for including in your mod. Note the full path to this utility somewhere.

Now create a response.txt file. This is a special type of file needed when packaging to ensure your mod files get mounted at the proper location within the game. The response file itself should be outside of your mod directory. The content of the response file is a list of mappings from files on your disk to game asset paths. Here is an example:

```
D:\Icarus\Saved\Cooked\WindowsNoEditor\Icarus\Content\Mods\MyMod\*.* ../../../Icarus/Content/Mods/MyMod/*.*
```

Note that the game asset path should always start with `../../../` because it ends up being relative to the actual game exe which is inside of Icarus/Binaries/Win64.

Next, create a .bat file that you can run to package your mod using UnrealPak and the response.txt file you created. Here is an example which will use a response file located at D:\MyMod\response.txt and output a pak file to D:\MyMod\MyMod_P.pak:

```
set UnrealPak=D:\UE4\UnrealEngine\Engine\Binaries\Win64\UnrealPak.exe
%UnrealPak% D:\MyMod\MyMod_P.pak -Create=D:\MyMod\response.txt
```

Make sure your pak file name ends with `_P` or the game will ignore it.

Now you have a pak file you can include with your mod. To iterate on the content, you would make changes in Unreal Editor, cook for Windows again, run your batch script to package things, then add the new pak to your mod.

## Example

For reference, I have created a blueprint mod called ExampleMod. All it does is show the player’s current coordinates in the top left corner of the screen, but it demonstrates attaching a custom actor component to a player controller using a patch.

* Packaged: [ExampleMod-1.0.0.zip](https://drive.google.com/file/d/117rXNIRmrgB-SMHNkDpL1xcIY5fZyFf1/view)
* Source: [ExampleModSource.zip](https://drive.google.com/file/d/13EdYyh4jQWcGbVS6TaM_qlz9HuD7g4sE/view)

The packaged version can be installed using the mod manager. The source version can be added to an existing Icarus Unreal project, allowing you to view and modify the blueprints.
