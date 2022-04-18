using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using RoR2;
using SearchableAttribute = HG.Reflection.SearchableAttribute;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
[assembly: SearchableAttribute.OptIn]

namespace RampingEgo
{
	[BepInPlugin("bubbet.rampingego", "Ramping Ego", "1.0.0")]
	public class RampingEgoPlugin : BaseUnityPlugin
	{
		public void Awake()
		{
			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
		}

		[SystemInitializer]
		public static void FixToken()
		{
			var token = DLC1Content.Items.LunarSun.descriptionToken;
			Language.english.SetStringByToken(token, Language.english.GetLocalizedStringByToken(token).Replace("60</style> seconds", "60</style><style=cStack>(-50% per stack)</style> seconds"));
		}
	}
}