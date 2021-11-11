using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using EntityStates;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;
using Path = System.IO.Path;

namespace Titanfall2Mod
{
    public static class Assets
    {
        //The file name of your asset bundle
        internal const string AssetBundleName = "MainBundle";

        //Should be the same name as your SerializableContentPack in the asset bundle
        internal const string ContentPackName = "MainContentPack";

        //Name of the your soundbank file, if any.
        internal const string SoundBankName = "Titanfall2ModSounds"; //HenryBank

        public static AssetBundle mainAssetBundle;
        public static ContentPack mainContentPack;
        internal static SerializableContentPack serialContentPack;

        public static List<EffectDef> effectDefs = new List<EffectDef>();
        public static AssetBundle fastAssetBundle;
        public static uint bankID;

        internal static void Init()
        {
            LoadAssetBundle();
            //LoadSoundBank();
            ContentPackProvider.Initialize();
            ApplyShaders();
        }

        // Loads the AssetBundle, which includes the Content Pack.
        private static void LoadAssetBundle()
        {
            if (mainAssetBundle != null) return;
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(path, AssetBundleName));
            fastAssetBundle = AssetBundle.LoadFromFile(Path.Combine(path, "FastAssets"));
            LoadContentPack();
        }

        // Sorts out ContentPack related shenanigans.
        // Sets up variables for reference throughout the mod and initializes a new content pack based on the SerializableContentPack.
        private static void LoadContentPack()
        {
            serialContentPack = mainAssetBundle.LoadAsset<SerializableContentPack>(ContentPackName);
            mainContentPack = serialContentPack.CreateContentPack();
            SkillGenerator.Init();
            AddEntityStateTypes();
            //CreateEffectDefs();
            ContentPackProvider.contentPack = mainContentPack;
            Prefabs.Init();
        }


        // Loads the sound bank for any custom sounds.
        [SystemInitializer]
        public static void LoadSoundBank()
        {
            if (SoundBankName == "")
            {
                Debug.LogFormat("Titanfall2Mod: SoundBank name is blank. Skipping loading SoundBank.");
                return;
            }

            
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Stream stream = File.Open(Path.Combine(path, SoundBankName + ".bnk"), FileMode.Open);
            var array = new byte[stream.Length];
            stream.Read(array, 0, array.Length);

            //R2API.SoundAPI.SoundBanks.Add(array);
            
            
            Titanfall2ModPlugin.logger.LogInfo("length of sndbnk: " + array.Length);
            var mem = Marshal.AllocHGlobal(array.Length);
            Marshal.Copy(array, 0, mem, array.Length);
            var result = AkSoundEngine.LoadBank(mem, (uint) array.Length, out bankID);
            if (result != AKRESULT.AK_Success)
                Titanfall2ModPlugin.logger.LogError("SoundBank Load Failed: " + result);
            //*/

            //AkSoundEngine.AddBasePath(path);
            //AkBankManager.LoadBank(SoundBankName, false, false);

            /*
            foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Debug.Log("names: "+ manifestResourceName);
            }
            using (var manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("Titanfall2Mod.TitanFall2ModSounds.bnk"))//"TitanFall2Mod." + SoundBankName + ".bnk"))
            {
                var array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                R2API.SoundAPI.SoundBanks.Add(array);
            }
            //*/
        }

        // Gathers all GameObjects with VFXAttributes attached and creates an EffectDef for each one.
        // Without this, the Effect is unable to be spawned.
        // Any VFX elements must have a NetWorkIdentity, VFXAttributes and EffectComponent on the base in order to be usable.
        private static void CreateEffectDefs()
        {
            var assets = mainAssetBundle.LoadAllAssets<GameObject>();
            var effects = assets.Where(g => g.GetComponent<EffectComponent>()).ToList();
            foreach (var g in effects)
            {
                var def = new EffectDef();
                def.prefab = g;

                effectDefs.Add(def);
            }

            mainContentPack.effectDefs.Add(effectDefs.ToArray());
        }


        // Finds all Entity State Types within the mod and adds them to the content pack.
        // Saves fuss of having to add them manually. Credit to KingEnderBrine for this code.
        private static void AddEntityStateTypes()
        {
            mainContentPack.entityStateTypes.Add(Assembly.GetExecutingAssembly().GetTypes().Where
                (type => typeof(EntityState).IsAssignableFrom(type)).ToArray());

            foreach (var unlockableDef in mainContentPack.unlockableDefs)
            {
                (unlockableDef as LessRetardedUnlockable)?.Awake();
            }
        }

        internal static void ApplyShaders()
        {
            var materials = mainAssetBundle.LoadAllAssets<Material>();
            foreach (var material in materials)
                if (material.shader.name.StartsWith("StubbedShader"))
                    material.shader = Resources.Load<Shader>("shaders" + material.shader.name.Substring(13));
        }
    }

    public class ContentPackProvider : IContentPackProvider
    {
        public static ContentPack contentPack;
        
        public string identifier => "bubbet.titanfall2mod";

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        internal static void Initialize()
        {
            ContentManager.collectContentPackProviders += AddCustomContent;
        }

        private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new ContentPackProvider());
        }
    }
}