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
		private double _lastLuck;

		public int rolls
		{
			get => _rolls;
			set
			{
				var temp = Math.Max(0, value);
				if (NetworkServer.active)
					body.SetBuffCount(Tarnished.BuffDef!.buffIndex, temp);
				if (temp > _rolls || temp <= 0 && !unLuckApplied)
				{
					ApplyLuck();
				}
				_rolls = temp;
			}
		}

		private void ApplyLuck()
		{
			body.master.luck -= luckDifference;

			var inst = SharedBase.GetInstance<Tarnished>()!;
			if (body.GetBuffCount(Tarnished.BuffDef) <= 0)
			{
				if (Tarnished.oldTarnished.Value)
					luckDifference = Mathf.FloorToInt(inst.scalingInfos[1].ScalingFunction(stack));
				else
				{
					luckDifference = 0;
					if (!body.HasBuff(Tarnished.BuffDef2) && !body.HasBuff(Tarnished.BuffDef))
						body.AddTimedBuff(Tarnished.BuffDef2, inst.scalingInfos[2].ScalingFunction(stack));
				}
				unLuckApplied = true;
				//if(NetworkServer.active && !body.HasBuff(Tarnished.BuffDef))
					//body.AddBuff(Tarnished.BuffDef);
			}
			else
			{
				unLuckApplied = false;
				luckDifference = Mathf.RoundToInt(inst.scalingInfos[3].ScalingFunction(stack));
				body.statsDirty = true;
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

				var instance = SharedBase.GetInstance<Tarnished>()!;
				rolls += Mathf.FloorToInt(instance.scalingInfos[0].ScalingFunction(stack) - instance.scalingInfos[0].ScalingFunction(_oldStack));
			}
			ApplyLuck();
		}
		
		private void InvChanged()
		{
			if (Math.Abs(body.master.luck - _lastLuck) < 0.001) return;
			luckDifference = 0;
			_lastLuck = body.master.luck;
			ApplyLuck();
		}

		private void OnDisable()
		{
			if (body && body.master)
				body.master.luck -= luckDifference;
			if(invSub && body && body.inventory)
				body.inventory.onInventoryChanged -= InvChanged;
		}
	}
}