# OptionalFeatures

These features are implemented on per-need basis, so if your translation needs a specific processing, feel free to request on adding one.

## Font-related features

* `OverrideFontScalingAsFallbackOnes`: If enabled, apply the font scaling adjustment.

Note: The way how font patching works is to dynamically register your fonts as "fallback fonts" of the base fonts respectively, i.e. as replacement whenever a glyph is missing on the base fonts. Unfortunately the text engine used in Monster Train does not align texts well, if there are significant differences between the base font and the fallback one in terms of font face information (e.g., line height, descending line). The `OverrideFontScalingAsFallbackOnes` feature *tries* to bring those information from the fallback font (yours) to the base one (game) so that it aligns well on your font instead of the base ones. Use it only if necessary. In general, if your language barely uses Latin characters then you might want to turn it on. Otherwise you wouldn't want to.

## Language-specific features

* `KoreanWordWrapping`: For Korean language. Apply `<nobr>` wrapping on every single Korean word to properly implement a proper line breaking.
* `KoreanPostpositionTransformation`: For Korean language. Apply postposition transform rules (e.g., 은 <-> 는, 을 <-> 를, 이 <-> 가) to the text. NOTE: this is a performance-heavy feature with reported frame drop.



