using System.Collections.Generic;

namespace BubbetsItems.Helpers
{
    public static class StringExtensions
    {
        public static readonly Dictionary<StyleEnum, string> styleStrings = new Dictionary<StyleEnum, string>()
        {
            [StyleEnum.Damage] = "<style=cIsDamage>",
            [StyleEnum.Heal] = "<style=cIsHealing>",
            [StyleEnum.Utility] = "<style=cIsUtility>",
            [StyleEnum.Stack] = "<style=cStack>",
            [StyleEnum.Health] = "<style=cIsHealth>",
            [StyleEnum.Void] = "<style=cIsVoid>",
            [StyleEnum.Mono] = "<style=cMono>",
            [StyleEnum.Death] = "<style=cDeath>",
            [StyleEnum.UserSetting] = "<style=cUserSetting>",
            [StyleEnum.Artifact] = "<style=cArtifact>",
            [StyleEnum.Sub] = "<style=cSub>",
            [StyleEnum.Event] = "<style=cEvent>",
            [StyleEnum.WorldEvent] = "<style=cWorldEvent>",
            [StyleEnum.KeywordName] = "<style=cKeywordName>",
            [StyleEnum.Shrine] = "<style=cShrine>",
            [StyleEnum.Red] = "<color=#FF0000>",
            [StyleEnum.Orange] = "<color=#FF7C00>",
            [StyleEnum.Yellow] = "<color=#FFFF00>",
            [StyleEnum.Green] = "<color=#00FF00>",
            [StyleEnum.Blue] = "<color=#0000FF>",
            [StyleEnum.Purple] = "<color=#7C00FF>",
            [StyleEnum.Cyan] = "<color=#00FFFF>",
            [StyleEnum.Magenta] = "<color=#FF00FF>",
            [StyleEnum.White] = "<color=#FFFFFF>",
            [StyleEnum.Black] = "<color=#000000>",
            [StyleEnum.WhiteItem] = "<color=#FFFFFF>",
            [StyleEnum.GreenItem] = "<color=#77FF17>",
            [StyleEnum.RedItem] =  "<color=#E7543A>",
            [StyleEnum.BossItem] = "<color=#FFEB04>",
            [StyleEnum.LunarItem] = "<color=#307FFF>",
            [StyleEnum.VoidItem] = "<color=#ED7FCD>",
            [StyleEnum.VoidLunar] = "<color=#8600CB>",
        };

        public static bool IsStyle(StyleEnum style) => styleStrings[style].StartsWith("<style="); //(int) style <= 6;

        public const string CloseStyle = "</style>";
        public const string CloseColor = "</color>";

        public static string Style(StyleEnum style, string contents)
        {
            var close = IsStyle(style) ? CloseStyle : CloseColor;
            return styleStrings[style] + contents + close;
        }

        public static string Style(this string contents, StyleEnum style)
        {
            return Style(style, contents);
        }
        
        public static string Sub(this string contents, int start, int end)
        {
            if (start < 0) start = contents.Length + start;
            if (end < 0) end = contents.Length + end;
            
            return contents.Substring(start, end - start);
        }

        public static string Sub(this string contents, int end)
        {
            var start = end;
            if (end < 0) start = contents.Length + start;
            return contents.Substring(start);
        }
    }

    public enum StyleEnum
    {
        Damage,
        Heal,
        Utility,
        Stack,
        Health,
        Void,
        Mono,
        Death,
        UserSetting,
        Artifact,
        Sub,
        Event,
        WorldEvent, 
        KeywordName,
        Shrine,
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
        Cyan,
        Magenta,
        White,
        Black,
        WhiteItem, 
        GreenItem, 
        RedItem, 
        BossItem,
        LunarItem, 
        VoidItem,
        VoidLunar
    }
}