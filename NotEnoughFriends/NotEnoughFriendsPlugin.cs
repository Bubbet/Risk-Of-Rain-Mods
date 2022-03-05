using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using Console = RoR2.Console;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace NotEnoughFriends
{
	[BepInPlugin("bubbet.notenoughfriends", "Not Enough Friends", "1.0.2")]
	public class NotEnoughFriendsPlugin : BaseUnityPlugin
	{
		public static ConfigEntry<int> LobbySize;
		public static (int players, int hardPlayers, int localPlayers) MaxDefault = (RoR2Application.maxPlayers, RoR2Application.hardMaxPlayers, RoR2Application.maxLocalPlayers);


		private Harmony harmony;
		private PatchClassProcessor patchProcessor;
		private bool _enabled;

		private void Awake()
		{
			harmony = new Harmony(Info.Metadata.GUID);
			patchProcessor = new PatchClassProcessor(harmony, typeof(HarmonyPatches));
			OnEnable();
		}

		private void OnEnable()
		{
			if (_enabled) return;
			_enabled = true;
			if (LobbySize == null)
				LobbySize = Config.Bind("General", "Lobby Size", 16, "Sets the max size of game lobbies.");
			LobbySize.SettingChanged += LobbySizeOnSettingChanged;
			RoR2Application.onLoad += OnGameLoad;
			patchProcessor.Patch();
			Logger.LogInfo("Enabling Mod");
		}
		private void OnDisable()
		{
			_enabled = false;
			LobbySize.SettingChanged -= LobbySizeOnSettingChanged;
			RoR2Application.onLoad -= OnGameLoad;
			SetSize();
			harmony.UnpatchSelf();
			Logger.LogInfo("Disabling Mod");
		}
		public void SetSize(int? maxPlayers = null, int? hardMaxPlayers = null, int? maxLocalPlayers = null)
		{
			Logger.LogMessage($"Setting max players to {maxPlayers ?? MaxDefault.players}");
			RoR2Application.maxPlayers = maxPlayers ?? MaxDefault.players;
			RoR2Application.hardMaxPlayers = hardMaxPlayers ?? MaxDefault.hardPlayers;
			RoR2Application.maxLocalPlayers = maxLocalPlayers ?? MaxDefault.localPlayers;
			Console.instance.SubmitCmd(null, "sv_maxplayers " + maxPlayers);
			Console.instance.SubmitCmd(null, "steam_lobby_max_members " + maxPlayers);
		}
		private void LobbySizeOnSettingChanged(object o, EventArgs e)
		{
			OnGameLoad();
		}
		private void OnGameLoad()
		{
			var maxPlayers = LobbySize.Value;
			SetSize(maxPlayers, maxPlayers, maxPlayers);
		}
	}
}