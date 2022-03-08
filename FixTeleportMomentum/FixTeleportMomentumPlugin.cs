using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace FixTeleportMomentum
{
	[BepInPlugin("bubbet.fixteleportmomentum", "Fix Teleport Momentum", "1.0.0")]
	public class FixTeleportMomentumPlugin : BaseUnityPlugin
	{
		public void Awake()
		{
			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
		}
	}
}