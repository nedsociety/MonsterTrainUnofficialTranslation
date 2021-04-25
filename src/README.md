## Codes

Here the code is housed. It's a VS2019 solution with three projects:

- TextExtractor: a BepInEx plugin that extracts all localizable texts.
- MonsterTrainUnofficialTranslation: a BepInEx plugin that applies the translation.

## Workflow

Prerequisite:

- The game
- [Mod Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=2187468759) installed
- Visual Studio 2019 or higher w/ C# support

1. Open the SteamAppsDir.props file from the root of this repository and adjust the path to your own.
2. Build TextExtractor. This will automatically install the plugin as local file in the Steam workshop directory well.
3. Now run the game once. It will extract the game strings to the "locale/en.csv" file to the root of the directory.
4. Translate the game as instructed in README.md at the root of this repository.
5. Build MonsterTrainUnofficialTranslation. This will automatically install the plugin as local file in the Steam workshop directory well.
6. Run the game and test if it is successfully applied.

