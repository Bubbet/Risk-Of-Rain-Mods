using System.Collections.Generic;
using RoR2;

namespace Titanfall2Mod
{
    public static class Tokens
    {
        public const string Prefix = "BUB_";
        
        [SystemInitializer(typeof(Language))]
        public static void Init()
        {
            MiscTokens();
            FromPreReg();
        }
        private static void MiscTokens()
        {
            Reg("Pilot Body Name", "Pilot Body");
            Reg("Pilot Body Subtitle", "Pilot Body Subtitle");

            Reg("Pilot Unlockable Name", "Pilot Unlockable");
            Reg("Pilot Name", "Pilot");
            Reg("Pilot Desc", "Pilot from titanfall2");
            Reg("Pilot Outro Flavor", "Nice work out there.");
            Reg("Pilot Ending Flavor", "Excellent job.");

            Reg("ENTER_TITAN", "Enter Titan");
            Reg("EXIT_TITAN", "Exit Titan");
            
            Reg("TITAN_SKILL", "Titan");
            Reg("TITAN_KIT_ION", "Ion Kit");
            Reg("TITAN_KIT_SCORCH", "Scorch Kit");
            Reg("TITAN_KIT_NORTHSTAR", "Northstar Kit");
            Reg("TITAN_KIT_RONIN", "Ronin Kit");
            Reg("TITAN_KIT_TONE", "Tone Kit");
            Reg("TITAN_KIT_LEGION", "Legion Kit");
            Reg("TITAN_KIT_MONARCH", "Monarch Kit");
            Reg("TITAN_KIT_SKILL", "Titan Kit");
            
            Reg("MALE_PILOT", "Male");
            Reg("FEMALE_PILOT", "Female");
            Reg("PRIME_MALE_PILOT", "Prime Titan - Male Pilot");
            Reg("PRIME_FEMALE_PILOT", "Prime Titan - Female Pilot");
            
            Reg("RONIN_BROADSWORD_NAME", "Broadsword");
            Reg("RONIN_BROADSWORD_DESC", "Massive sword used to slice through enemies.");
            
            Reg("ADS_NAME", "Aim Down Sights");
            Reg("ADS_DESC", "Aim down the sights of your weapon for increased accuracy.");
            
            Reg("NOT_IMPLEMENTED", "Not yet implemented!");
            Reg("MASTERY_NAME", "Mastery Skin");
        }
        
        public static void Reg(string key, string val)
        {
            //LanguageAPI.Add(Prefix + key.Replace(" ", "_").ToUpper(), val);

            Language.english.SetStringByToken(Prefix + key.Replace(" ", "_").ToUpper(), val);
        }

             
        private static List<(string key, string value)> preReg = new List<(string key, string value)>();
        private static void FromPreReg()
        {
            foreach (var (key, value) in preReg)
            {
                Reg(key, value);
            }
        }
        public static void PreStartReg(string key, string val)
        {
            preReg.Add((key,val));
        }
    }
}