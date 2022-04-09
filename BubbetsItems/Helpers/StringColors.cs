﻿using System.Collections.Generic;

namespace BubbetsItems.Helpers
{
    public static class StringColors
    {
        public static readonly Dictionary<StyleEnum, string> StyleStrings = new Dictionary<StyleEnum, string>()
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
        };

        public static bool IsStyle(StyleEnum style) => StyleStrings[style].StartsWith("<style="); //(int) style <= 6;

        public const string CloseStyle = "</style>";
        public const string CloseColor = "</color>";

        public static string Style(StyleEnum style, string contents)
        {
            var close = IsStyle(style) ? CloseStyle : CloseColor;
            return StyleStrings[style] + contents + close;
        }

        public static string Style(this string contents, StyleEnum style)
        {
            return Style(style, contents);
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
        Black
    }
}