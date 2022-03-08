#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RoR2;
using UnityEngine;

namespace DamageHistory
{
    public class DamageHistoryBehavior : MonoBehaviour
    {
        /// <summary>
        /// Key is genrally a gameobject, but can be a string for unknown and fall damage
        /// </summary>
        public readonly Dictionary<object, DamageLog> history = new Dictionary<object, DamageLog>();
        private float oldHealth;
        public HealthComponent healthComponent;

        public void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            //HarmonyPatches.onCharacterHealWithRegen += OnHeal;
            GlobalEventManager.onClientDamageNotified += ClientDamaged;
            //GlobalEventManager.onServerDamageDealt += report => report.damageInfo.inflictor;
            //GlobalEventManager.onCharacterDeathGlobal += CharacterDeath;
        }

        public void OnDestroy()
        {
            //HarmonyPatches.onCharacterHealWithRegen -= OnHeal;
            GlobalEventManager.onClientDamageNotified -= ClientDamaged;
            //GlobalEventManager.onCharacterDeathGlobal -= CharacterDeath;
        }

        public void FixedUpdate()
        {
            var newHealth = healthComponent.health;
            var diff = newHealth - oldHealth;
            if (diff > 0)
            {
                OnHeal(diff);
            }

            if (newHealth >= healthComponent.fullHealth)
            {
                history.Clear();
            }

            oldHealth = newHealth;
        }

        private void ClientDamaged(DamageDealtMessage obj)
        {
            if (obj.victim != gameObject) return;
            object? attacker = null;
            if (obj.damageType.HasFlag(DamageType.FallDamage)) attacker = "Fall Damage";
            if (obj.damageType.HasFlag(DamageType.OutOfBounds)) attacker = "Out Of Bounds";
            attacker ??= obj.attacker;
            attacker ??= "Unknown";
            if (history.TryGetValue(attacker, out var value))
            {
                value.amount += obj.damage;
                value.when = Time.time;
            }
            else
            {
                history.Add(attacker, new DamageLog(attacker, obj.damage));
            }
            
        }

        private void OnHeal(float amount)
        {
            while (true)
            {
                if (history.Count == 0) return;
                var sorted = history.Values.ToList();
                if (history.Count > 1) sorted.Sort((o, o1) => Math.Sign(o1.when - o.when));
                sorted[0].amount -= amount;
                if (sorted[0].amount <= 0)
                {
                    history.Remove(sorted[0].who);
                    if (sorted[0].amount < 0)
                    {
                        amount = -sorted[0].amount;
                        continue;
                    }
                }

                break;
            }
        }
    }
    // TODO gut this whole thing to store logs by gameobject(attacker) so i can do a tablelookup when adding new damage; set the time to most recent damage; track the amount of hits per attacker;
    public class DamageLog
    {
        public float when;
        public float amount;
        public object who; // same as key in dict
        public string whoPretty;

        public DamageLog(object what, float damage)
        {
            amount = damage;
            when = Time.time;
            who = what;
            whoPretty = TryForPrettyName(who);
        }

        private static string TryForPrettyName(object logWho)
        {
            switch (logWho)
            {
                case GameObject go:
                    var body = go.GetComponent<CharacterBody>();
                    return body != null ? body.GetDisplayName() : go.name;
                case string str:
                    return str;
                default:
                    return "Unknown";
            }
        }
    }
}