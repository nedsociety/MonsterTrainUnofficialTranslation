using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;

namespace MonsterTrainUnofficialTranslation
{
    public class TextPatcher
    {
        OptionalFeatures optionalFeatures;
        BepInEx.Logging.ManualLogSource Logger;
        bool active;
        List<Dictionary<string, string>> textData;
        int localizationLoadCallFrameCount = 0;
        bool done = false;

        public TextPatcher(string textPath, OptionalFeatures optionalFeatures, BepInEx.Logging.ManualLogSource logger)
        {
            this.optionalFeatures = optionalFeatures;
            Logger = logger;
            active = !string.IsNullOrWhiteSpace(textPath);

            if (!active)
            {
                Logger.LogInfo("No text has been configured for current language. Disabled translation.");
                return;
            }

            textData = ReadWeblateCsvData(textPath);
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

        List<Dictionary<string, string>> ReadWeblateCsvData(string path)
        {
            var ret = new List<Dictionary<string, string>>();

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

                    if (optionalFeatures.HasFlag(OptionalFeatures.KoreanWordWrapping))
                        target = FixKoreanWordWrapping(target);

                    var entry = new Dictionary<string, string>();

                    entry["Key"] = source;
                    entry["English [en-US]"] = target;

                    ret.Add(entry);
                }
            }

            return ret;
        }

        dynamic DictToDynamic<K, V>(Dictionary<K, V> dict)
        {
            var ret = new System.Dynamic.ExpandoObject();
            foreach (var kvp in dict)
                (ret as IDictionary<string, object>)[kvp.Key.ToString()] = kvp.Value;
            
            return ret;
        }

        string StringizeCSV(List<Dictionary<string, string>> data)
        {
            using (var streamwriter = new StringWriter())
            using (var csv = new CsvHelper.CsvWriter(streamwriter, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(data.Select(x => DictToDynamic(x)));
                return streamwriter.ToString();
            }
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
                    "The game didn't seem to load any language sources -- this might be caused from game update. Patch canceled."
                );
                return;
            }

            string textDataAsCsvString = StringizeCSV(textData);
            foreach (var src in sources)
                src.Import_CSV(null, textDataAsCsvString, I2.Loc.eSpreadsheetUpdateMode.Merge);

            Logger.LogInfo("Text patching done!");
        }
    }
}
