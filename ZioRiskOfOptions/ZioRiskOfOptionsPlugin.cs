using System.Reflection;
using BepInEx;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.Options;

namespace ZioRiskOfOptions
{
	[BepInPlugin("bubbet.zioriskofoptions", "Zio Risk Of Options", "1.0.0")]
	[BepInDependency("bubbet.zioconfigfile")]
	[BepInDependency("com.rune580.riskofoptions")]
	public class ZioRiskOfOptionsPlugin : BaseUnityPlugin
	{
		/*
		public void Awake()
		{
			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
		}*/
	}
	/*
	[HarmonyPatch]
	public class HarmonyPatches
	{
		//*
		public static void PatchAddOption(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchLdarg(out _));
			c.GotoNext(x => x.MatchLdarg(out _));
			var target = c.Next;
			c.Index = 0;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<BaseOption, bool>>(option => false);
			c.Emit(OpCodes.Brtrue, target);
		}///

		public static MethodInfo SetCategoryName = typeof(BaseOption).GetMethod("SetCategoryName", BindingFlags.NonPublic | BindingFlags.Instance);
		public static MethodInfo SetName = typeof(BaseOption).GetMethod("SetName", BindingFlags.NonPublic | BindingFlags.Instance);
		public static MethodInfo SetDescription = typeof(BaseOption).GetMethod("SetDescription", BindingFlags.NonPublic | BindingFlags.Instance);
		
		[HarmonyPrefix, HarmonyPatch(typeof(ModSettingsManager), nameof(ModSettingsManager.AddOption))]
		public static void Prefix(BaseOption option)
		{
			if (!(option is ZioBaseOption zioBaseOption)) return;
			var configEntry = zioBaseOption.ConfigEntry;
			if (configEntry == null) return;
			var config = option.GetConfig();
			SetCategoryName.Invoke(option, new object[] {configEntry.Definition.Section, config});
			SetName.Invoke(option, new object[] {configEntry.Definition.Key, config});
			SetDescription.Invoke(option, new object[] {configEntry.Description.Description, config});
		}
	}
	*/
}