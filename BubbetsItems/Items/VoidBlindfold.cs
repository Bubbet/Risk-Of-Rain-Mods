﻿using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

namespace BubbetsItems.Items
{
	public class VoidBlindfold : ItemBase
	{
		protected override void MakeTokens()
		{
			// Where III is located in ACIDSOAKEDBLINDFOLD_DESC, create a new config for spawn time please
			base.MakeTokens();
			AddToken("VOIDBLINDFOLD_NAME", "Lost Seers Tragedy");
			AddToken("VOIDBLINDFOLD_PICKUP", ""); // TODO the rest of these tokens need to exist
			AddToken("VOIDBLINDFOLD_DESC", "Every {2} seconds, " + "summon a Blind Vermin".Style(StyleEnum.Utility) + " with " + "{1} ".Style(StyleEnum.Utility) + "Common".Style(StyleEnum.White) + " or " + "Uncommon".Style(StyleEnum.Green) + " items.");
			AddToken("VOIDBLINDFOLD_DESC_SIMPLE", "Every 30 seconds, " + "summon a Blind Vermin".Style(StyleEnum.Utility) + " with " + "10 ".Style(StyleEnum.Utility) + "(+5 per stack) ".Style(StyleEnum.Stack) + "Common".Style(StyleEnum.White) + " or " + "Uncommon".Style(StyleEnum.Green) + " items.");
			SimpleDescriptionToken = "VOIDBLINDFOLD_DESC_SIMPLE"; 
			AddToken("VOIDBLINDFOLD_LORE", "What is that smell?");
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