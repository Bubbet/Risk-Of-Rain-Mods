using System;
using System.Collections.Generic;
using System.Linq;
using BubbetsItems.Items;
using HG;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Behaviours
{
	[RequireComponent(typeof(NetworkedBodyAttachment))]
	public class SubmergingCisternNetworkBehavior : NetworkBehaviour
	{
		private NetworkedBodyAttachment networkedBodyAttachment;
		private SphereSearch sphereSearch;
		public float radius; // Do via scalingInfo
		public TetherVfxOrigin tetherVfxOrigin;
		private double clearTimer;

		private void Awake()
		{
			networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
			sphereSearch = new SphereSearch();
		}

		private void OnEnable()
		{
			GlobalEventManager.onClientDamageNotified += DamageDealt;
			GlobalEventManager.onServerDamageDealt += DamageDealt;
		}

		private void OnDisable()
		{
			GlobalEventManager.onServerDamageDealt -= DamageDealt;
			GlobalEventManager.onClientDamageNotified -= DamageDealt;
		}

		private void DamageDealt(DamageReport obj)
		{
			DamageDealt(obj.attacker.GetComponent<CharacterBody>(), obj.damageDealt);
		}
		
		private void DamageDealt(DamageDealtMessage obj)
		{
			if (!obj.attacker) return;
			DamageDealt(obj.attacker.GetComponent<CharacterBody>(), obj.damage);
		}

		private void DamageDealt(CharacterBody body, float damage)
		{
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			var inst = SharedBase.GetInstance<SubmergingCistern>();
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			var info = inst.scalingInfos[0];
			info.WorkingContext.d = damage;
			var healing = info.ScalingFunction(amount);
			var info1 = inst.scalingInfos[1];
			var teammateCount = Mathf.FloorToInt(info1.ScalingFunction(amount));
			var info2 = inst.scalingInfos[2];
			var range = info2.ScalingFunction(amount);
			HealNearby(healing, teammateCount, range);
		}

		public void SearchForTargets(ref List<HurtBox> dest)
		{
			var mask = TeamMask.none;
			mask.AddTeam(networkedBodyAttachment.attachedBody.teamComponent.teamIndex);
			sphereSearch.mask = LayerIndex.entityPrecise.mask;
			sphereSearch.origin = transform.position;
			sphereSearch.radius = networkedBodyAttachment.attachedBody.radius + radius;
			sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
			sphereSearch.RefreshCandidates();
			sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
			sphereSearch.OrderCandidatesByDistance();
			sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
			sphereSearch.GetHurtBoxes(dest);
			dest = dest.Where(x => x.healthComponent.gameObject != networkedBodyAttachment.attachedBody.gameObject && x.healthComponent.alive).ToList();
			sphereSearch.ClearCandidates();
		}

		private void HealNearby(float amount, int teammateCount, float range)
		{
			radius = range;
			//var test = inst.scalingInfos[0].ScalingFunction(amount);
			var list = new List<HurtBox>();
			SearchForTargets(ref list);

			var list2 = CollectionPool<HealInfo, List<HealInfo>>.RentCollection();
			foreach (var hurtBox in list)
			{
				var hc = hurtBox.healthComponent;
				list2.Add(new HealInfo(hc.health / hc.fullHealth, hc));
			}
			var sum = list2.Sum(x => x.fraction);
			list2.Sort((h1, h2) => Mathf.FloorToInt(Mathf.Sign(h1.fraction - h2.fraction)));

			var pmask = new ProcChainMask();
			pmask.AddProc(ProcType.HealOnHit);
			var i = 0;
			var list3 = CollectionPool<Transform, List<Transform>>.RentCollection();
			foreach (var hurtBox in list)
			{
				var hc = hurtBox.healthComponent;
				var hamount = list2[i].fraction / sum * amount;
				if (hamount > 0)
				{
					list3.Add(hc.transform);
					if (NetworkServer.active)
						hc.Heal(hamount, pmask);
					if (i > teammateCount) break;
				}
				i++;
			}
			
			if (tetherVfxOrigin)
			{
				tetherVfxOrigin.SetTetheredTransforms(list3);
				clearTimer = Time.time + 0.2;
			}

			CollectionPool<Transform, List<Transform>>.ReturnCollection(list3);
			CollectionPool<HealInfo, List<HealInfo>>.ReturnCollection(list2);
		}

		private void FixedUpdate()
		{
			if (Time.time > clearTimer && tetherVfxOrigin.tetheredTransforms.Count > 0)
			{
				for (var i = 0; i < tetherVfxOrigin.tetheredTransforms.Count; i++)
					tetherVfxOrigin.RemoveTetherAt(0);
			}
		}
	}
	
	struct HealInfo
	{
		public float fraction;
		public HealthComponent hc;
		public HealInfo(float healthFraction, HealthComponent hc)
		{
			fraction = healthFraction;
			this.hc = hc;
		}
	}
}