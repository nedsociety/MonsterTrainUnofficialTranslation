# MonsterTrainUnofficialTranslation (WIP)

This repository houses both codes and translations for the unofficial translations of [Monster Train](https://store.steampowered.com/app/1102190/Monster_Train/).

## Contributing to the Translation

Currently we don't have a centralized site for crowdsourced translations. Please contact to the language maintainers:

- Korean: nedsociety, see #1 .

## Adding a new language

Please add a pull request to this repository. Following files must be included in the changelist:

- locale/languages.json
- locale/[langcode].csv
- locale/[langcode]assetbundle
  - The asset bundle consisting of TextMeshPro-fied fonts as referred in languages.json file.
  - This is optional if you don't specify `FontAssetBundle` in languages.json file.
- LICENSE.Translation.[langname].md
- AUTHORS.Translation.[langname].md

Do note that you will need to include fonts which are publicly available, like ones licensed under [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL). [Google Fonts](https://fonts.google.com/) is probably the easiest way to find ones.

## External codes

This repository makes use of the following codes:

- [BepInEx](https://github.com/BepInEx/BepInEx)
- [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
- [CsvHelper](https://joshclose.github.io/CsvHelper/)
- [Json.NET](https://www.newtonsoft.com/json)

## Disclaimer

*Monster Train* is a trademark of *Shiny Shoe LLC*. Unless otherwise stated, the authors and the contributors of this repository is not affiliated with nor endorsed by Shiny Shoe LLC.