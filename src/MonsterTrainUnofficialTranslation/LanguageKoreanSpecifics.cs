using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MonsterTrainUnofficialTranslation
{
    static class PostpositionTransformer
    {
        public struct PostpositionEntry
        {
            public string withoutPlosive;
            public string withPlosive;

            // Most of the postposition rules are based on whether the last vowel has plosive or not.
            // There's an exception: (으)로(부터/서/써) family also checks if that plosive is ㄹ or not.

            public bool ignoreㄹAsPlosive;
        };

        public static List<PostpositionEntry> Entries = new List<PostpositionEntry>
        {
            new PostpositionEntry{ withoutPlosive = "는", withPlosive = "은", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "가", withPlosive = "이", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "를", withPlosive = "을", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "와", withPlosive = "과", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "야", withPlosive = "아", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "여", withPlosive = "이여", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "랑", withPlosive = "이랑", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "나", withPlosive = "이나", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "란", withPlosive = "이란", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "든", withPlosive = "이든", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "든가", withPlosive = "이든가", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "든지", withPlosive = "이든지", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "나마", withPlosive = "이나마", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "야", withPlosive = "이야", ignoreㄹAsPlosive = false },
            new PostpositionEntry{ withoutPlosive = "야말로", withPlosive = "이야말로", ignoreㄹAsPlosive = false },

            new PostpositionEntry{ withoutPlosive = "로", withPlosive = "으로", ignoreㄹAsPlosive = true },
            new PostpositionEntry{ withoutPlosive = "로서", withPlosive = "으로서", ignoreㄹAsPlosive = true },
            new PostpositionEntry{ withoutPlosive = "로써", withPlosive = "으로써", ignoreㄹAsPlosive = true },
            new PostpositionEntry{ withoutPlosive = "로부터", withPlosive = "으로부터", ignoreㄹAsPlosive = true },
        };

        static PostpositionEntry? FindPostposition(string postposition)
        {
            foreach (var entry in Entries)
            {
                if (postposition.Equals(entry.withoutPlosive) || postposition.Equals(entry.withPlosive))
                    return entry;
            }
            return null;
        }

        public static string Transform(string lastVowelOrWord, string postposition)
        {
            var entry = FindPostposition(postposition);
            if (entry == null)
                return postposition;

            var codepoints = LanguageKoreanSpecifics.ToCodePoints(lastVowelOrWord);
            if (codepoints.Count == 0)
                return postposition;

            int lastVowelCodepoint = codepoints[codepoints.Count - 1];

            if ((0xAC00 <= lastVowelCodepoint) && (lastVowelCodepoint <= 0xD7A3))
            {
                // 가-힣
                switch ((lastVowelCodepoint - 0xAC00) % 28)
                {
                    case 0: // No plosive
                        return entry.Value.withoutPlosive;
                    case 8: // ㄹ
                        return entry.Value.ignoreㄹAsPlosive ? entry.Value.withoutPlosive : entry.Value.withPlosive;
                    default: // Other plosives
                        return entry.Value.withPlosive;
                }
            }
            else if ((0x30 <= lastVowelCodepoint) && (lastVowelCodepoint <= 0x39))
            {
                // Numbers
                switch (lastVowelCodepoint - 0x30)
                {
                    case 0: // 영, 십, 백, 천, 만, 억, *조*, 경, *해*
                        // This is actually confusing as
                        // - the number is a multiple of 10^12 but not a multiple of 10^13 (~조),
                        // - and the number is a multiple of 10^20 but not a multiple of 10^24 (~해),
                        // are without plosives.
                        //
                        // There are also cases for 자, 구, 재 but they're not practically spoken at all in daily life.
                        //
                        // Anyway in most case it's usually justified to use any postposition in every day life even for 조 and 해,
                        // not to mention the game where such numbers don't appear at all.
                        return entry.Value.withPlosive;
                    case 1: // 일
                    case 7: // 칠
                    case 8: // 팔
                        return entry.Value.ignoreㄹAsPlosive ? entry.Value.withoutPlosive : entry.Value.withPlosive;
                    case 2: // 이
                    case 4: // 사
                    case 5: // 오
                    case 9: // 구
                        return entry.Value.withoutPlosive;
                    case 3: // 삼
                    case 6: // 육
                        return entry.Value.withPlosive;
                }
            }

            // Unknown otherwise
            return postposition;
        }
    };

    static class LanguageKoreanSpecifics
    {
        // From https://stackoverflow.com/a/28155130/3567518
        public static List<int> ToCodePoints(string str)
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

        public static string FixWordWrapping(string text)
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

        static Regex regexSpriteNameExtractor = new Regex((
            "\\<sprite .*?\\bname=\"(.+?)\".*?\\>"
        ), RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Dictionary<string, string> SpriteName = new Dictionary<string, string>
        {
            { "Attack", "공격력" },
            { "Capacity", "공간" },
            { "PyreHealth", "불씨 체력" },
            { "ChargedEchoes", "충전된 메아리수정" },
            { "Gold", "금화" },
            { "Ember", "잿불" },
            { "Health", "최대 체력" },
            { "PactShards", "서약 파편" },
            { "CorruptionSlot", "메아리수정 슬롯" },
            { "Xcost", "X 비용" },
        };

        static Regex regexPostposition = new Regex((
            @"(?<lastVowel>[^\<\>]|(?:\<sprite[^>]*\>))"
            + @"(?<interspersedNonvowels>(?:\*| |(?:\<(?!br\b)(?!sprite\b)[^>]+\>))*)"
            + @"\$(?<nobrCheck>\<nobr\>)?(?<postposition>.+?)(?:\</nobr\>)?\$"
        ), RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string TransformPostposition(string text)
        {
            // Here's the convention applied for Korean translations:
            // all Korean text whose postpositions are to be automatically transformed must be braced by $.
            //
            // Example:
            // "[armor] [effect0.status0.power] $을$ 부여합니다." -> "방어도 10 을 부여합니다." / "방어도 12 를 부여합니다."
            //
            // Otherwise it won't be transformed, just like:
            // "[consume]를 부여합니다." -> "소모를 부여합니다."

            return regexPostposition.Replace(
                text,
                delegate (Match m) {
                    string lastVowel = m.Groups["lastVowel"].Value;

                    // Sprite to word as read
                    Match match = regexSpriteNameExtractor.Match(lastVowel);
                    if (match.Success && !SpriteName.TryGetValue(match.Groups[1].Value, out lastVowel))
                        lastVowel = m.Groups["lastVowel"].Value;


                    string newPostposition = PostpositionTransformer.Transform(
                        lastVowel, m.Groups["postposition"].Value
                    );
                    if (match.Groups["nobrCheck"].Length > 0)
                        return $"{m.Groups["lastVowel"]}{m.Groups["interspersedNonvowels"]}<nobr>{newPostposition}</nobr>";
                    else
                        return $"{m.Groups["lastVowel"]}{m.Groups["interspersedNonvowels"]}{newPostposition}";
                }
            );
        }

        static Regex regexRawPostposition = new Regex((
            @"\$(?<postposition>.+?)\$"
        ), RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string DisablePostpositionTransform(string text)
        {
            // In case where KoreanDisablePostpositionTransformation is specified, we want to remove the brace ($$).
            return regexRawPostposition.Replace(text, "${postposition}");
        }
    }
}
