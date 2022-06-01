using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BubbetsItems.Items
{
	public class VoidBlindfold : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("VOIDBLINDFOLD_NAME", "Lost Seers Tragedy");
			var corrupt = "Corrupts all Acid Soaked Blindfolds.".Style(StyleEnum.Void);
			AddToken("VOIDBLINDFOLD_PICKUP", "Spawn a Barnacle with items on-kill." + corrupt);
			AddToken("VOIDBLINDFOLD_DESC", "Killing an enemy " + "spawns a".Style(StyleEnum.Utility) + " Barnacle ".Style(StyleEnum.Void) + "with " + "{1} ".Style(StyleEnum.Utility) + "Void Common ".Style(StyleEnum.VoidItem) + "items. " + "Limited to " + "{0} ".Style(StyleEnum.Utility) + "Barnacles".Style(StyleEnum.Void) + ". ");
			AddToken("VOIDBLINDFOLD_DESC_SIMPLE", "Killing an enemy " + "spawns a".Style(StyleEnum.Utility) + " Barnacle ".Style(StyleEnum.Void) + "with " + "3 ".Style(StyleEnum.Utility) + "(+3 per stack) ".Style(StyleEnum.Stack) + "Void Common ".Style(StyleEnum.VoidItem) + "items. " + "Limited to " + "1 ".Style(StyleEnum.Utility) + "(+1 per stack) ".Style(StyleEnum.Stack) + "Barnacles".Style(StyleEnum.Void) + ". ");
			SimpleDescriptionToken = "VOIDBLINDFOLD_DESC_SIMPLE"; 
			AddToken("VOIDBLINDFOLD_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a]", "Barnacle Count");
			AddScalingFunction("[a] * 3", "Item Count");
			KillOld = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Void Blindfold Should Kill Old", true, "Should it kill the old minion, or just not spawn more.");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing("ItemDefAcidSoakedBlindfold");
		}

		public override void MakeRiskOfOptions()
		{
			base.MakeRiskOfOptions();
			ModSettingsManager.AddOption(new CheckBoxOption(KillOld));
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onCharacterDeathGlobal += CharacterDeath;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onCharacterDeathGlobal -= CharacterDeath;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.GetDeployableSameSlotLimit))]
		public static bool GetDeployableLimit(CharacterMaster __instance, DeployableSlot slot, ref int __result)
		{
			if (slot != Slot) return true;
			var inv = __instance.inventory;
			if (!inv) return true;
			var inst = GetInstance<VoidBlindfold>();
			var stack = inv.GetItemCount(inst.ItemDef);
			if (stack <= 0) return true;
			var maxCount = inst.scalingInfos[0].ScalingFunction(stack);
			__result = Mathf.FloorToInt(maxCount);
			return false;
		}

		private const DeployableSlot Slot = (DeployableSlot) 340503;
		private void CharacterDeath(DamageReport obj)
		{
			var body = obj.attackerBody;
			if (!body) return;
			var master = obj.attackerMaster;
			if (!master) return;
			var inv = body.inventory;
			if (!inv) return;
			var stack = inv.GetItemCount(ItemDef);
			if (stack <= 0) return;
			if (!KillOld.Value)
			{
				var maxCount = scalingInfos[0].ScalingFunction(stack);
				var count = master.GetDeployableCount(Slot);
				if (count >= maxCount) return;
			}

			var request = new DirectorSpawnRequest( // RoR2/DLC1/VoidBarnacle/cscVoidBarnacleNoCast.asset
				Csc, // RoR2/DLC1/VoidBarnacle/cscVoidBarnacle.asset
				new DirectorPlacementRule
				{
					placementMode = DirectorPlacementRule.PlacementMode.Approximate,
					minDistance = 3f,
					maxDistance = 40f,
					spawnOnTarget = obj.victim.transform
				}, RoR2Application.rng
			) {summonerBodyObject = body.gameObject, onSpawnedServer = BarnacleSpawnedServer};
			DirectorCore.instance.TrySpawnObject(request);
		}

		private SpawnCard? csc;
		private ConfigEntry<bool> KillOld;

		public SpawnCard Csc => csc ??= Addressables
			.LoadAssetAsync<SpawnCard>("RoR2/DLC1/VoidBarnacle/cscVoidBarnacleAlly.asset").WaitForCompletion(); 

		private void BarnacleSpawnedServer(SpawnCard.SpawnResult obj)
		{
			var instances = obj.spawnedInstance;
			if (!instances) return;
			var master = instances.GetComponent<CharacterMaster>();
			if (!master) return;
			var body = obj.spawnRequest.summonerBodyObject.GetComponent<CharacterBody>();
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			var stack = inv.GetItemCount(ItemDef);
			if (stack <= 0) return;
			
			master.teamIndex = body.master.teamIndex;
			//master.inventory;

			var runInstance = Run.instance;
			var list1 = runInstance.availableVoidTier1DropList;
			var tRng = runInstance.treasureRng;
			
			for (var i = 0; i < scalingInfos[1].ScalingFunction(stack); i++)
			{
				master.inventory.GiveItem(list1[tRng.RangeInt(0, list1.Count)].pickupDef.itemIndex);
			}

			var deployable = instances.GetComponent<Deployable>();
			if (!deployable) return;
			//deployable.onUndeploy = new UnityEvent();
			//deployable.ownerMaster = body.master;
			body.master.AddDeployable(deployable, Slot);
		}
	}
}