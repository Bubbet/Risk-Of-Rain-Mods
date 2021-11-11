using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Phedg1Studios.StartingItemsGUI;
using RoR2;
using UnityEngine;

namespace StartingItemsGuiPatch
{
    [HarmonyPatch]
    public class SteamCloudItemsStorage
    {
        [HarmonyILManipulator, HarmonyPatch(typeof(Data), "SaveConfigProfile")]
        public static void SaveInject(ILContext il)
        {
            if (!StartingItemsGuiPatch.SteamCloudSaves.Value) return;
            
            var c = new ILCursor(il);
            c.GotoNext( MoveType.After,
                /*
                x => x.MatchLdsfld<Data>("configName"),
                x => x.MatchLdcI4(0),
                x => x.OpCode == OpCodes.Callvirt, //x.MatchCallvirt<List<string>>("get_Item"),
                x => x.MatchLdcI4(0),
                x => x.MatchNewobj<StreamWriter>(),
                x => x.MatchStloc(2)*/
                x => x.MatchLdsfld<Data>("configProfileName"),
                x => x.MatchLdcI4(0),
                x => x.OpCode == OpCodes.Callvirt,
                x => x.MatchLdsfld<Data>("userProfile"),
                x => x.MatchLdsfld<Data>("profileConfigFile"),
                x => x.MatchCall<string>("Concat"),
                x => x.MatchLdcI4(0),
                x => x.MatchNewobj<StreamWriter>()
            );
            c.Index -= 2;
            c.RemoveRange(2);
            c.EmitDelegate<Func<string, Stream>>(FillFile);
            
            c.GotoNext(x => x.MatchCallvirt<TextWriter>("Write"));
            c.RemoveRange(3);
            c.EmitDelegate<WriteAndCloseDele>(WriteAndClose);
            /*
            //var methodInfo = typeof(Encoding).GetProperty("UTF8")?.GetType().GetMethod("GetBytes", BindingFlags.Instance | BindingFlags.Public);
            //var methodInfo = Encoding.UTF8.GetType().GetMethod("GetBytes", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new []{typeof(string)}, new ParameterModifier[] {});
            var methodInfo = Encoding.UTF8.GetType().GetMethod("GetBytes", new[] {typeof(string)});
            Debug.Log(methodInfo);
            c.Emit(OpCodes.Callvirt, methodInfo);

            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Call, typeof(string).GetMethod("get_Length"));

            var writeMethodInfo = typeof(Stream).GetMethod("Write", new[] {typeof(byte[]), typeof(int), typeof(int)});
            Debug.Log(writeMethodInfo);
            c.Emit(OpCodes.Callvirt, writeMethodInfo);
            /*
            c.GotoNext(x => x.MatchCallvirt<TextWriter>("Close"));
            c.Remove();
            c.Emit(OpCodes.Callvirt, typeof(Stream).GetMethod("Close", BindingFlags.Public | BindingFlags.Instance));*/
        }

        private static void WriteAndClose(Stream stream, string input)
        {
            var b = Encoding.UTF8.GetBytes(input);
            stream.Write(b, 0, b.Length);
            stream.Close();
        }
        delegate void WriteAndCloseDele(Stream stream, string input);
        private static Stream FillFile(string oldPath)
        {
            var parts = oldPath.Split(new[] {"config"}, StringSplitOptions.RemoveEmptyEntries);
            //RoR2Application.cloudStorage / ;
            var ou = RoR2Application.cloudStorage.OpenFile(parts[1], FileMode.Create, FileAccess.Write);
            return ou;
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(Data), "ReadConfig")]
        public static void ReadConfigInject(ILContext il)
        {
            if (!StartingItemsGuiPatch.SteamCloudSaves.Value) return;

            var c = new ILCursor(il);

            var givenPath = -1;
            var givenSuffix = -1;
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out givenPath),
                x => x.MatchLdarg(out givenSuffix),
                x => x.MatchCall<string>("Concat"),
                x => x.OpCode == OpCodes.Call,
                x => x.MatchBrfalse(out _)
            );
            c.Index--;
            c.Emit(OpCodes.Ldloc, givenPath);
            c.Emit(OpCodes.Ldarg, givenSuffix);
            c.EmitDelegate<Func<string, string, bool>>(CloudFileExists);
            c.Emit(OpCodes.Or);

            int lindex = -1;
            c.GotoNext(
                x => x.MatchStloc(out lindex),
                x => x.MatchLdloc(out _),
                x => x.MatchLdarg(out _),
                x => x.MatchCall<string>("Concat"),
                x => x.OpCode == OpCodes.Newobj,
                x => x.MatchStloc(out _),
                x => x.MatchBr(out _)
            );
            c.Index += 3;
            c.RemoveRange(4);
            c.Emit(OpCodes.Ldloc, lindex);
            c.EmitDelegate<ReadAndCloseDele>(ReadAndClose);
            c.Index++;
            var start = c.Index;
            // From 27-32
            /*
            c.GotoNext( // From 37-40
                x => x.MatchLdloc(out _),
                x => x.MatchCallvirt<TextReader>("Peek"),
                x => x.MatchLdcI4(out _),
                x => x.MatchBge(out _)
            );
            c.RemoveRange(4);

            c.GotoPrev( // From 33-36
                x => x.MatchLdloc(out _),
                x => x.MatchLdloc(out _),
                x => x.MatchCallvirt<TextReader>("ReadLine"),
                x => x.OpCode == OpCodes.Callvirt //x.MatchCallvirt<List<string>>("Add")
            );
            c.RemoveRange(4);
            
            */
            c.GotoNext( MoveType.After, // From 41-42
                x => x.MatchLdloc(out _),
                x => x.MatchCallvirt<TextReader>("Close")
            );
            var end = c.Index;
            c.Index = start;
            c.RemoveRange(end-start);
        }

        private static bool CloudFileExists(string givenPath, string givenSuffix)
        {
            var parts = givenPath.Split(new[] {"config"}, StringSplitOptions.RemoveEmptyEntries);
            var exists = RoR2Application.cloudStorage.FileExists(parts[1] + givenSuffix);
            return exists;
        }

        delegate void ReadAndCloseDele(string text, string givenSuffix, List<string> list);

        private static void ReadAndClose(string text, string givenSuffix, List<string> list)
        {
            if (givenSuffix != "") 
            {
                // TODO read file line by line and emit to list
                
                var parts = text.Split(new[] {"config"}, StringSplitOptions.RemoveEmptyEntries);
                //RoR2Application.cloudStorage / ;
                var ou = RoR2Application.cloudStorage.OpenFile(parts[1] + givenSuffix, FileMode.Open, FileAccess.Read);
                if (ou == null) goto Failed;
                var bytes = new List<byte>();
                while (true)
                {
                    var by = ou.ReadByte();
                    if (by == -1) break;
                    bytes.Add((byte) by);
                }
                ou.Close();

                if (bytes.Count == 0)
                    goto Failed;

                var str = Encoding.UTF8.GetString(bytes.ToArray());
                var lines = str.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
                list.AddRange(lines);

                return;
            }
            
            Failed:
            // Do old code for Config reading
            if (!File.Exists(text + givenSuffix)) return;
            StreamReader streamReader = new StreamReader(text + givenSuffix);
            while (streamReader.Peek() >= 0)
            {
                list.Add(streamReader.ReadLine());
            }
            streamReader.Close();
        }
    }
}