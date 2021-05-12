using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Dynamic;
using JObject = Newtonsoft.Json.Linq.JObject;
using JToken = Newtonsoft.Json.Linq.JToken;

namespace TextExtractor
{
    [BepInEx.BepInPlugin("com.nedsociety.monstertrainunofficialtranslation", "MonsterTrainUnofficialTranslation", "1.0.0.0")]
    public class MonsterTrainUnofficialTranslation : BepInEx.BaseUnityPlugin
    {
        public static MonsterTrainUnofficialTranslation Instance { get; private set; }
        bool patchAttempt = false;
        
        List<string> unknownEncounteredFonts = new List<string>();
        Dictionary<string, TMPro.TMP_FontAsset> fontReplacementMapping = new Dictionary<string, TMPro.TMP_FontAsset>();

        JObject languageSetting = null;
        bool fontAdjustFaceInfo = false;

        const string DEFAULTLANGUAGE = "[Default (English)]";

        void ReadLanguageSetting()
        {
            var path = Path.Combine(Path.GetDirectoryName(Info.Location), "locale", "languages.json");

            JObject languageMap;
            using (StreamReader streamreader = new StreamReader(path))
            {
                Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(streamreader);
                Newtonsoft.Json.JsonSerializer se = new Newtonsoft.Json.JsonSerializer();
                languageMap = se.Deserialize(reader) as JObject;
                if (languageMap == null)
                {
                    Logger.LogError($"Failed to read {path}.");

                    // Leave as empty
                    languageMap = new JObject();
                }
            }

            var acceptableLanguageOptions = new List<string> { DEFAULTLANGUAGE };
            acceptableLanguageOptions.AddRange((languageMap as IDictionary<string, JToken>).Keys);

            var configLanguage = Config.Bind(
                "General", "Language", DEFAULTLANGUAGE,
                new BepInEx.Configuration.ConfigDescription(
                    "A language to replace English. Changes to this option applies when the game restarts.",
                    new BepInEx.Configuration.AcceptableValueList<string>(acceptableLanguageOptions.ToArray())
                )
            );

            if (configLanguage.Value == "")
            {
                Logger.LogInfo("No translation language set.");
            }
            else if (!languageMap.ContainsKey(configLanguage.Value))
            {
                Logger.LogError($"Unknown language '{configLanguage.Value}' set.");
            }
            else
            {
                languageSetting = languageMap[configLanguage.Value] as JObject;
            }
        }
        void LoadFonts()
        {
            if (languageSetting == null)
                return;

            string fontAssetBundleFilename = languageSetting["FontAssetBundle"].ToString();
            JObject fontFallbacks = languageSetting["FontFallbacks"] as JObject;

            if (fontAssetBundleFilename == null || fontFallbacks == null)
            {
                Logger.LogInfo("No font replacement has been configured for this language.");
                return;
            }

            var path = Path.Combine(Path.GetDirectoryName(Info.Location), "locale", fontAssetBundleFilename);
            UnityEngine.AssetBundle assetBundle = UnityEngine.AssetBundle.LoadFromFile(path);
            foreach (var entry in fontFallbacks)
            {
                TMPro.TMP_FontAsset fontAsset = assetBundle.LoadAsset<TMPro.TMP_FontAsset>(entry.Value.ToString());
                if (fontAsset == null)
                {
                    Logger.LogError($"Font {entry.Value.ToString()} not found in the bundle.");
                    continue;
                }

                fontReplacementMapping[entry.Key] = fontAsset;
            }

            fontAdjustFaceInfo = languageSetting.Value<bool>("FontAdjustFaceInfo") || false;
        }

        void Awake()
        {
            Instance = this;

            ReadLanguageSetting();
            LoadFonts();

            var harmony = new HarmonyLib.Harmony("com.nedsociety.monstertrainunofficialtranslation");
            harmony.PatchAll();
        }

        List<IDictionary<String, Object>> ReadWeblateCsvData(string path)
        {
            var ret = new List<IDictionary<String, Object>>();

            using (var streamreader = new StreamReader(path))
            using (var csv = new CsvHelper.CsvReader(streamreader, System.Globalization.CultureInfo.InvariantCulture))
            {
                foreach (var record in csv.GetRecords<dynamic>())
                {
                    // By default Weblate leaves untranslated entries as an empty string.
                    // Those entries must be removed to make sure they don't get merged.
                    if (string.IsNullOrEmpty(record.target as string))
                    {
                        continue;
                    }

                    dynamic entry = new ExpandoObject();
                    var entryAsIDict = entry as IDictionary<String, Object>;

                    entryAsIDict["Key"] = record.source;
                    entryAsIDict["English [en-US]"] = record.target;
                    
                    ret.Add(entry);
                }
            }

            return ret;
        }

        string StringizeCSV(List<IDictionary<String, Object>> data)
        {
            using (var streamwriter = new StringWriter())
            using (var csv = new CsvHelper.CsvWriter(streamwriter, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(data.Cast<dynamic>());
                return streamwriter.ToString();
            }
        }

        public void OnLocalizationLoaded()
        {
            if (patchAttempt)
                return;
            patchAttempt = true;

            var sources = I2.Loc.LocalizationManager.Sources;
            if (sources.Count == 0)
            {
                Logger.LogError(
                    "The game didn't seem to load any language sources -- this might be caused from game update. Patch canceled."
                );
                return;
            }

            if (languageSetting == null)
                return;

            var path = Path.Combine(Path.GetDirectoryName(Info.Location), "locale", languageSetting["Texts"].ToString());
            var data = ReadWeblateCsvData(path);

            foreach (var src in sources)
            {
                src.Import_CSV(null, StringizeCSV(data), I2.Loc.eSpreadsheetUpdateMode.Merge);
            }

            Logger.LogInfo("Text patching done!");
        }

        UnityEngine.TextCore.FaceInfo AdjustFaceInfo(UnityEngine.TextCore.FaceInfo orig, UnityEngine.TextCore.FaceInfo repl)
        {
            if (orig.pointSize == 0.0f)
            {
                Logger.LogWarning($"0 pointSize");
            }
            // See http://digitalnativestudios.com/textmeshpro/docs/font/
            float scalingFactor = ((float)orig.pointSize) / repl.pointSize;

            UnityEngine.TextCore.FaceInfo ret = orig;
            ret.scale = repl.scale;
            ret.lineHeight = repl.lineHeight * scalingFactor;
            ret.ascentLine = repl.ascentLine * scalingFactor;
            ret.capLine = repl.capLine * scalingFactor;
            ret.meanLine = repl.meanLine * scalingFactor;
            ret.baseline = repl.baseline * scalingFactor;
            ret.descentLine = repl.descentLine * scalingFactor;
            ret.underlineOffset = repl.underlineOffset * scalingFactor;
            ret.underlineThickness = repl.underlineThickness * scalingFactor;
            ret.strikethroughOffset = repl.strikethroughOffset * scalingFactor;
            ret.strikethroughThickness = repl.strikethroughThickness * scalingFactor;
            ret.superscriptOffset = repl.superscriptOffset * scalingFactor;
            ret.superscriptSize = repl.superscriptSize * scalingFactor;
            ret.subscriptOffset = repl.subscriptOffset * scalingFactor;
            ret.subscriptSize = repl.subscriptSize * scalingFactor;
            ret.tabWidth = repl.tabWidth * scalingFactor;

            return ret;
        }

        public void OnBeforeGuiElementLoadsFontAsset(TMPro.TextMeshProUGUI element)
        {
            var fontAsset = typeof(TMPro.TextMeshProUGUI).GetField("m_fontAsset", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(element) as TMPro.TMP_FontAsset;
            if (fontAsset == null)
                return;

            if (languageSetting == null)
                return;

            TMPro.TMP_FontAsset replacement;
            if (fontReplacementMapping.TryGetValue(fontAsset.name, out replacement))
            {
                if (fontAsset.m_FallbackFontAssetTable == null)
                {
                    fontAsset.m_FallbackFontAssetTable = new List<TMPro.TMP_FontAsset>();
                }
                else if (fontAsset.m_FallbackFontAssetTable.Contains(replacement))
                {
                    return;
                }

                fontAsset.m_FallbackFontAssetTable.Insert(0, replacement);
                Logger.LogInfo($"{fontAsset.name} -> {replacement.name}");

                if (fontAdjustFaceInfo)
                {
                    var newFaceInfo = AdjustFaceInfo(fontAsset.faceInfo, replacement.faceInfo);
                    typeof(TMPro.TMP_FontAsset).GetField("m_FaceInfo", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fontAsset, newFaceInfo);
                }
            }
            else if (!unknownEncounteredFonts.Contains(fontAsset.name))
            {
                unknownEncounteredFonts.Add(fontAsset.name);
                Logger.LogWarning($"During the font patching an unknown font is encountered: '{fontAsset.name}'.");
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(LocalizationUtil))]
    [HarmonyLib.HarmonyPatch("InitLanguageSources")]
    public static class Harmony_LocalizationUtil_InitLanguageSources
    {
        static void Postfix()
        {
            MonsterTrainUnofficialTranslation.Instance.OnLocalizationLoaded();
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(TMPro.TextMeshProUGUI))]
    [HarmonyLib.HarmonyPatch("LoadFontAsset")]
    public static class Harmony_TMPro_TextMeshProUGUI_LoadFontAsset
    {
        static void Prefix(TMPro.TextMeshProUGUI __instance)
        {
            MonsterTrainUnofficialTranslation.Instance.OnBeforeGuiElementLoadsFontAsset(__instance);
        }
    }
}
