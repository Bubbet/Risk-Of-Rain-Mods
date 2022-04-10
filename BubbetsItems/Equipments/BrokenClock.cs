using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BubbetsItems.Bases;
using BubbetsItems.Helpers;
using InLobbyConfig.Fields;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BubbetsItems.Equipments
{
    public class BrokenClock : EquipmentBase
    {
        public static ConfigEntry<float>? Cooldown;
        public static ConfigEntry<float>? Duration;
        public static ConfigEntry<float>? Interval;

        public static readonly FieldInfo Velocity = typeof(CharacterMotor).GetField("velocity");
        public static BrokenClock? Instance;

        public BrokenClock()
        {
            Instance = this;
        }

        protected override void MakeTokens()
        {
            base.MakeTokens();
            AddToken("BROKEN_CLOCK_NAME", "Broken Clock");
            AddToken("BROKEN_CLOCK_PICKUP", "Turn back the clock " + "{0} seconds ".Style(StyleEnum.Utility) + "for yourself.");
            AddToken("BROKEN_CLOCK_DESC", "Turn back the clock " + "{0} seconds ".Style(StyleEnum.Utility) + "to revert " + "health ".Style(StyleEnum.Heal) + "and " + "velocity".Style(StyleEnum.Utility) + ".");
            AddToken("BROKEN_CLOCK_LORE", "Broken clock lore.");
        }

        public override string GetFormattedDescription(Inventory? inventory = null, string? token = null)
        {
            return Language.GetStringFormatted(EquipmentDef!.descriptionToken, Duration!.Value);
        }

        public override bool PerformEquipment(EquipmentSlot equipmentSlot)
        {
            base.PerformEquipment(equipmentSlot);
            ConfigUpdate();
            return equipmentSlot.inventory.GetComponent<BrokenClockBehaviour>().ToggleReversing();
        }

        public override void OnUnEquip(Inventory inventory, EquipmentState newEquipmentState)
        {
            base.OnUnEquip(inventory, newEquipmentState);
            Object.Destroy(inventory.GetComponent<BrokenClockBehaviour>()); // TODO check if this even works
        }

        public override void OnEquip(Inventory inventory, EquipmentState? oldEquipmentState)
        {
            base.OnEquip(inventory, oldEquipmentState);
            inventory.gameObject.AddComponent<BrokenClockBehaviour>();
        }

        protected override void MakeConfigs()
        {
            base.MakeConfigs();
            Cooldown = configFile!.Bind(ConfigCategoriesEnum.General, "Broken Clock Cooldown", 60f, "Broken Clock equipment cooldown.", 5f);
            Cooldown.SettingChanged += (_, _) => ConfigUpdate();
            Duration = configFile.Bind(ConfigCategoriesEnum.General, "Broken Clock Buffer Duration", 10f, "Duration of time to store in the broken clock.");
            Duration.SettingChanged += (_, _) => ConfigUpdate();
            Interval = configFile.Bind(ConfigCategoriesEnum.General, "Broken Clock Keyframe Interval", 0.25f, "How often to capture a keyframe and store it. Also determines the size of the stack in conjunction with the duration. duration/interval = size size takes memory so try to keep it small enough.");
            Interval.SettingChanged += (_, _) => ConfigUpdate();
            ConfigUpdate();
        }

        private void ConfigUpdate()
        {
            if (EquipmentDef != null)
                EquipmentDef.cooldown = Cooldown!.Value;
            BrokenClockBehaviour.StackDuration = Duration!.Value;
            BrokenClockBehaviour.KeyframeInterval = Interval!.Value;
        }

        protected override void PostEquipmentDef()
        {
            base.PostEquipmentDef();
            ConfigUpdate();
        }

        
        public override void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> scalingFunctions)
        {
            base.MakeInLobbyConfig(scalingFunctions);

            var general = scalingFunctions[ConfigCategoriesEnum.General];

            general.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(Cooldown));
            general.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(Duration));
            general.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(Interval));
        }
    }
    
    public class BrokenClockBehaviour : MonoBehaviour
    {
        public static float StackDuration = 10f;
        public static float KeyframeInterval = 0.25f;

        private float _keyframeStopwatch;
        public bool reversing;

        private CharacterMaster? _master;
        private CharacterBody? _body;
        private CharacterBody? Body
        {
            get
            {
                if (!_master) return null;
                var body = _master!.GetBody();
                if (body)
                    return _body ??= body!;
                return null;
            }
        }

        private HealthComponent? _healthComponent;
        // ReSharper disable twice Unity.NoNullPropagation
        private HealthComponent? HealthComponent => _healthComponent ??= Body?.GetComponent<HealthComponent>();

        private CharacterMotor? _characterMotor;
        private CharacterMotor? CharacterMotor => _characterMotor ??= Body?.GetComponent<CharacterMotor>();

        public readonly DropoutStack<BrokenClockKeyframe> DropoutStack = new(Mathf.RoundToInt(StackDuration/KeyframeInterval));
        
        public bool ToggleReversing()
        {
            reversing = !reversing;
            if (!Body)
            {
                return false;
            }

            AkSoundEngine.PostEvent("BrokenClock_Break", Body!.gameObject);
            if (!reversing) return true;
            AkSoundEngine.PostEvent("BrokenClock_Start", Body.gameObject);
            if (!Body.hasEffectiveAuthority) return true;

            //AddOneStock(); // TODO does not work on clients, probably because we arent reaching here because its only ran on server?
            _previousKeyframe = MakeKeyframe();
            _currentTargetKeyframe = DropoutStack.Pop();
            return false;
        }

        public void Awake()
        {
            _master = GetComponent<CharacterMaster>();
            _master.onBodyDeath.AddListener(OnDeath);
        }

        public void OnDeath()
        {
            if (Body is not null) AkSoundEngine.PostEvent("BrokenClock_Break", Body.gameObject);
            reversing = false;
        }

        public void FixedUpdate()
        {
            if (!Body) return;
            if (!Body!.hasEffectiveAuthority) return;
            if (reversing)
            {
                DoReverseBehaviour();
            }
            else
            {
                _keyframeStopwatch += Time.fixedDeltaTime;
                if (_keyframeStopwatch < KeyframeInterval) return;
                _keyframeStopwatch -= KeyframeInterval;
                var frame = MakeKeyframe();
                if (frame is null) return;
                DropoutStack.Push((BrokenClockKeyframe) frame);
            }
        }

        private BrokenClockKeyframe? MakeKeyframe()
        {
            var keyframe = new BrokenClockKeyframe();
            if (HealthComponent is null) return null;

            keyframe.Health = HealthComponent.health;
            keyframe.Barrier = HealthComponent.barrier;
            keyframe.Shield = HealthComponent.shield;

            if (!CharacterMotor) return keyframe;
            keyframe.Position = CharacterMotor!.transform.position;
            keyframe.Velocity = (CharacterMotor as IPhysMotor).velocity;
            
            //keyframe.LookDir = body.inputBank.aimDirection; // TODO replace with CameraRigController.desiredCameraState.rotation;

            /*
            ArrayUtils.CloneTo(body.buffs, ref keyframe.Buffs);
            keyframe.TimedBuffs = body.timedBuffs.Select(x => (TimedBuff) x).ToList(); //new CharacterBody.TimedBuff {buffIndex = x.buffIndex, timer = x.timer}).ToList();
            */

            return keyframe;
        }

        private void ApplyKeyframe(BrokenClockKeyframe keyframe)
        {
            if (HealthComponent is null) return;

            HealthComponent.health = keyframe.Health;
            HealthComponent.barrier = keyframe.Barrier;
            HealthComponent.shield = keyframe.Shield;

            if (CharacterMotor is not null)
            {
                CharacterMotor.Motor
                    .MoveCharacter(keyframe
                        .Position); // This does not work as we are in the server scope right now and server cannot move client authoritive player.
                //CharacterMotor.velocity = keyframe.Velocity; thanks i hate it
                BrokenClock.Velocity.SetValue(CharacterMotor, keyframe.Velocity);
            }

            //characterMotor.velocity = Vector3.zero;
            //_lastVelocity = keyframe.Velocity;

            //body.inputBank.aimDirection = keyframe.LookDir;

            /*
            ArrayUtils.CloneTo(keyframe.Buffs, ref body.buffs);
            body.timedBuffs = keyframe.TimedBuffs.Select(x => (CharacterBody.TimedBuff) x).ToList(); //new CharacterBody.TimedBuff {buffIndex = x.buffIndex, timer = x.timer}).ToList();
            */
        }

        private BrokenClockKeyframe? _previousKeyframe;
        private BrokenClockKeyframe _currentTargetKeyframe;
        private float _ratio;

        private void DoReverseBehaviour()
        {
            var any = DropoutStack.Any(); 
            if (!any || !Body)
            {
                reversing = false;
                if (Body is null) return;
                AkSoundEngine.PostEvent("BrokenClock_Break", Body.gameObject);
                byte i = 0;
                foreach (var equipmentState in Body.inventory.equipmentStateSlots)
                {
                    if (equipmentState.equipmentDef == BrokenClock.Instance!.EquipmentDef)
                        Body.inventory.DeductEquipmentCharges(i, 1);
                    i++;
                }

                //characterMotor.velocity = _lastVelocity;
                return;
            }
            
            /*
            var currentKeyframe = MakeKeyframe(); // Probably dont need to do this any longer with the storing of the old frame
            var speed = any ? dropoutStack.Average(x => x.Velocity.magnitude) : 1f;
            if (_currentTargetKeyframe.Equals(currentKeyframe)) // maybe add a timer that gets reset on pop, which basically prevents you from being stuck on a timeframe for too long
                _currentTargetKeyframe = dropoutStack.Pop(); 
            currentKeyframe.LerpFrom(_currentTargetKeyframe, speed * 5f);
            */

            _ratio += Time.fixedDeltaTime;
            if (_ratio > KeyframeInterval)
            {
                _ratio = 0f;
                _previousKeyframe = _currentTargetKeyframe;
                _currentTargetKeyframe = DropoutStack.Pop();
            }

            if (_previousKeyframe == null) return;
            var currentKeyframe = BrokenClockKeyframe.Lerp((BrokenClockKeyframe) _previousKeyframe, _currentTargetKeyframe, _ratio/KeyframeInterval);
            ApplyKeyframe(currentKeyframe);
        }
    }
    
    public struct BrokenClockKeyframe// : IEquatable<BrokenClockKeyframe>
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 LookDir;
        public float Health;
        public float Shield;
        public float Barrier;

        /*
        public int[] Buffs;
        public List<TimedBuff> TimedBuffs;
        */

        public void MoveTowards(BrokenClockKeyframe target, float rate)
        {
            var time = Time.fixedDeltaTime * rate;
            Position = Vector3.MoveTowards(Position, target.Position, time);
            //Velocity = Vector3.MoveTowards(Velocity, target.Velocity, time);
            Velocity = target.Velocity;
            LookDir = Vector3.MoveTowards(LookDir, target.LookDir, time);

            Health = Mathf.MoveTowards(Health, target.Health, time);
            Shield = Mathf.MoveTowards(Shield, target.Shield, time);
            Barrier = Mathf.MoveTowards(Barrier, target.Barrier, time);
            
            /*
            ArrayUtils.CloneTo(target.Buffs, ref Buffs);
            
            foreach (var targetTimedBuff in target.TimedBuffs)
            {
                if (TimedBuffs.Any(x => x.BuffIndex == targetTimedBuff.BuffIndex)))
                {
                    
                }
            }*/
        }

        public static BrokenClockKeyframe Lerp(BrokenClockKeyframe from, BrokenClockKeyframe to, float v)
        {
            var keyframe = new BrokenClockKeyframe
            {
                Position = Vector3.Lerp(from.Position, to.Position, v),
                Velocity = Vector3.Lerp(from.Velocity, to.Velocity, v),
                LookDir = Vector3.Lerp(from.LookDir, to.LookDir, v),
                Health = Mathf.Lerp(from.Health, to.Health, v),
                Shield = Mathf.Lerp(from.Shield, to.Shield, v),
                Barrier = Mathf.Lerp(from.Barrier, to.Barrier, v)
            };
            return keyframe;
        }

        /*
        private bool _increasing;
        private float _oldDist;
        public bool Equals(BrokenClockKeyframe other)
        {
            const float tolerance = 0.01f;
            var dist = Vector3.SqrMagnitude(Position - other.Position);
            Debug.Log(dist);
            if (Math.Abs(dist - _oldDist) < 0.1f)
            {
                Debug.Log("Failed rate of change: " + (dist - _oldDist));
                return true;
            }
            _oldDist = dist;
            return dist < 5f;  //Mathf.Min(Velocity.sqrMagnitude, 100f) + 1f;
        }*/
    }

    /*
    public struct TimedBuff // Require my own datatype so i can easily lerp the current keyframe as structs are kept by copy instead of reference
    {
        public BuffIndex BuffIndex;
        public float Timer;

        public static explicit operator TimedBuff(CharacterBody.TimedBuff x)
        {
            return new TimedBuff
            {
                BuffIndex = x.buffIndex,
                Timer = x.timer
            };
        }

        public static explicit operator CharacterBody.TimedBuff(TimedBuff x)
        {
            return new CharacterBody.TimedBuff
            {
                buffIndex = x.BuffIndex,
                timer = x.Timer
            };
        }
    }
    */
}