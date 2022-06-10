using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Equipments
{
	public class HolographicDonkey : EquipmentBase
	{
		public const DeployableSlot Slot = (DeployableSlot) 340504;
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			Enabled.Value = true; // no gemo
			TargetAttachedTo = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Holographic Donkey Target Attached To", false, "Should the enemies try to target the enemy the donkey is attached to or just the donkey.");
			duration = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Holographic Donkey Duration", 15f, "Donkey effect duration.");
			range = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Holographic Donkey Range", 60f, "Donkey effect range.");
			// range
		}

		public override void MakeRiskOfOptions()
		{
			base.MakeRiskOfOptions();
			ModSettingsManager.AddOption(new CheckBoxOption(TargetAttachedTo));
			var config = new SliderConfig() { min = 0, max = 80};
			ModSettingsManager.AddOption(new SliderOption(duration, config));
			ModSettingsManager.AddOption(new SliderOption(range, config));
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("HOLOGRAPHICDONKEY_NAME", "Holographic Donkey");
			AddToken("HOLOGRAPHICDONKEY_PICKUP", "Be a shepard, lead the charge.");
			AddToken("HOLOGRAPHICDONKEY_DESC", "Distract enemies, or attach it to enemies to divert their attention to the enemy. Lasts {0} seconds.\n\nCooldown: {1}");
			AddToken("HOLOGRAPHICDONKEY_LORE", "This is gemos fault.");
		}

		public override string GetFormattedDescription(Inventory? inventory = null, string? token = null, bool forceHideExtended = false)
		{
			return Language.GetStringFormatted(EquipmentDef.descriptionToken, duration.Value, Cooldown.Value);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.GetDeployableSameSlotLimit))]
		public static bool GetDeployableLimit(CharacterMaster __instance, DeployableSlot slot, ref int __result)
		{
			if (slot != Slot) return true;
			__result = 1;
			return false;
		}

		private static GameObject? _projectile;
		public static ConfigEntry<bool> TargetAttachedTo;
		public static ConfigEntry<float> duration;
		public static ConfigEntry<float> range;
		public static GameObject projectile => _projectile ??= BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("HolographicDonkeyProjectile");

		public override EquipmentActivationState PerformEquipment(EquipmentSlot equipmentSlot)
		{
			if (!NetworkServer.active) return EquipmentActivationState.DidNothing; // swap to be client auth?
			var master = equipmentSlot.inventory.GetComponent<CharacterMaster>();
			if (!master) return EquipmentActivationState.DidNothing;
			if (master.IsDeployableLimited(Slot)) return EquipmentActivationState.DidNothing;
			var ray = equipmentSlot.GetAimRay();
			projectile.GetComponent<DonkeyBehavior>().teamIndex = equipmentSlot.teamComponent.teamIndex;
			ProjectileManager.instance.FireProjectile(
				new FireProjectileInfo
				{
					projectilePrefab = projectile,
					owner = equipmentSlot.gameObject,
					position = ray.origin,
					rotation = Quaternion.LookRotation(ray.direction),
				}
			);
			Util.PlaySound("Play_DeployDonkey", equipmentSlot.gameObject);
			return EquipmentActivationState.ConsumeStock;
		}
	}

	public class DonkeyBehavior : MonoBehaviour, IOnTakeDamageServerReceiver
	{
		private CharacterBody ownerBody;
		public Transform donkeyTransform;
		private float watch;
		private float interval = 1f;
		private BullseyeSearch search;
		private CharacterBody stuckTo;
		private TeamIndex stuckToTeam;
		private bool justStuck;
		private ProjectileStickOnImpact impact;
		public TeamIndex teamIndex;
		private int sound;

		public void Awake()
		{
			var group = GetComponent<HurtBoxGroup>();
			var hurt = group.mainHurtBox;
			Physics.IgnoreCollision(hurt.GetComponent<Collider>(), GetComponent<Collider>());
		}

		public void Start()
		{
			GameObject owner = GetComponent<ProjectileController>().owner;
			if (owner)
			{
				ownerBody = owner.GetComponent<CharacterBody>();
			}

			if (NetworkServer.active)
			{
				DeployToOwner();
			}

			if (ownerBody)
			{
				var body = GetComponent<CharacterBody>();
				body.teamComponent.teamIndex = teamIndex;
				body.hurtBoxGroup = GetComponent<HurtBoxGroup>();
				body.mainHurtBox = body.hurtBoxGroup.mainHurtBox;
			}

			var mask = TeamMask.AllExcept(ownerBody.teamComponent.teamIndex);
			mask.AddTeam(TeamIndex.Neutral);
			search = new BullseyeSearch
			{
				maxDistanceFilter = HolographicDonkey.range.Value,
				teamMaskFilter = mask,
			};
			impact = GetComponent<ProjectileStickOnImpact>();

			GetComponent<ProjectileSimple>().lifetime = HolographicDonkey.duration.Value;
		}

		private void Update()
		{
			if (justStuck && impact.stuckTransform)
			{
				justStuck = false;
				stuckTo = impact.stuckBody;
				if (stuckTo)
				{
					stuckToTeam = stuckTo.teamComponent.teamIndex;
					stuckTo.teamComponent.teamIndex = TeamIndex.Neutral;
				}
			}
			/*
			var eulerAngles = donkeyTransform.rotation.eulerAngles;
			eulerAngles.y += Time.deltaTime * 30f;
			donkeyTransform.rotation = Quaternion.Euler(eulerAngles);
			*/
			var time = Time.deltaTime;
			watch += time;
			if (watch > interval)
			{
				watch -= interval;
				search.searchOrigin = transform.position;
				search.RefreshCandidates();
				var results = search.GetResults();
				foreach (var hurtBox in results)
				{
					if(!hurtBox) continue;
					var hc = hurtBox.healthComponent;
					if(!hc) continue;
					var body = hc.body;
					if(!body) continue;
					var master = body.master;
					if(!master) continue;
					var ais = master.aiComponents;
					if(!ais.Any()) continue;
					foreach (var ai in ais)
					{
						if (stuckTo && stuckTo.master != master && HolographicDonkey.TargetAttachedTo.Value)
						{
							ai.currentEnemy.gameObject = stuckTo.gameObject;
							ai.customTarget.gameObject = stuckTo.gameObject;
						}
						else
						{
							ai.currentEnemy.gameObject = gameObject;
							ai.customTarget.gameObject = gameObject;
							//ai.skillDriverUpdateTimer = 2f;
							//ai.targetRefreshTimer = 2f;
						}
					}
				}

				if (sound % 2 == 0) Util.PlaySound("Play_AttractDonkey", gameObject); 
				sound++;
			}
			donkeyTransform.Rotate(0, 30f * time, 0);
		}

		public void StuckTo() => justStuck = true;

		private void OnDisable()
		{
			if (stuckTo)
			{
				stuckTo.teamComponent.teamIndex = stuckToTeam;
			}
		}

		private void DeployToOwner()
		{
			if (!ownerBody) return;
			CharacterMaster master = ownerBody.master;
			if (master)
			{
				master.AddDeployable(GetComponent<Deployable>(), HolographicDonkey.Slot);
			}
		}

		public void OnTakeDamageServer(DamageReport damageReport)
		{
			if (stuckTo && stuckTo.healthComponent)
			{
				var damageInfo = (DamageInfo) SharedBase.MemberwiseCloneRef?.Invoke(damageReport.damageInfo, new object[]{})!;
				damageInfo.inflictor = damageInfo.attacker;
				damageInfo.attacker = ownerBody.gameObject;
				stuckTo.healthComponent.TakeDamage(damageInfo);
			}
		}
	}
}