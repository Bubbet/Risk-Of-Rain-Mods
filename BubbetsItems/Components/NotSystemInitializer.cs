using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RoR2;

namespace BubbetsItems
{
	/// <summary>
	///	An attribute that flags a method to be subscribed to a delegate that gets called in the postfix of the type passed as an argument
	/// Made because i'm tired of shit breaking system initializer
	/// </summary>
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class NotSystemInitializer : Attribute
	{
		private readonly Type[]? _dependency;
		private MethodInfo? Target;
		private static readonly List<NotSystemInitializer> Instances = new();
		private static readonly Dictionary<Type, Info> Types = new();
		private static readonly List<NotSystemInitializer> NoType = new();
		private static bool _noTypeHooked;

		public NotSystemInitializer(params Type[]? dependency)
		{
			_dependency = dependency;
			Instances.Add(this);
		}
		
		private static void HarmonyPostfix(MethodBase __originalMethod)
		{
			var typ = __originalMethod.DeclaringType;
			if (typ == typeof(Language))
			{
				foreach (var attribute in NoType)
				{
					attribute.Target?.Invoke(null, new object[] { });
				}
				return;
			}

			foreach (var attribute in Types[typ])
			{
				var loaded = true;
				if (attribute._dependency != null && attribute._dependency.Length > 1)
				{
					foreach (var type in attribute._dependency)
					{
						if (typ != type && !Types[type].Loaded)
							loaded = false;
					}
				}
				
				if (loaded)
					attribute.Target?.Invoke(null, new object[] { });
			}

			Types[typ].Loaded = true;
		}
		
		public static void Hook(Harmony harm)
		{
			var assembly = Assembly.GetCallingAssembly();

			foreach (var type in assembly.GetTypes())
			foreach (var method in type.GetMethods())
			foreach (var attribute in method.GetCustomAttributes())
				if (attribute is NotSystemInitializer attrib)
					attrib.Target = method;
			
			foreach (var notSystemInitializer in Instances)
			{
				if (notSystemInitializer._dependency == null)
				{
					NoType.Add(notSystemInitializer);
					continue;
				}

				foreach (var type in notSystemInitializer._dependency)
				{
					if (!Types.ContainsKey(type))
					{
						var meth = type.GetMethod("Init", BindingFlags.Static | BindingFlags.NonPublic);
						if (meth == null) continue;
						Types.Add(type, new Info());
						harm.Patch(meth, null, HarmPostfix);
					}
					
					Types[type].Add(notSystemInitializer);
				}
			}

			if (_noTypeHooked || NoType.Count <= 0) return;
			var mee = typeof(Language).GetMethod(nameof(Language.Init));
			harm.Patch(mee, null, HarmPostfix);
			_noTypeHooked = true;
		}

		private static HarmonyMethod? _harmPostfix;
		private static HarmonyMethod HarmPostfix => _harmPostfix ??= new HarmonyMethod(typeof(NotSystemInitializer).GetMethod(nameof(HarmonyPostfix), BindingFlags.NonPublic | BindingFlags.Static));
	}

	public class Info : List<NotSystemInitializer>
	{
		public bool Loaded;
	}
}