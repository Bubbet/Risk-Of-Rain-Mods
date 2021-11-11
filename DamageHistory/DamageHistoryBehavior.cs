using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RoR2;
using UnityEngine;

namespace DamageHistory
{
    public class DamageHistoryBehavior : MonoBehaviour, ILifeBehavior
    {
        public readonly List<DamageLog> history = new List<DamageLog>();
        private float oldHealth;
        private HealthComponent healthComponent;

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
            Texture icon = Resources.Load<Texture>("Textures/BodyIcons/texUnidentifiedKillerIcon");
            var isFallDamage = (obj.damageType & DamageType.FallDamage) > DamageType.Generic;
            AddOrMerge(isFallDamage ? "Fall Damage" : GetName(obj.attacker?.gameObject), obj.damage, icon);
        }

        private void AddOrMerge(string name, float damage, Texture icon)
        {
            if (history.Count > 0)
            {
                var last = history.FirstOrDefault(x => x.Chip && x.Who == name);
                if (last.Chip && damage < 10 * Run.instance.difficultyCoefficient)
                {
                    var where = history.IndexOf(last);
                    last.Damage += damage;
                    history[where] = last;
                    return;
                }
            }
            
            history.Add(new DamageLog(name, damage, icon, damage < 10));
        }
        
        private void CharacterDeath(DamageReport obj)
        {
            if (obj.victimBody.gameObject != gameObject) return;
            Texture icon = Resources.Load<Texture>("Textures/BodyIcons/texUnidentifiedKillerIcon");
            if (obj.attacker != null)
            {
                var comp = obj.attacker.GetComponent<CharacterBody>();
                if (comp) icon = comp.portraitIcon;
            }
            history.Add(new DamageLog(GetName(obj.attacker), obj.damageDealt, icon));
            OnDeathStart();
        }
        private void OnHeal(HealthComponent hc, float amount, bool nonRegen)
        {
            if (hc.gameObject != gameObject) return;
            OnHeal(amount);
        }

        private void OnHeal(float amount)
        {
            if (history.Count == 0) return;
            var log = history[0];
            ref var iamount = ref log.Damage;            
            iamount -= amount;
            if (iamount <= 0)
            {
                DamageHistoryPlugin.Log.LogInfo("Removing " + log.Who + " for character: " + gameObject.name);
                history.RemoveAt(0);
                if (iamount < 0) OnHeal(-iamount);
            }
            else
            {
                log.Damage = iamount;
                history[0] = log;
            }
        }

        private string GetName([CanBeNull] GameObject obj)
        {
            string who = "Unknown";
            if (!(obj is null))
            {
                try
                {
                    who = obj.GetComponent<CharacterBody>().GetDisplayName();
                }
                catch (NullReferenceException)
                {
                    who = obj.name;
                }
            }

            return who;
        }

        public StringBuilder BuildString(bool flip = true, bool verboose = false)
        {
            history.Reverse();
            var sb = new StringBuilder();
            sb.AppendLine();
            if (verboose)
            {
                sb.AppendLine(healthComponent.body.GetUserName());
            }
            sb.AppendLine("Damage History: " + history.Sum(x => x.Damage));
            foreach (DamageLog log in history)
            {
                if (verboose)
                    sb.Append("T-" + Mathf.Abs(log.Time - Time.time) + " - ").Append("Attacker: " + log.Who).AppendLine(" - Amount: " + log.Damage);
                else
                    sb.Append(log.Who).AppendLine(" - " + (int) log.Damage);
            }
            if (flip) history.Reverse();
            else history.Clear();
            return sb;
        }

        public void OnDeathStart()
        {
            var sb = BuildString(true, true);
            DamageHistoryPlugin.Log.LogInfo(sb);
        }
    }
    
    public struct DamageLog
    {
        public float Time;
        public string Who;
        public float Damage;
        public Texture Icon;
        public bool Chip;

        public DamageLog(string who, float damage, Texture icon, bool chip = false)
        {
            Time = UnityEngine.Time.time;
            Who = who;
            Damage = damage;
            Icon = icon;
            Chip = chip;
        }
    }
}