using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace OnlyYourDamageNumbers
{
	[BepInPlugin("bubbet.onlyyourDamagenumbers", "Only Your Damage Numbers", "1.0.1")]
	public class OnlyYourDamageNumbersPlugin : BaseUnityPlugin
	{
		public void Awake()
		{
			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
		}
	}
}