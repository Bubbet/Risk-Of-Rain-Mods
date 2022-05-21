using System;
using BubbetsItems.Items.VoidLunar;
using RoR2;
using RoR2.Items;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.ItemBehaviors
{
	public class TarnishedBehavior : BaseItemBodyBehavior
	{
		private int _rolls;
		private bool unLuckApplied;
		private int luckDifference;

		[ItemDefAssociationAttribute(useOnServer = true, useOnClient = true)]
		private static ItemDef? GetItemDef()
		{
			var instance = SharedBase.GetInstance<Tarnished>();
			return instance?.ItemDef;
		}

		private int _oldStack;
		private bool invSub;

		public int rolls
		{
			get => _rolls;
			set
			{
				_rolls = value;
				if (NetworkServer.active)
					if (Tarnished.BuffDef is not null)
						body.SetBuffCount(Tarnished.BuffDef.buffIndex, rolls);
				if (value <= 0 && !unLuckApplied)
				{
					ApplyLuck();
				}
			}
		}

		private void ApplyLuck()
		{
			body.master.luck -= luckDifference;

			
			if (body.GetBuffCount(Tarnished.BuffDef) <= 0)
			{
				var inst = SharedBase.GetInstance<Tarnished>()!.scalingInfos[1].ScalingFunction(stack);
				luckDifference = Mathf.FloorToInt(inst);
				unLuckApplied = true;
				//if(NetworkServer.active && !body.HasBuff(Tarnished.BuffDef))
					//body.AddBuff(Tarnished.BuffDef);
			}
			else
			{
				unLuckApplied = false;
				luckDifference = 1;
				//if(NetworkServer.active && body.HasBuff(Tarnished.BuffDef))
					//body.RemoveBuff(Tarnished.BuffDef);
			}

			body.master.luck += luckDifference;
		}

		public void Update()
		{
			if (_oldStack == stack) return;
			
			OnStackChange();
			_oldStack = stack;
		}

		private void OnStackChange()
		{
			if (stack > _oldStack)
			{
				if (!invSub)
				{
					invSub = true;
					body.inventory.onInventoryChanged += InvChanged;
				}

				var instance = SharedBase.GetInstance<Tarnished>();
				rolls += Mathf.FloorToInt(instance.scalingInfos[0].ScalingFunction(stack) - instance.scalingInfos[0].ScalingFunction(_oldStack));
			}
			ApplyLuck();
		}

		private void InvChanged()
		{
			luckDifference = 0;
			ApplyLuck();
		}

		private void OnDisable()
		{
			body.master.luck -= luckDifference;
			if(invSub)
				body.inventory.onInventoryChanged -= InvChanged;
		}
	}
}