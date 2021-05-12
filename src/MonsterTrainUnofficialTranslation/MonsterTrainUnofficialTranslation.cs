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

        JObject languageSetting = null;
        Dictionary<string, TMPro.TMP_FontAsset> fontReplacementMapping = new Dictionary<string, TMPro.TMP_FontAsset>();
        List<string> unknownEncounteredFonts = new List<string>();

        [Flags]
        enum OptionalFeatures
        {
            None = 0,
            OverrideFontScalingAsFallbackOnes = 1,
            KoreanWordWrapping = 2,
        };
        OptionalFeatures optionalFeatures = OptionalFeatures.None;

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

            if (configLanguage.Value == "" || configLanguage.Value == DEFAULTLANGUAGE)
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

                string optionalFeaturesStr = languageSetting["OptionalFeatures"].ToString();
                if (optionalFeaturesStr != null)
                {
                    if (!Enum.TryParse(optionalFeaturesStr, out optionalFeatures))
                    {
                        Logger.LogWarning($"Unknown OptionalFeatures: {optionalFeaturesStr}.");
                    }
                    else
                    {
                        Logger.LogInfo($"OptionalFeatures: {optionalFeatures}.");
                    }
                }
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
        }

        void Awake()
        {
            Instance = this;

            ReadLanguageSetting();
            LoadFonts();

            var harmony = new HarmonyLib.Harmony("com.nedsociety.monstertrainunofficialtranslation");
            harmony.PatchAll();
        }

        // From https://stackoverflow.com/a/28155130/3567518
        List<int> ToCodePoints(string str)
        {
            var codePoints = new List<int>(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                codePoints.Add(Char.ConvertToUtf32(str, i));
                if (Char.IsHighSurrogate(str[i]))
                    i += 1;
            }

            return codePoints;
        }


        string FixKoreanWordWrapping(string text)
        {
            // Since the game uses old TextMeshPro 1.4.0, it does not incorporate a proper Korean wordwrapping feature introduced in 1.5.0.
            // This code will simulate it as much as possible.

            var segments = new List<string>();

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
                        segments.Add("<nobr>");
                        isNoBrOpened = true;
                    }
                }
                else
                {
                    if (isNoBrOpened)
                    {
                        segments.Add("</nobr>");
                        isNoBrOpened = false;
                    }
                }
                segments.Add(char.ConvertFromUtf32(i));
            }

            if (isNoBrOpened)
            {
                segments.Add("</nobr>");
            }

            return string.Join("", segments);
        }

        List<IDictionary<String, Object>> ReadWeblateCsvData(string path)
        {
            var ret = new List<IDictionary<String, Object>>();

            using (var streamreader = new StreamReader(path))
            using (var csv = new CsvHelper.CsvReader(streamreader, System.Globalization.CultureInfo.InvariantCulture))
            {
                foreach (var record in csv.GetRecords<dynamic>())
                {
                    string target = record.target as string;
                    
                    // By default Weblate leaves untranslated entries as an empty string.
                    // Those entries must be removed to make sure they don't get merged.
                    if (string.IsNullOrEmpty(target))
                    {
                        continue;
                    }

                    if (optionalFeatures.HasFlag(OptionalFeatures.KoreanWordWrapping))
                    {
                        target = FixKoreanWordWrapping(target);
                    }

                    dynamic entry = new ExpandoObject();
                    var entryAsIDict = entry as IDictionary<String, Object>;

                    entryAsIDict["Key"] = record.source;
                    entryAsIDict["English [en-US]"] = target;
                    
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

                if (optionalFeatures.HasFlag(OptionalFeatures.OverrideFontScalingAsFallbackOnes))
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
