using HarmonyLib;
using RoR2;

namespace SkipIntroCutscene
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		[HarmonyPrefix, HarmonyPatch(typeof(SplashScreenController), nameof(SplashScreenController.Start))]
		public static void SetConsoleCommandValues()
		{
			Console.instance.SubmitCmd(null, "set_scene title");
		}
	}
}