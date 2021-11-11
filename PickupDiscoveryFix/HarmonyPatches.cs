using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RoR2;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Zio;

namespace PickupDiscoveryFix
{
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        private static string _currentFile;
        public static readonly Dictionary<string, string[]> DiscoveredPickups = new Dictionary<string, string[]>();

        //delegate void instanceDele(UserProfile.SaveFieldAttribute o);
        //private static UserProfile.SaveFieldAttribute attributeInstance;
        delegate void fieldDele(FieldInfo field);
        private static FieldInfo fieldInstance;

        [HarmonyPatch(typeof(UserProfile.SaveFieldAttribute), nameof(UserProfile.SaveFieldAttribute.SetupPickupsSet)), HarmonyILManipulator]
        public static void SetupPickupsPatch(ILContext il)
        {
            var c = new ILCursor(il);

            //c.Emit(OpCodes.Ldarg_0);
            //c.EmitDelegate<instanceDele>(attribute => attributeInstance = attribute);

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<fieldDele>(field => fieldInstance = field);

            c.GotoNext(MoveType.After,
                x=> x.MatchLdarg(0),
                x => x.MatchLdloc(0),
                x => x.GetFtn(out Getterdele));
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldftn, typeof(HarmonyPatches).GetMethod("GetterMeth", BindingFlags.Static | BindingFlags.Public));
            
            c.GotoNext(MoveType.After,
                x=> x.MatchLdarg(0),
                x => x.MatchLdloc(0),
                x => x.GetFtn(out Setterdele));
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldftn, typeof(HarmonyPatches).GetMethod("SetterMeth", BindingFlags.Static | BindingFlags.Public));
            
            //PickupDiscoveryFixPlugin.Log.LogInfo(il);
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(UserProfile), "LoadUserProfileFromDisk")]
        public static void LoadFromDiskHook(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(out _),
                x => x.MatchCall<XDocument>("Load"),
                x => true, //x.OpCode == OpCodes.Call && x.Operand.GetType().Name == "FromXml",
                x => x.MatchStloc(out _)
            );
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<SetCurrentDele>(path => _currentFile = path.GetNameWithoutExtension());
        }

        delegate void SetCurrentDele(UPath path);
        public static bool GetFtn(this Instruction x, out MethodReference ftn)
        {
            var isfunc = x.OpCode == OpCodes.Ldftn;
            if (isfunc)
                ftn = (MethodReference) x.Operand;
            else
                ftn = null;
            return isfunc;
        }

        private static MethodReference Getterdele;
        public static string GetterMeth(UserProfile.SaveFieldAttribute attribute, UserProfile profile)
        {
            var old = (string) Getterdele.ResolveReflection().Invoke(attribute, new object[] {profile}); // Call old method
            if (profile.fileName == null) return old;
            if (!DiscoveredPickups.ContainsKey(profile.fileName)) // catch key not present in dict 
            {
                PickupDiscoveryFixPlugin.Log.LogWarning("Key not present in dict for profile " + profile.name + " " + profile.fileName);
                return old;
            }

            var oldList = old.Split(' ');
            var newList = DiscoveredPickups[profile.fileName];
            var combined = oldList.Union(newList).Distinct();
            var ret = string.Join(" ", combined);
            //TODO save ret to disk using profile.fileName
            
            RoR2Application.cloudStorage.CreateDirectory("/PickupDiscoveryFix");
            var stream = RoR2Application.cloudStorage.OpenFile("/PickupDiscoveryFix/" + profile.fileName, FileMode.Create, FileAccess.Write);
            var by = Encoding.UTF8.GetBytes(ret);
            stream.Write(by, 0, by.Length);
            stream.Close();
            
            return ret; // this is to be saved to disk
        }

        private static MethodReference Setterdele;

        public static string SetterMeth(UserProfile.SaveFieldAttribute attribute, UserProfile profile, string value) // FromXml aka reading the file
        {
            //TODO load disk and fill mineIn
            var stream = RoR2Application.cloudStorage.OpenFile("/PickupDiscoveryFix/" + _currentFile, FileMode.Open, FileAccess.Read);
            string[] mineIn = {};
            if (stream != null)
            {
                var byt = new List<byte>();
                while (true)
                {
                    var by = stream.ReadByte();
                    if (by == -1) break;
                    byt.Add((byte) by);
                }

                stream.Close();
                mineIn = Encoding.UTF8.GetString(byt.ToArray()).Split(' ');
            }

            var xmlIn = value.Split(' ');
            var ou = xmlIn.Union(mineIn).Distinct().ToArray(); // TODO combine with loaded file from disk
            DiscoveredPickups[_currentFile] = ou;
            return (string) Setterdele.ResolveReflection().Invoke(attribute, new object[] {profile, string.Join(" ", ou)});
        }
    }
}