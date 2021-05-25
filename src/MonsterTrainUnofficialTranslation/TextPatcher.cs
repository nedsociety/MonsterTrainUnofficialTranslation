using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MonsterTrainUnofficialTranslation
{
    public class TextPatcher
    {
        BepInEx.Logging.ManualLogSource Logger;
        bool active;
        
        OrderedDictionary textDataBase;
        OrderedDictionary textDataTranslated;
        OptionalFeatures optionalFeatures;
        string italicSpacing = null;

        int localizationLoadCallFrameCount = 0;
        bool done = false;

        int outdatedBaseEntryCount = 0;
        int missingBaseEntryCount = 0;
        int missingTranslationEntryCount = 0;

        public TextPatcher(string textPathTranslated, string textPathBase, OptionalFeatures optionalFeatures, string italicSpacing, BepInEx.Logging.ManualLogSource logger)
        {
            this.optionalFeatures = optionalFeatures;
            this.italicSpacing = italicSpacing;
            Logger = logger;
            active = !string.IsNullOrWhiteSpace(textPathTranslated);

            if (!active)
            {
                Logger.LogInfo("No text has been configured for current language. Disabled translation.");
                return;
            }

            textDataBase = ReadWeblateCsvData(textPathBase, false, optionalFeatures);
            textDataTranslated = ReadWeblateCsvData(textPathTranslated, true, optionalFeatures);
        }

        // From https://stackoverflow.com/a/28155130/3567518
        List<int> ToCodePoints(string str)
        {
            var codePoints = new List<int>(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                codePoints.Add(char.ConvertToUtf32(str, i));
                if (char.IsHighSurrogate(str[i]))
                    i += 1;
            }

            return codePoints;
        }

        string FixKoreanWordWrapping(string text)
        {
            // Since the game uses old TextMeshPro 1.4.0, it does not incorporate a proper Korean wordwrapping feature introduced in 1.5.0.
            // This code will simulate it as much as possible.

            var builder = new StringBuilder();

            bool isNoBrOpened = false;
            foreach (var i in ToCodePoints(text))
            {
                // See https://stackoverflow.com/a/56314869/3567518
                if (
                    ((0x1100 <= i) && (i <= 0x11FF))
                    || ((0x3001 <= i) && (i <= 0x3003))
                    || ((0x3008 <= i) && (i <= 0x3011))
                    || ((0x3013 <= i) && (i <= 0x301F))
                    || ((0x302E <= i) && (i <= 0x3030))
                    || ((0x3037 <= i) && (i <= 0x3037))
                    || ((0x30FB <= i) && (i <= 0x30FB))
                    || ((0x3131 <= i) && (i <= 0x318E))
                    || ((0x3200 <= i) && (i <= 0x321E))
                    || ((0x3260 <= i) && (i <= 0x327E))
                    || ((0xA960 <= i) && (i <= 0xA97C))
                    || ((0xAC00 <= i) && (i <= 0xD7A3))
                    || ((0xD7B0 <= i) && (i <= 0xD7C6))
                    || ((0xD7CB <= i) && (i <= 0xD7FB))
                    || ((0xFE45 <= i) && (i <= 0xFE46))
                    || ((0xFF61 <= i) && (i <= 0xFF65))
                    || ((0xFFA0 <= i) && (i <= 0xFFBE))
                    || ((0xFFC2 <= i) && (i <= 0xFFC7))
                    || ((0xFFCA <= i) && (i <= 0xFFCF))
                    || ((0xFFD2 <= i) && (i <= 0xFFD7))
                    || ((0xFFDA <= i) && (i <= 0xFFDC))
                )
                {
                    if (!isNoBrOpened)
                    {
                        builder.Append("<nobr>");
                        isNoBrOpened = true;
                    }
                }
                else
                {
                    if (isNoBrOpened)
                    {
                        builder.Append("</nobr>");
                        isNoBrOpened = false;
                    }
                }
                builder.Append(char.ConvertFromUtf32(i));
            }

            if (isNoBrOpened)
                builder.Append("</nobr>");

            return builder.ToString();
        }
        
        Regex regexApplyItalicSpacing = new Regex(@"(?<!\<i\>)\</i\>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string ApplyItalicSpacing(string text)
        {
            // Since the game uses old TextMeshPro 1.4.0, it does not incorporate a proper Italic glyph adjustment feature introduced in 2.0.2.
            // (See https://forum.unity.com/threads/italics-is-too-italicized-how-to-customize.688924/#post-4610032)
            // This code will add a spacing at the end of italic string to compensate the intrusion.

            return regexApplyItalicSpacing.Replace(text, $"<space={italicSpacing}></i>");
        }

        OrderedDictionary ReadWeblateCsvData(string path, bool postprocess, OptionalFeatures optionalFeatures)
        {
            var ret = new OrderedDictionary();

            using (var streamreader = new StreamReader(path))
            using (var csv = new CsvHelper.CsvReader(streamreader, System.Globalization.CultureInfo.InvariantCulture))
            {
                foreach (var record in csv.GetRecords<dynamic>())
                {
                    string source = record.source as string;
                    string target = record.target as string;

                    if (string.IsNullOrEmpty(source))
                    {
                        Logger.LogWarning($"Empty source ID for string translation '{target}' -- is the text file corrupted?");
                        continue;
                    }

                    // By default Weblate leaves untranslated entries as an empty string.
                    // Those entries must be removed to make sure they don't get merged.
                    if (string.IsNullOrEmpty(target))
                        continue;

                    if (postprocess)
                    {
                        if (optionalFeatures.HasFlag(OptionalFeatures.KoreanWordWrapping))
                            target = FixKoreanWordWrapping(target);

                        if (!string.IsNullOrEmpty(italicSpacing))
                            target = ApplyItalicSpacing(target);
                    }

                    ret.Add(source, target);
                }
            }

            return ret;
        }

        StreamReader StringToStreamReader(string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return new StreamReader(stream);
        }

        OrderedDictionary ReadI2LocData(I2.Loc.LanguageSourceData src)
        {
            var ret = new OrderedDictionary();

            using (var streamreader = StringToStreamReader(src.Export_CSV(null)))
            using (var csv = new CsvHelper.CsvReader(streamreader, System.Globalization.CultureInfo.InvariantCulture))
            {
                foreach (var record in csv.GetRecords<dynamic>()) {
                    var dict = record as IDictionary<string, object>;
                    ret.Add(dict["Key"] as string, dict["English [en-US]"] as string);
                }
            }

            return ret;
        }

        void MergeI2LocData(I2.Loc.LanguageSourceData src, OrderedDictionary data)
        {
            var records = new List<dynamic>();
            foreach (DictionaryEntry kvp in data)
            {
                var record = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
                record["Key"] = kvp.Key;
                record["English [en-US]"] = kvp.Value;

                records.Add(record);
            }

            using (var streamwriter = new StringWriter())
            using (var csv = new CsvHelper.CsvWriter(streamwriter, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
                src.Import_CSV(null, streamwriter.ToString(), I2.Loc.eSpreadsheetUpdateMode.Merge);
            }
        }
        
        OrderedDictionary FilterTextData(OrderedDictionary source, OrderedDictionary baseLanguage, OrderedDictionary translated)
        {
            OrderedDictionary ret = new OrderedDictionary();
            foreach (DictionaryEntry kvp in source)
            {
                string sourceKey = kvp.Key as string;
                string sourceString = kvp.Value as string;

                if (!baseLanguage.Contains(sourceKey))
                {
                    missingBaseEntryCount += 1;
                    continue;
                }

                if (!sourceString.Equals(baseLanguage[sourceKey]))
                {
                    outdatedBaseEntryCount += 1;
                    continue;
                }

                if (!translated.Contains(sourceKey))
                {
                    missingTranslationEntryCount += 1;
                    continue;
                }

                ret.Add(sourceKey, translated[sourceKey]);
            }

            return ret;
        }

        void PatchSource(I2.Loc.LanguageSourceData src)
        {
            var textDataSource = ReadI2LocData(src);
            var textDataFiltered = FilterTextData(textDataSource, textDataBase, textDataTranslated);
            MergeI2LocData(src, textDataFiltered);
        }

        public void OnLocalizationLoadCallBegin()
        {
            // This function is pathologically reentrant. We count which depth of the callstack we're in,
            // to make sure that the patch must be done only when the outmost call is done cleanly.
            // Otherwise we're injecting texts mid-initialization, leading to some bugs (notably InkLoc).
            localizationLoadCallFrameCount += 1;
        }

        public void OnLocalizationLoadCallEnd()
        {
            localizationLoadCallFrameCount -= 1;

            if (localizationLoadCallFrameCount > 0)
                return;

            if (!active || done)
                return;
            done = true;

            var sources = I2.Loc.LocalizationManager.Sources;
            if (sources.Count == 0)
            {
                Logger.LogError(
                    "The game didn't seem to load any language sources. Patch canceled."
                );
                return;
            }

            foreach (var src in sources)
                PatchSource(src);

            if (missingBaseEntryCount > 0)
            {
                Logger.LogWarning(
                    $"Cannot find {missingBaseEntryCount} entries in the English text definition. This might be caused from the game update."
                );
            }
            if (outdatedBaseEntryCount > 0)
            {
                Logger.LogWarning(
                    $"Mismatch occured for {outdatedBaseEntryCount} entries in the English text definition. This might be caused from the game update. Outdated translations will not be applied."
                );
            }
            if (missingTranslationEntryCount > 0)
            {
                Logger.LogInfo(
                    $"{missingTranslationEntryCount} strings currently have their translation missing for this language."
                );
            }

            Logger.LogInfo("Text patching done!");
        }
    }
}
