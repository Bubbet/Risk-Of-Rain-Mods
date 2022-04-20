using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NCalc;
using RoR2;
using UnityEngine;
using SearchableAttribute = HG.Reflection.SearchableAttribute;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
[assembly: SearchableAttribute.OptIn]

namespace RampingEgo
{
	[BepInPlugin("bubbet.rampingego", "Ramping Ego", "1.1.0")]
	public class RampingEgoPlugin : BaseUnityPlugin
	{
		private ConfigEntry<string> _scalingFunction;
		private string _oldValue;
		private static Func<EgoContext, float> _function;
		private static EgoContext _context;

		public void Awake()
		{
			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
			_scalingFunction = Config.Bind("General", "Scaling Function", "[o] / [a]", "[a] = egocentrism amount, [o] = previous value from il (default 60 unless changed by other mods)");
			_oldValue = _scalingFunction.Value;
			_function = new Expression(_oldValue).ToLambda<EgoContext, float>();
			_scalingFunction.SettingChanged += EntryChanged;
			_context = new EgoContext();
		}

		private void EntryChanged(object sender, EventArgs e)
		{
			if (_scalingFunction.Value == _oldValue) return;
			_function = new Expression(_scalingFunction.Value).ToLambda<EgoContext, float>();
			_oldValue = _scalingFunction.Value;
		}

		[SystemInitializer]
		public static void FixToken()
		{
			var token = DLC1Content.Items.LunarSun.descriptionToken;
			Language.english.SetStringByToken(token, Language.english.GetLocalizedStringByToken(token).Replace("60</style> seconds", "60</style><style=cStack>(-50% per stack)</style> seconds"));
		}

		public static float GetDuration(float f, int behaviorStack)
		{
			_context.o = f;
			_context.a = behaviorStack;
			return _function.Invoke(_context);
		}
	}

	public class EgoContext
	{
		public float a = 1;
		public float o = 60;
		
		public int RoundToInt(float x)
		{
			return Mathf.RoundToInt(x);
		}

		public float Log(float x)
		{
			return Mathf.Log(x);
		}

		public float Max(float x, float y)
		{
			return Mathf.Max(x, y);
		}

		public float Min(float x, float y)
		{
			return Mathf.Min(x, y);
		}
	}
}