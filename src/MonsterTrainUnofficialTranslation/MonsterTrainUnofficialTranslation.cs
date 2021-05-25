using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Dynamic;
using JObject = Newtonsoft.Json.Linq.JObject;
using JToken = Newtonsoft.Json.Linq.JToken;

namespace MonsterTrainUnofficialTranslation
{
    [Flags]
    public enum OptionalFeatures
    {
        None = 0,
        OverrideFontScalingAsFallbackOnes = 1,
        KoreanWordWrapping = 2
    };

    [BepInEx.BepInPlugin("com.nedsociety.monstertrainunofficialtranslation", "MonsterTrainUnofficialTranslation", "1.0.0.0")]
    public class MonsterTrainUnofficialTranslation : BepInEx.BaseUnityPlugin
    {
        public static MonsterTrainUnofficialTranslation Instance { get; private set; }

        public TextPatcher TextPatcher { get; private set; }
        public FontPatcher FontPatcher { get; private set; }

        const string DEFAULTLANGUAGE = "[Default (English)]";
        const string BASELANGUAGETEXTDEF = "en.csv";

        IDictionary<string, JToken> ReadLanguageMap()
        {
            var path = Path.Combine(Path.GetDirectoryName(Info.Location), "locale", "languages.json");
            IDictionary<string, JToken> languageMap;
            using (StreamReader streamreader = new StreamReader(path))
            {
                Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(streamreader);
                Newtonsoft.Json.JsonSerializer se = new Newtonsoft.Json.JsonSerializer();
                languageMap = se.Deserialize(reader) as JObject;
                if (languageMap == null)
                    throw new IOException($"Failed to read {path}.");
            }

            return languageMap;
        }

        void SetupDummyPatchers()
        {
            TextPatcher = new TextPatcher(null, null, OptionalFeatures.None, null, Logger);
            FontPatcher = new FontPatcher(null, null, OptionalFeatures.None, Logger);
        }

        void SetupLanguagePatchers(JObject languageSetting)
        {
            OptionalFeatures optionalFeatures = OptionalFeatures.None;
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

            var textPath = Path.Combine(Path.GetDirectoryName(Info.Location), "locale", languageSetting["Texts"].ToString());
            var textPathBase = Path.Combine(Path.GetDirectoryName(Info.Location), "locale", BASELANGUAGETEXTDEF);
            var fontAssetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "locale", languageSetting["FontAssetBundle"].ToString());
            var fontFallbacks = languageSetting["FontFallbacks"].ToObject<Dictionary<string, string>>();
            var italicSpacing = languageSetting["ItalicSpacing"].ToString();

            TextPatcher = new TextPatcher(textPath, textPathBase, optionalFeatures, italicSpacing, Logger);
            FontPatcher = new FontPatcher(fontAssetBundlePath, fontFallbacks, optionalFeatures, Logger);
        }

        void Awake()
        {
            Instance = this;

            var languageMap = ReadLanguageMap();

            var acceptableLanguageOptions = new List<string> { DEFAULTLANGUAGE };
            acceptableLanguageOptions.AddRange(languageMap.Keys);

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
                SetupDummyPatchers();
                
            }
            else if (!languageMap.ContainsKey(configLanguage.Value))
            {
                Logger.LogError($"Unknown language '{configLanguage.Value}' set.");
                SetupDummyPatchers();
            }
            else
                SetupLanguagePatchers(languageMap[configLanguage.Value] as JObject);

            new HarmonyLib.Harmony("com.nedsociety.monstertrainunofficialtranslation").PatchAll();
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(LocalizationUtil))]
    [HarmonyLib.HarmonyPatch("InitLanguageSources")]
    public static class Harmony_LocalizationUtil_InitLanguageSources
    {
        static void Prefix()
        {
            MonsterTrainUnofficialTranslation.Instance.TextPatcher.OnLocalizationLoadCallBegin();
        }
        static void Postfix()
        {
            MonsterTrainUnofficialTranslation.Instance.TextPatcher.OnLocalizationLoadCallEnd();
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(TMPro.TextMeshProUGUI))]
    [HarmonyLib.HarmonyPatch("LoadFontAsset")]
    public static class Harmony_TMPro_TextMeshProUGUI_LoadFontAsset
    {
        static void Prefix(TMPro.TextMeshProUGUI __instance)
        {
            MonsterTrainUnofficialTranslation.Instance.FontPatcher.OnBeforeGuiElementLoadsFontAsset(__instance);
        }
    }
}
