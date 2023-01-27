using System.Linq;
using BubbetsItems.Components;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Items.BarrierItems
{
	public class BoneVisor : ItemBase
	{
		private static BuffDef? _buffDef;
		public static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefBoneVisor");
		private static GameObject? _shardPrefab;

		public static GameObject ShardPrefab => (_shardPrefab ??= BubbetsItemsPlugin.ContentPack.networkedObjectPrefabs.Find("BoneVisorShard"))!;
		protected override void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack)
		{
			base.FillDefsFromSerializableCP(serializableContentPack);
			// yeahh code based content because TK keeps fucking freezing
			var buff = ScriptableObject.CreateInstance<BuffDef>();
			buff.canStack = true;
			buff.name = "BuffDefBoneVisor";
			buff.buffColor = new Color(r: 1, g: 0.80784315f, b: 0, a: 1);
			buff.iconSprite = BubbetsItemsPlugin.AssetBundle.LoadAsset<Sprite>("VisorBuff");
			serializableContentPack.buffDefs = serializableContentPack.buffDefs.AddItem(buff).ToArray();
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("BONEVISOR_NAME","Bone Visor");
			AddToken("BONEVISOR_DESC", "{3}% chance on kill to drop bone shards. Bone shards give {2}x barrier regeneration for {0} seconds.");
			AddToken("BONEVISOR_DESC_SIMPLE", "20% chance on kill to drop bone shards. Bone shards give 1x barrier regeneration for 3 seconds."); // TODO style descs
			SimpleDescriptionToken = "BONEVISOR_DESC_SIMPLE";
			AddToken("BONEVISOR_PICKUP", "Chance on kill to drop bone shards that give barrier regeneration.");
			AddToken("BONEVISOR_LORE","Was there a bone tribe somewhere lost here? I've found this near an ancient ruins on Mars and still haven't figured out the origins to it yet.");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("3", "Barrier Regen Buff Duration");
			AddScalingFunction("[a]", "Barrier Regen Buff Max Stacks");
			AddScalingFunction("1 * [b]", "Barrier Regen Buff Rate", desc:"[a] = item count; [b] = buff amount; [m] = max barrier");
			AddScalingFunction("(1 - 1 / Pow(([a] + 1), 0.33)) * 100", "Shard Spawn Chance");
			AddScalingFunction("10", "Barrier Add On Pickup");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[2].WorkingContext.b = 1;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onCharacterDeathGlobal += OnDeath;
			CommonBodyPatches.CollectExtraStats += GetBarrierDecay;
		}

		protected override void DestroyBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onCharacterDeathGlobal -= OnDeath;
			CommonBodyPatches.CollectExtraStats -= GetBarrierDecay;
		}

		private void OnDeath(DamageReport obj)
		{
			var body = obj.attackerBody;
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			var amount = inv.GetItemCount(ItemDef);
			if (amount <= 0) return;
			if (!Util.CheckRoll(scalingInfos[3].ScalingFunction(amount), obj.attackerMaster)) return;
			var shard = Object.Instantiate(ShardPrefab, obj.victim.transform.position, Random.rotation);
			if (!shard) return;
			var teamFilter = shard!.GetComponent<TeamFilter>();
			if (teamFilter) teamFilter.teamIndex = obj.attackerTeamIndex;
			NetworkServer.Spawn(shard);
		}

		private void GetBarrierDecay(ref CommonBodyPatches.ExtraStats obj)
		{
			var count = obj.inventory.GetItemCount(ItemDef);
			if (count <= 0) count = 1;
			var buffCount = obj.body.GetBuffCount(BuffDef);
			if (buffCount <= 0) return;
			var info = scalingInfos[2];
			info.WorkingContext.b = buffCount;
			info.WorkingContext.m = obj.body.maxBarrier;
			obj.barrierDecay = -info.ScalingFunction(count);
			obj.priority = 1;
		}
	}

	public class BonePickup : MonoBehaviour
	{
		// Token: 0x06001AC1 RID: 6849 RVA: 0x00072D8C File Offset: 0x00070F8C
		private void OnTriggerStay(Collider other)
		{
			if (!NetworkServer.active || !alive ||
			    TeamComponent.GetObjectTeam(other.gameObject) != teamFilter.teamIndex) return;
			var component = other.GetComponent<CharacterBody>();
			if (!component) return;
			var inv = component.inventory;
			if (!inv) return;
			var inst = SharedBase.GetInstance<BoneVisor>()!;
			var amt = inv.GetItemCount(inst.ItemDef);
			if (amt <= 0) amt = 1;
			alive = false;
			component.healthComponent.AddBarrier(inst.scalingInfos[4].ScalingFunction(amt));
			component.AddTimedBuff(BoneVisor.BuffDef, inst.scalingInfos[0].ScalingFunction(amt), Mathf.RoundToInt(inst.scalingInfos[1].ScalingFunction(amt)));
			//EffectManager.SimpleEffect(this.pickupEffect, base.transform.position, Quaternion.identity, true);
			Destroy(baseObject);
		}
		
		[Tooltip("The base object to destroy when this pickup is consumed.")]
		public GameObject baseObject;
		[Tooltip("The team filter object which determines who can pick up this pack.")]
		public TeamFilter teamFilter;
		public GameObject pickupEffect;

		// Token: 0x040020E7 RID: 8423
		private bool alive = true;
	}
}