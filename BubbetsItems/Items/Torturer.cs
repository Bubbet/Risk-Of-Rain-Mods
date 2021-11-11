using BepInEx.Configuration;
using BubbetsItems.Helpers;
using RoR2;

namespace BubbetsItems.Items
{
    public class Torturer : ItemBase
    {
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

        protected override void MakeConfigs(ConfigFile configFile)
        {
            defaultScalingFunction = "[d] * ([a] * 0.025 + 0.025)";
            base.MakeConfigs(configFile);
        }

        public float ScalingFunction(int itemCount, float damageDealt)
        {
            return scalingFunction(new ExpressionContext { a = itemCount, d = damageDealt });
        }
        public override float ScalingFunction(int itemCount)
        {
            return ScalingFunction(itemCount, 1f);
        }

        private void DamageDealt(DamageReport obj)
        {
            var dot = (obj.damageInfo.damageType & DamageType.DoT) > DamageType.Generic;
            if (!dot || !obj.attackerBody || !obj.attackerBody.inventory) return;
            // ReSharper disable twice Unity.NoNullPropagation
            var count = obj.attackerBody.inventory.GetItemCount(ItemDef);
            if (count <= 0) return;
            //var amt = obj.damageDealt * (count * 0.025f + 0.025f);
            var amt = ScalingFunction(count, obj.damageDealt);
            //Logger.LogInfo("HEALED FROM DAMAGE: " + amt);
            obj.attackerBody.healthComponent.Heal(amt, default);
        }

        protected override void MakeTokens()
        {
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_NAME", "Torturer");
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_PICKUP", "Heal from damage over time inflicted.");
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_DESC", $"{"Heal".Style(StyleEnum.Heal)} for {"{1:P}".Style(StyleEnum.Heal)} of {"Damage Over Time".Style(StyleEnum.Damage)} you inflict.\n{{0}}");//"Heal 5% (+2.5% per item) of damage dealt via DOT.");
            AddToken("HEAL_FROM_DOT_INFLICTED_ITEM_LORE", "Torturer");
            base.MakeTokens();
        }
    }
}