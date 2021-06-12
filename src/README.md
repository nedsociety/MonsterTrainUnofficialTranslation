# Here the code is housed

## Prerequisite

Before trying to build the projects, make sure you have:

- The game
- [Mod Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=2187468759) installed
- Visual Studio 2019 or higher w/ C# support

Then, open `SteamAppsDir.props` file at the root of the repository (not this directory) and adjust the path to your own.

## TextExtractor project

This is a BepInEx plugin that extracts all localizable strings. This plugin is NOT meant to be published to Steam Workshop.

A successful build will try to automatically install the plugin into Steam as a local mod. (If you don't want that behavior, make sure to disable the post-build step for the project) Once the game is run with this plugin, it will extract the game strings to the "locale/en.csv" file to the root of this repository.

Note that you should NOT run this mod alongside the main one (MonsterTrainUnofficialTranslation). As the patch works by overwriting the English strings, it might extract the translated strings from the game, depending on the load order of those mods. Make sure to remove the local mod from Steam Workshop directory after use, or at least make sure to disable it in the Mod Settings.

## MonsterTrainUnofficialTranslation

This is the main BepInEx plugin that applies the translation. This plugin is published to [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2513567370).

A successful build will package the plugin onto `package` directory of the root of this repo. You may copy the "content" directory into the Steam Workshop directory to make it a testable local mod.

## `External` directory

Houses external plugins distributed alongside with the plugins.

- [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (prebuilt binary, tag v16.3)

## `Tools` directory

Tools for use in dev process.

- Mono binary for debugging the process with dnSpy.

