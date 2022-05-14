using BepInEx.Configuration;
using BubbetsItems.Helpers;
using RoR2;

namespace BubbetsItems.Items
{
    public class Torturer : ItemBase
    {
        protected override void MakeTokens()
        {
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_NAME", "Torturer");
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_DESC", "Heal ".Style(StyleEnum.Heal) + "for " + "{0:0.#%} ".Style(StyleEnum.Heal) + "of damage over time you inflict.");
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_DESC_SIMPLE", "Heals ".Style(StyleEnum.Heal) + "you for " + "5% ".Style(StyleEnum.Heal) + "(+2.5% per stack) ".Style(StyleEnum.Stack) + "of damage dealt over time.");
            SimpleDescriptionToken = "HEAL_FROM_DOT_INFLICTED_ITEM_DESC_SIMPLE";
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_PICKUP", "Heal ".Style(StyleEnum.Heal) + "from inflicted damage over time.");
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_LORE", "Torturer");
            base.MakeTokens();
        }
        protected override void MakeConfigs()
        {
            base.MakeConfigs();
            AddScalingFunction("[d] * ([a] * 0.025 + 0.025)", "Healing From Damage", "[a] = amount, [d] = damage");
        }

        public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
        {
            scalingInfos[0].WorkingContext.d = 1;
            return base.GetFormattedDescription(inventory, token, forceHideExtended);
        }

        protected override void MakeBehaviours()
        {
            GlobalEventManager.onServerDamageDealt += DamageDealt;
            base.MakeBehaviours();
        }
        protected override void DestroyBehaviours()
        {
            GlobalEventManager.onServerDamageDealt -= DamageDealt;
            base.DestroyBehaviours();
        }
        private void DamageDealt(DamageReport obj)
        {
            var dot = (obj.damageInfo.damageType & DamageType.DoT) > DamageType.Generic;
            if (!dot || !obj.attackerBody || !obj.attackerBody.inventory) return;
            // ReSharper disable twice Unity.NoNullPropagation
            var count = obj.attackerBody.inventory.GetItemCount(ItemDef);
            if (count <= 0) return;
            //var amt = obj.damageDealt * (count * 0.025f + 0.025f);
            var info = scalingInfos[0];
            info.WorkingContext.d = obj.damageDealt;
            var amt = info.ScalingFunction(count);
            //Logger.LogInfo("HEALED FROM DAMAGE: " + amt);
            obj.attackerBody.healthComponent.Heal(amt, default);
        }
    }
}