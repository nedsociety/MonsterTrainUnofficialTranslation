using System.Collections.Generic;
using HarmonyLib;

namespace MonsterTrainUnofficialTranslation
{
    public class FontPatcher
    {
        bool active;
        Dictionary<string, TMPro.TMP_FontAsset> fontMapping = new Dictionary<string, TMPro.TMP_FontAsset>();
        OptionalFeatures optionalFeatures;
        BepInEx.Logging.ManualLogSource Logger;
        List<string> unhandledFonts = new List<string>();

        public FontPatcher(
            string fontAssetBundlePath, Dictionary<string, string> fontFallbacks,
            OptionalFeatures optionalFeatures, BepInEx.Logging.ManualLogSource logger
        )
        {
            this.optionalFeatures = optionalFeatures;
            Logger = logger;
            active = !(string.IsNullOrWhiteSpace(fontAssetBundlePath) || (fontFallbacks == null) || fontFallbacks.IsNullOrEmpty());

            if (!active)
            {
                Logger.LogInfo("No font fallback has been configured for this language.");
                return;
            }

            UnityEngine.AssetBundle assetBundle = UnityEngine.AssetBundle.LoadFromFile(fontAssetBundlePath);
            foreach (var entry in fontFallbacks)
            {
                TMPro.TMP_FontAsset fontAsset = assetBundle.LoadAsset<TMPro.TMP_FontAsset>(entry.Value);
                if (fontAsset == null)
                {
                    Logger.LogError($"Font {entry.Value} not found in the bundle.");
                    continue;
                }

                fontMapping[entry.Key] = fontAsset;
            }
        }

        UnityEngine.TextCore.FaceInfo AdjustFaceInfo(UnityEngine.TextCore.FaceInfo orig, UnityEngine.TextCore.FaceInfo repl)
        {
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
            if (!active)
                return;

            var fontAsset = Traverse.Create(element).Field("m_fontAsset").GetValue<TMPro.TMP_FontAsset>();
            if (fontAsset == null)
                return;

            TMPro.TMP_FontAsset fallback;
            if (fontMapping.TryGetValue(fontAsset.name, out fallback))
            {
                if (fontAsset.m_FallbackFontAssetTable == null)
                {
                    fontAsset.m_FallbackFontAssetTable = new List<TMPro.TMP_FontAsset>();
                }
                else if (fontAsset.m_FallbackFontAssetTable.Contains(fallback))
                {
                    return;
                }

                fontAsset.m_FallbackFontAssetTable.Insert(0, fallback);
                Logger.LogInfo($"{fontAsset.name} -> {fallback.name}");

                if (optionalFeatures.HasFlag(OptionalFeatures.OverrideFontScalingAsFallbackOnes))
                {
                    var newFaceInfo = AdjustFaceInfo(fontAsset.faceInfo, fallback.faceInfo);
                    Traverse.Create(fontAsset).Field("m_FaceInfo").SetValue(newFaceInfo);
                }
            }
            else if (!unhandledFonts.Contains(fontAsset.name))
            {
                unhandledFonts.Add(fontAsset.name);
                Logger.LogWarning($"During the font patching an unknown font is encountered: '{fontAsset.name}'.");
            }
        }
    }
}
