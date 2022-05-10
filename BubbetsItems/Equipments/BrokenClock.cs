using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using InLobbyConfig;
using InLobbyConfig.Fields;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BubbetsItems.Equipments
{
    public class BrokenClock : EquipmentBase
    {
        private ConfigEntry<float> cooldown;
        public static ConfigEntry<float> duration;
        public static ConfigEntry<float> interval;

        public static FieldInfo velocity = typeof(CharacterMotor).GetField("velocity");
        public static BrokenClock instance;

        public BrokenClock()
        {
            instance = this;
        }

        protected override void MakeTokens()
        {
            base.MakeTokens();
            AddToken("BROKEN_CLOCK_NAME", "Broken Clock");
            AddToken("BROKEN_CLOCK_PICKUP", "Turn back the clock " + "{0} seconds ".Style(StyleEnum.Utility) + "for yourself.");
            AddToken("BROKEN_CLOCK_DESC", "Turn back the clock " + "{0} seconds ".Style(StyleEnum.Utility) + "to revert " + "health ".Style(StyleEnum.Heal) + "and " + "velocity".Style(StyleEnum.Utility) + ".");
            AddToken("BROKEN_CLOCK_LORE", "Broken clock lore.");
        }

        public override string GetFormattedDescription(Inventory inventory = null, string? token = null, bool forceHideExtended = false)
        {
            return Language.GetStringFormatted(EquipmentDef.descriptionToken, duration.Value);
        }

        public override EquipmentActivationState PerformEquipment(EquipmentSlot equipmentSlot)
        {
            base.PerformEquipment(equipmentSlot);
            ConfigUpdate();
            return equipmentSlot.inventory.GetComponent<BrokenClockBehaviour>().ToggleReversing(); // This only matters on the server and all the data is tracked on the authority character
        }

        public override void PerformClientAction(EquipmentSlot equipmentSlot, EquipmentActivationState state)
        {
            base.PerformClientAction(equipmentSlot, state);
            equipmentSlot.inventory.GetComponent<BrokenClockBehaviour>().PlaySounds(state);
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
            cooldown = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Broken Clock Cooldown", 60f, "Broken Clock equipment cooldown.", 5f);
            cooldown.SettingChanged += (_, _) => ConfigUpdate();
            duration = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Broken Clock Buffer Duration", 10f, "Duration of time to store in the broken clock.");
            duration.SettingChanged += (_, _) => ConfigUpdate();
            interval = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Broken Clock Keyframe Interval", 0.25f, "How often to capture a keyframe and store it. Also determines the size of the stack in conjunction with the duration. duration/interval = size size takes memory so try to keep it small enough.");
            interval.SettingChanged += (_, _) => ConfigUpdate();
            ConfigUpdate(); // TODO make risk of options support
        }

        private void ConfigUpdate()
        {
            if (EquipmentDef != null)
                EquipmentDef.cooldown = cooldown.Value;
            BrokenClockBehaviour.stackDuration = duration.Value;
            BrokenClockBehaviour.keyframeInterval = interval.Value;
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

            general.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(cooldown));
            general.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(duration));
            general.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(interval));
        }
    }

    public class BrokenClockBehaviour : MonoBehaviour
    {
        public static float stackDuration = 10f;
        public static float keyframeInterval = 0.25f;

        private float _keyframeStopwatch;
        public bool reversing;

        private CharacterMaster _master;
        private CharacterBody _body;
        private CharacterBody Body => _body ? _body : _body = _master.GetBody();
        
        private HealthComponent _healthComponent;
        // ReSharper disable twice Unity.NoNullPropagation
        private HealthComponent HealthComponent => _healthComponent ? _healthComponent : _healthComponent = Body?.GetComponent<HealthComponent>();

        private CharacterMotor _characterMotor;
        private CharacterMotor CharacterMotor => _characterMotor ? _characterMotor : (_characterMotor = Body?.GetComponent<CharacterMotor>());

        public DropoutStack<BrokenClockKeyframe> dropoutStack = new DropoutStack<BrokenClockKeyframe>(Mathf.RoundToInt(stackDuration/keyframeInterval));

        public void PlaySounds(EquipmentBase.EquipmentActivationState state)
        {
            AkSoundEngine.PostEvent("BrokenClock_Break", Body.gameObject);
            if (state == EquipmentBase.EquipmentActivationState.DontConsume)
            {
                AkSoundEngine.PostEvent("BrokenClock_Start", Body.gameObject);
                if (!_body.hasEffectiveAuthority) return;
                reversing = true;
                _previousKeyframe = MakeKeyframe();
                _currentTargetKeyframe = dropoutStack.Pop();
            }

            if (state == EquipmentBase.EquipmentActivationState.ConsumeStock && _body.hasEffectiveAuthority)
            {
                reversing = false;
            }
        }
        
        public EquipmentBase.EquipmentActivationState ToggleReversing()
        {
            reversing = !reversing;
            return reversing ? EquipmentBase.EquipmentActivationState.DontConsume : EquipmentBase.EquipmentActivationState.ConsumeStock;
        }

        public void Awake()
        {
            _master = GetComponent<CharacterMaster>();
            _master.onBodyDeath.AddListener(OnDeath);
            Stage.onStageStartGlobal += StageStart;
        }

        private void OnDestroy()
        {
            Stage.onStageStartGlobal -= StageStart;
        }

        private void StageStart(Stage obj)
        {
            dropoutStack.Clear();
        }

        public void OnDeath()
        {
            AkSoundEngine.PostEvent("BrokenClock_Break", Body.gameObject);
            reversing = false;
        }

        public void FixedUpdate()
        {
            if (!Body) return;
            if (!Body.hasEffectiveAuthority) return;
            if (reversing)
            {
                DoReverseBehaviour();
            }
            else
            {
                _keyframeStopwatch += Time.fixedDeltaTime;
                if (_keyframeStopwatch < keyframeInterval) return;
                _keyframeStopwatch -= keyframeInterval;
                dropoutStack.Push(MakeKeyframe());
            }
        }
        
        private void AddOneStock()
        {
            var slot = Body.inventory.activeEquipmentSlot;
            var equipmentState = Body.inventory.GetEquipment(slot);
            Body.inventory.SetEquipment(new EquipmentState(equipmentState.equipmentIndex, equipmentState.chargeFinishTime, (byte) (equipmentState.charges + 1)), slot);
        }

        private BrokenClockKeyframe MakeKeyframe()
        {
            var keyframe = new BrokenClockKeyframe();
            keyframe.Health = HealthComponent.health;
            keyframe.Barrier = HealthComponent.barrier;
            keyframe.Shield = HealthComponent.shield;
            if (!CharacterMotor) return keyframe;
            keyframe.Position = CharacterMotor.transform.position;
            keyframe.Velocity = (CharacterMotor as IPhysMotor).velocity;

            //keyframe.LookDir = something.GetComponent<CameraRigController>().currentCameraState.rotation; //Body.inputBank.aimDirection; // TODO replace with CameraRigController.desiredCameraState.rotation;

            /*
            ArrayUtils.CloneTo(body.buffs, ref keyframe.Buffs);
            keyframe.TimedBuffs = body.timedBuffs.Select(x => (TimedBuff) x).ToList(); //new CharacterBody.TimedBuff {buffIndex = x.buffIndex, timer = x.timer}).ToList();
            */

            return keyframe;
        }

        private void ApplyKeyframe(BrokenClockKeyframe keyframe)
        {
            if (keyframe.Equals(default)) return;
            HealthComponent.health = keyframe.Health;
            HealthComponent.barrier = keyframe.Barrier;
            HealthComponent.shield = keyframe.Shield;

            CharacterMotor.Motor.MoveCharacter(keyframe.Position); // This does not work as we are in the server scope right now and server cannot move client authoritive player.
            //CharacterMotor.velocity = keyframe.Velocity; thanks i hate it
            BrokenClock.velocity.SetValue(CharacterMotor, keyframe.Velocity);
            
            //characterMotor.velocity = Vector3.zero;
            //_lastVelocity = keyframe.Velocity;

            //var controller = something.GetComponent<CameraRigController>();
            //var state = controller.currentCameraState
            //state.rotation = keyframe.LookDir;
            //controller.SetCameraState(state);
            //Body.inputBank.aimDirection = keyframe.LookDir;

            /*
            ArrayUtils.CloneTo(keyframe.Buffs, ref body.buffs);
            body.timedBuffs = keyframe.TimedBuffs.Select(x => (CharacterBody.TimedBuff) x).ToList(); //new CharacterBody.TimedBuff {buffIndex = x.buffIndex, timer = x.timer}).ToList();
            */
        }

        private BrokenClockKeyframe _previousKeyframe;
        private BrokenClockKeyframe _currentTargetKeyframe;
        private float ratio;

        private void DoReverseBehaviour()
        {
            var any = dropoutStack.Any(); 
            if (!any || !Body)
            {
                reversing = false;
                AkSoundEngine.PostEvent("BrokenClock_Break", Body.gameObject);
                byte i = 0;
                foreach (var equipmentState in Body.inventory.equipmentStateSlots)
                {
                    if (equipmentState.equipmentDef == BrokenClock.instance.EquipmentDef)
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

            ratio += Time.fixedDeltaTime;
            if (ratio > keyframeInterval)
            {
                ratio = 0f;
                _previousKeyframe = _currentTargetKeyframe;
                _currentTargetKeyframe = dropoutStack.Pop();
            }
            
            var currentKeyframe = BrokenClockKeyframe.Lerp(_previousKeyframe, _currentTargetKeyframe, ratio/keyframeInterval);
            ApplyKeyframe(currentKeyframe);
        }
    }
    
    public struct BrokenClockKeyframe : IEquatable<BrokenClockKeyframe>
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion LookDir;
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
            //LookDir = Vector3.MoveTowards(LookDir, target.LookDir, time);

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
                //LookDir = Vector3.Lerp(from.LookDir, to.LookDir, v),
                LookDir = Quaternion.Lerp(from.LookDir, to.LookDir, v),
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
        private const float TOLERANCE = 0.01f;
        public bool Equals(BrokenClockKeyframe other)
        {
            return Position == other.Position && 
                   Velocity == other.Velocity &&
                   Math.Abs(Health - other.Health) < TOLERANCE && 
                   Math.Abs(Shield - other.Shield) < TOLERANCE && 
                   Math.Abs(Barrier - other.Barrier) < TOLERANCE;
        }

        public override bool Equals(object? obj)
        {
            return obj is BrokenClockKeyframe other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Velocity.GetHashCode();
                hashCode = (hashCode * 397) ^ LookDir.GetHashCode();
                hashCode = (hashCode * 397) ^ Health.GetHashCode();
                hashCode = (hashCode * 397) ^ Shield.GetHashCode();
                hashCode = (hashCode * 397) ^ Barrier.GetHashCode();
                return hashCode;
            }
        }
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