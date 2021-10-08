## Summary

Adding a new language involves following files to be added/modified.

### In-game translation

- locale/languages.json
- locale/[langcode].csv
- locale/[langcode]assetbundle

### Other files required by this repository

- LICENSE.Translation.[langname].md
- AUTHORS.Translation.[langname].md
- steam-related/thumbnail.psd
- steam-related/[langcode].Desc.txt

Do note that we accept WIP translations as well. And feel free to reach us anytime this guide is too hard to follow.

------

## Adding a new language entry (locale/languages.json)

This JSON file lists available languages. Each entry in the root dictionary represents a language, with its key shown in the configuration menu (F1). Do not change key names unless necessary, as it will make users with that language have their configuration reset to English.

 For example, this part defines a language for Korean:

```json
{
    "한국어": {
        "Texts": "ko.csv",
        "FontAssetBundle": "koassetbundle",
        "FontFallbacks": {
            "Acme-Regular SDF": "DoHyeon-Regular SDF",
            "PTSansNarrowRegular SDF": "GothicA1-Medium SDF",
            "PTSansNarrowBold SDF": "GothicA1-Bold SDF",
            "Roboto-Medium SDF": "NotoSansKR-Regular SDF",
            "Roboto-MediumItalic SDF": "NotoSansKR-Bold SDF",
            "Roboto-Black SDF": "NotoSansKR-Black SDF",
            "Journal SDF": "NanumBrushScript-Regular SDF"
        },
        "OptionalFeatures": "OverrideFontScalingAsFallbackOnes, KoreanWordWrapping",
        "ItalicSpacing": "0.2em"
    },
    //...
}
```

With each dictionary entry, following key/values are supported:
* `Texts`: Filename to the CSV file that contains the text translation.
* `FontAssetBundle`: (Optional) Filename to the Unity asset bundle file that contains the font files.
* `FontFallbacks`: (Optional) Mapping from each font of the base game to your corresponding font replacement. Each replacement must be present in the asset bundle file specified in `FontAssetBundle`.
* `ItalicSpacing`: (Optional) Used add some space after italicized text.
* `OptionalFeatures`: (Optional) List of comma-separated specific features required for the translation. See [OptionalFeatures](OptionalFeatures.md).

We recommend to use the filename convention in "[langcode].csv" and "[langcode]assetbundle" for consistency.

---

## Text translation (locale/[langcode].csv)

This is a CSV file that contains all strings for a language.

We recommend [Weblate](https://weblate.org/) to create this file. Unfortunately we do not host a centralized crowdsourcing translation site for now -- after all it costs hosting price and maintenance efforts -- so our recommendation is to use a [self-hosted Weblate docker instance](https://docs.weblate.org/en/latest/admin/install/docker.html) to setup your translation site. Just specifying this repository (or your forked one) as a Git source should be enough for Weblate to grab the strings and to begin your translation project.

The strict requirements for this file is:

* It must use a common CSV dialect, aka Excel variant, which is supported by various CSV libraries.
* It must have a header line.
* It must have a column named "source", which contains string IDs.
* It must have a column named "target", which contains translations.

In addition to this, we recommend to follow these guidelines for the sake of compatibility with Weblate:

* The CSV file should be operated in "quote everything" mode. This mode is also supported by various CSV libraries as well.
* It must have following columns in order: `"location","source","target","ID","fuzzy","context","translator_comments","developer_comments"`. See [Weblate documentation](https://docs.weblate.org/en/latest/formats.html#csv-files).

The `en.csv` file hosted in this repository follows all of the conventions above, and Weblate follows it as well.

---

## Replacing the fonts (locale/[langcode]assetbundle)

Check if your language uses glyphs unsupported by the game. If your texts entirely consist of Latin characters or Simplified Chinese then you might skip this section. Otherwise you should inject your font into the game.

These are only rough steps. It requires Unity to create an AssetBundle file.

1. Find the fonts you want to use as replacements. **The font must be publicly usable by this project.** [Google Fonts](https://fonts.google.com/) is probably one of the easiest way to grab them.
2. Install Unity. For the game build #12731 the recommended version is 2019.4.4 as the game is built around it.
3. Follow the [TextMesh Pro: Font Asset Creation](https://learn.unity.com/tutorial/textmesh-pro-font-asset-creation) tutorial to create SDF fonts.
4. Follow the [Introduction to Asset Bundles](https://learn.unity.com/tutorial/introduction-to-asset-bundles) tutorial to create an AssetBundle file that contains your SDF fonts.
5. Link the fonts properly in `languages.json` file (`FontAssetBundle`, `FontFallbacks`).
6. Run the game and see if your fonts are properly applied for your texts.

### Troubleshooting

1. The line does not align well / glyph is to big or small.
   - Try enable `"OptionalFeatures": "OverrideFontScalingAsFallbackOnes"` as a workaround.
2. Italicized texts overlap the following normal texts
   - Try specifying `"ItalicSpacing": "0.2em"` as a workaround.

---

## Properly attributing your work (LICENSE.Translation.[langname].md, AUTHORS.Translation.[langname].md)

Please include these files to your commit properly. The licensing text must include not only for your translated texts, but for any other things like used fonts as well.

------

## Steam Workshop descriptions and thumbnails (steam-related/[langcode].Desc.txt, steam-related/thumbnail.psd)

The description text will be manually placed in Steam Workshop description (https://steamcommunity.com/sharedfiles/filedetails/?id=2513567370) for each language.

And feel free to add your language name and "meep" into the thumbnail image so that users may recognize your language in the workshop.