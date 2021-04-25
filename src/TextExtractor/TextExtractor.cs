using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TextExtractor
{
    [BepInEx.BepInPlugin("com.nedsociety.textextractor", "TextExtractor", "1.0.0.0")]
    public class TextExtractor : BepInEx.BaseUnityPlugin
    {
        public static TextExtractor Instance { get; private set; }
        bool extractAttempt = false;

        void Awake()
        {
            Instance = this;

            var harmony = new HarmonyLib.Harmony("com.nedsociety.textextractor");
            harmony.PatchAll();
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

        List<IDictionary<String, Object>> ReadI2LocCsvData(List<I2.Loc.LanguageSourceData> sources)
        {
            var ret = new List<IDictionary<String, Object>>();
            foreach (var src in sources)
            {
                using (var streamreader = StringToStreamReader(src.Export_CSV(null)))
                using (var csv = new CsvHelper.CsvReader(streamreader, System.Globalization.CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>();
                    ret.AddRange(records.Cast<IDictionary<String, Object>>());
                }
            }
            return ret;
        }

        void WriteWeblateCsvData(List<IDictionary<String, Object>> data, string path)
        {
            var weblatedata = new List<dynamic>();

            foreach (var entry in data)
            {
                dynamic record = new System.Dynamic.ExpandoObject();
                record.location = entry["Key"];
                record.source = entry["Key"];
                record.target = entry["English [en-US]"];
                record.ID = entry["Key"];
                record.fuzzy = "";
                record.context = "";
                record.translator_comments = "";

                string comment = entry["Descriptions"] as string;
                if (!string.IsNullOrEmpty(entry["Plural"] as string))
                {
                    comment = $"(Plural form: '{entry["Plural"]}')\n{comment}";
                }
                if (!string.IsNullOrEmpty(entry["Group"] as string))
                {
                    comment = $"(Group: '{entry["Group"]}')\n{comment}";
                }

                record.developer_comments = comment.Trim();
                weblatedata.Add(record);
            }

            var configuration = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                // Weblate's CSV parser relies on Python's csv.Sniffer which is incredibly unhelping. This is an ad-hoc workaround of that.
                ShouldQuote = _ => true
            };

            using (var streamwriter = new StreamWriter(path))
            using (var csv = new CsvHelper.CsvWriter(streamwriter, configuration))
            {
                csv.WriteRecords(weblatedata);
            }
        }
        public void OnLocalizationLoaded()
        {
            if (extractAttempt)
                return;
            extractAttempt = true;

            var sources = I2.Loc.LocalizationManager.Sources;
            if (sources.Count == 0)
            {
                Logger.LogError(
                    "The game didn't seem to load any language sources -- this might be caused from game update. Text extraction canceled."
                );
                return;
            }

            var dumppath = Path.Combine(
                // This file is placed by PostBuildEvent, containing the location to the locale directory
                File.ReadAllText(Path.Combine(Path.GetDirectoryName(Info.Location), "dumppath")).Trim(),
                $"en.csv"
            );

            var localizationData = ReadI2LocCsvData(sources);
            Logger.LogInfo($"Got {localizationData.Count} strings.");
            WriteWeblateCsvData(localizationData, dumppath);

            Logger.LogInfo("Text extraction done!");
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(LocalizationUtil))]
    [HarmonyLib.HarmonyPatch("InitLanguageSources")]
    public static class Harmony_LocalizationUtil_InitLanguageSources
    {
        static void Postfix()
        {
            TextExtractor.Instance.OnLocalizationLoaded();
        }
    }
}



