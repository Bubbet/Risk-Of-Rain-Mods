using System.Collections.Generic;

namespace Titanfall2Mod.SkillGeneration
{
    public static partial class SkillGenerator
    {
        public static readonly Dictionary<string, string[]> skillTypes = new Dictionary<string, string[]>
        {
            {
                "TITAN", new[]
                {
                    "Ion - Titan flexible enough to fulfill many roles.\nAbilities share energy resource.\nDifficulty: *",
                    "Scorch - Control enemy movement and stack damage with a heavy Titan fire-based arsenal.\nDifficulty: ***",
                    "Northstar - Long-range sniper class Titan that can hover over the battlefield.\nDifficulty: **",
                    "Ronin - Close range hit-and-run style attacks and broadsword melee expertise.\nDifficulty: ***",
                    "Tone - Mid-range combo damage using lock-on tracking ordnance.\nDifficulty: **",
                    "Legion - Frontline devastation and area lockdown by means of relentless damage output.\nDifficulty: **",
                    "Monarch - Mid-range Vanguard-based Titan that can upgrade itself on the battlefield.\nDifficulty: ***",
                }
            },

            {
                "TITANKIT", new[]
                {
                    "Assault Chip - Improves Auto-Titan precision and enables the use of offensive and utility abilities. ie without it auto titan only left clicks",
                    "Stealth Auto-Eject - Automatically eject and cloak when your Titan is doomed, preventing Pilot death.",
                    "Turbo Engine - <style=cIsDamage>Ronin, Northstar, Monarch, Ion & Tone:</style> 1 extra dash.\n<style=cIsDamage>Scorch & Legion:</style> Reduced dash cooldown.",
                    "Overcore - Titan Core always starts with 20% build time.",
                    "Nuclear Ejection - Ejecting while doomed causes your Titan to detonate its core causing nearby enemies massive damage.",
                    "Counter Ready - 1 extra Electric Smoke countermeasure.",
                }
            },

            {
                "TITANSPECIFICKIT_ION", new[]
                {
                    "Entangled Energy - Splitter Rifle critical hits restore energy.",
                    "Zero-Point Tripwire - Tripwire deployment uses zero energy.",
                    "Vortex Amplifier - Increases Vortex Shield's return damage output by 35%.",
                    "Grand Cannon - Laser Core lasts longer.",
                    "Refraction Lens - Splitter Rifle splits 5 ways.",
                }
            },

            {
                "TITANSPECIFICKIT_SCORCH", new[]
                {
                    "Wildfire Launcher - Increased direct damage and thermite from the T-203 Thermite Launcher.",
                    "Tempered Plating - Scorch is immune to crits and his own thermite damage.",
                    "Inferno Shield - Increased damage and duration for Thermal Shield.",
                    "Fuel for the Fire - Firewall's cooldown is reduced by 2 seconds.",
                    "Scorched Earth - Flame Core ignites the ground, leaving thermite in its wake.",
                }
            },

            {
                "TITANSPECIFICKIT_NORTHSTAR", new[]
                {
                    "Piercing Shot - Plasma Railgun round fire through targets.",
                    "Enhanced Payload - Cluster Missile's secondary explosions hit a larger range and last longer.",
                    "Twin Traps - Tether Trap fires two traps.",
                    "Viper Thrusters - Move faster during Hover and Flight Core.",
                    "Threat Optics - Enemies are highlighted while zooming in.",
                }
            },

            {
                "TITANSPECIFICKIT_RONIN", new[]
                {
                    "Ricochet Rounds - The Leadwall's round bounce off surfaces.",
                    "Thunderstorm - Arc Wave has two charges.",
                    "Temporal Anomaly - Phase Dash is available more often(shorter cooldown).",
                    "Highlander - Titan kills extend the duration of Sword Core.",
                    "Phase Reflex - When doomed, Ronin phases out of danger.",
                }
            },

            {
                "TITANSPECIFICKIT_TONE", new[]
                {
                    "Enhanced Tracker Rounds - Critical hits apply 2 tracker marks on targets.",
                    "Reinforced Particle Wall - Particle Wall lasts longer and blocks more damage.",
                    "Pulse-Echo - After a short delay, Sonar Pulse echoes a second pulse.",
                    "Rocket Barrage - Tracker Rockets fire 2 additional missiles.",
                    "Burst Loader - Aiming allows the 40mm to store up to 3 shots to burst fire.",
                }
            },

            {
                "TITANSPECIFICKIT_LEGION", new[]
                {
                    "Enhanced Ammo Capacity - Increases the ammo capacity of the Predator Cannon.",
                    "Sensor Array - Smart Core lasts longer.",
                    "Bulwark - Gun Shield blocks twice as much damage.",
                    "Light-Weight Alloys - Move faster while the Predator Cannon is spun up.",
                    "Hidden Compartment - Power Shot has two charges. Power Shot damage is reduced by 15%.",
                }
            },

            {
                "TITANSPECIFICKIT_MONARCH", new[]
                {
                    "Shield Amplifier - Energy Siphon's Shield gain is increased by 25%.",
                    "Energy Thief - Core Meter is earned 10% faster and Titan executions steal a Battery.",
                    "Rapid Rearm - Reduces the cooldown of Rearm by 5 seconds.",
                    "Survival of the Fittest - Batteries can repair Monarch out of Doomed State.",
                }
            },

            {
                "TITANLOADOUT_ION", new[]
                {
                    "Laser Core - Heavy, chest fired laser cannon.",
                    "Laser Shot - Precision shoulder laser.",
                    "Vortex Shield - Blocks and returns incoming fire.",
                    "Tripwire - Laser triggered explosive mines.",
                    "Splitter Rifle - Primary: Automatic energy rifle. Alt: Stronger split shot. *DRAINS ENERGY",
                }
            },

            {
                "TITANLOADOUT_SCORCH", new[]
                {
                    "Flame Core - Thermite shockwave that engulfs targets along its path.",
                    "Firewall - Fires a directed wall of thermite.",
                    "Thermal Shield - Melts incoming fire and burns nearby enemies.",
                    "Incendiary Trap - Fills an area with thermite-ignitable gas.",
                    "T-203 Thermite Launcher - Giant thermite grenades ignite the impact area.",
                }
            },

            {
                "TITANLOADOUT_NORTHSTAR", new[]
                {
                    "Flight Core - Hover, unleashing rockets at targets below.",
                    "Cluster Missile - Creates sustained explosions on impact.",
                    "Tether Trap - Mine that locks nearby enemy Titans down.",
                    "VTOL Hover - Vertical take-off hover.",
                    "Plasma Railgun - Sniper railgun that charges up while zoomed. Hold RMB to charge.",
                }
            },

            {
                "TITANLOADOUT_RONIN", new[]
                {
                    "Sword Core - Electrifies broadsword, empowering attacks and Sword Block.",
                    "Arc Wave - Slows and damages enemies.",
                    "Sword Block - Reduces damage from incoming fire.",
                    "Phase Dash - Quick, directional phase shift.",
                    "Leadwall - Projectile shotgun with a wide spread.",
                }
            },

            {
                "TITANLOADOUT_TONE", new[]
                {
                    "Salvo Core - Guided missiles that follow where Tone aims.",
                    "Tracking Rockets - Fires missiles at fully locked enemies. FULL LOCK-ON REQUIRED",
                    "Particle Wall - Force field blocks incoming fire on one side.",
                    "Sonar Lock - Reveals enemies in an area. GRANTS PARTIAL LOCK-ON",
                    "40mm Tracker Cannon - Semi-auto explosive rounds. GRANTS PARTIAL LOCK-ON",
                }
            },

            {
                "TITANLOADOUT_LEGION", new[]
                {
                    "Smart Core - Automatic smart lock-on to targets.",
                    "Power Shot - Close-Range: Knocks back nearby enemies. Long-Range: Damages all enemies in its path.",
                    "Gun Shield - Shield deployed around the Predator Cannon.",
                    "Mode Switch - Switch between close range and long-range precision rounds.",
                    "Predator Cannon - Powerful minigun with a long spin-up time.",
                }
            },

            {
                "TITANLOADOUT_MONARCH", new[]
                {
                    "Upgrade Core - Recharges your Titan's Shields and upgrades your Titan in order of the upgrades above.",
                    "Rocket Salvo - Launches an unguided rocket swarm.",
                    "Energy Siphon - Slows enemies and generates Shields. Heavily armored targets generate more Shield.",
                    "Rearm - Refreshes the cooldown of your Dash, Offensive, and Defensive Ability.",
                    "XO-16 - 20mm automatic rifle.",
                }
            },

            //TODO probably end up merging these together
            {
                "TITANLOADOUT_MONARCH_CORE", new[]
                {
                    "Arc Rounds - XO-16 rounds deal more damage to Shields and drain energy from Vortex and Thermal shields. Increases ammo capacity.",
                    "Missile Racks - Rocket Salvo fires twice the amount of missiles.",
                    "Energy Transfer - Hitting friendly Titans with Energy Siphon gives them Shield.",

                    "Rearm and Reload - Faster Reload and Rearm speed.",
                    "Maelstrom - Electric Smoke is intensified, dealing more damage to Titans and Pilots.",
                    "Energy Field - Energy Siphon affects a large area around the point of impact.",

                    "Multi-Target Missiles - Hold Rocket Salvo to lock onto heavily armored targets. Missiles deal more damage.",
                    "Superior Chassis - Upgrades Monarch's max Health and removes weak point vulnerabilities.",
                    "XO-16 Accelerator - Installs the Accelerator mod for the XO-16, increasing its max fire rate and damage.",
                }
            },

            {
                "PILOTBOOST", new[]
                {
                    "Amped Weapons - Temporarily increase damage for your primary and secondary weapons. 80%",
                    "Ticks - Spider-like drones actively seek out enemies before self-detonation. Comes with two charges. <style=cIsDamage>NOTE: Limit of 6 in inventory.</style> 65%",
                    "Pilot Sentry - Anti-Personnel automated turret. 1 minute life time. <style=cIsDamage>NOTE: Limit 3 in inventory.</style> 72%",
                    "Map Hack - Reveal enemies to your entire team. 70%",
                    "Battery Back-up - Give yourself a free battery. 80%",
                    "Radar Jammer - Scramble the enemy's RADAR. 40%",
                    "Titan Sentry - Anti-Titan automated turret. 1 minute life time. <style=cIsDamage>NOTE: Limit 3 in inventory.</style> 35%",
                    "Smart Pistol - Locks onto nearby targets for guaranteed hits. Two 12 round magazines. <style=cIsDamage>WARNING: Replaces weapon.</style> 60%",
                    "Phase Rewind - Phase Shift to a location visited shortly before activation. 25%",
                    "Hard Cover - Reinforced Pilot-sized particle shield. 20%",
                    "Holo Pilot Nova - Create multiple decoys of yourself. <style=cIsDamage>NOTE: Limit 3 in inventory.</style> 40%",
                    "Dice Roll - A randomly chosen Boost is activated on use. 50%",
                }
            },

            {
                "PILOTKIT", new[]
                {
                    "Cloak - Become nearly invisible. Cloak has increased effectiveness vs. Titans.",
                    "Pulse Blade - Expose enemies through all surfaces with this sonar pulse-emitting throwing knife.",
                    "Grapple - Grapple Hook for getting to out of reach places quickly.\n\nStrategic jumping enhances its effectiveness.",
                    "Stim - Quickly heals and boosts your speed for a short time.",
                    "A-Wall - Pilot sized particle shield that amps outgoing shots.",
                    "Phase Shift - Teleport into an alternate space for a short time.",
                    "Holo Pilot - Create a holographic copy of yourself mimicking your actions when activated.",
                }
            },

            {
                "PILOTORDANANCE", new[]
                {
                    "Frag Grenade - Cookable explosive ordnance.",
                    "Arc Grenade - Stuns Pilots and blinds Titans.",
                    "Firestar - Incendiary throwing star.",
                    "Gravity Star - Pulls in enemies and projectiles before exploding.",
                    "Electric Smoke Grenade - Carpets an area with electric smoke.",
                    "Satchel - Two remotely detonated heavy explosives.",
                }
            },

            {
                "PILOTWEAPON", new[]
                {
                    "G2A5 - Semi-auto precision rifle.",
                    "CAR - Consistent recoil SMG.",
                    "X-55 Devotion - Ramps up fire rate over time.",
                    "Kraber-AP Sniper - Scoped heavy rifle.",
                    "Mastiff - Auto-loading shotgun with wide spread.",
                    "EPG-1 - Single fire, direct energy propelled launcher.",
                    "SA-3 Mozambique - Controlled spread triple barrel shotgun pistol.",
                }
            }
        };
    }
}