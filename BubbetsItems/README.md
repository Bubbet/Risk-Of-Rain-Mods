# Bubbet's Items
Yet another item mod that adds all sorts of unique items, including Void Tiers. There are highly configurable item stackings in the configs too. This mod is still being worked on and will of course add more and more items. You must have `SOTV` expansion in order to access the Void tier items.

# Items Added

Items | Function | Type
---|---:|---
Jellied Soles | Reduces fall damage and add the reduced damage to your next attack | `Common` |
---|---|---|
Torturer | Heal from inflicted damage over time | `Uncommon` |
Abundant Hourglass | Duration of buffs are increased | `Uncommon` |
Escape Plan | Increases movement speed the closer you are to death | `Uncommon` |
Bunny Foot | Source bhopping in a item | `Uncommon` |
---|---|---|
Repulsion Armor Mk2 | Two modes in config, gives extra reduction or armor per Repulsion Plate | `Legendary` |
Acid Soaked Blindfold | Spawn a blind vermin ally with some items | `Legendary` |
---|---|---|
Void Scrap | Prioritized when used with Common 3D Printers. Corrupts all Broken items | `Void Common` |
Shifted Quartz | Deal bonus damage if there aren't nearby enemies. Corrupts all Focus Crystals | `Void Common` |
Recursion Bullets | Attacking bosses increases attack speed. Corrupts all Armor-Piercing Rounds | `Void Common` |
Scintillating Jet | Reduce damage temporarily after getting hit. Corrupts all oddly shaped opal | `Void Common` |
Adrenaline Sprout | Increase regen while in combat. Corrupts all cautious slug | `Void Common` |
Zealotry Embrace | Deal more damage to enemies with barely any debuffs inflicted. Corrupts all Death Marks | `Void Uncommon` |
---|---|---|
Broken Clock | Turn back the clock 10 seconds for yourself | `Equipment` |
Wildlife Camera | Take a photo of an enemy, and spawn them as an ally later | `Equipment` |

# Other
If you have suggestions or questions, feel free to message `Bubbet#2639` or `GEMO#0176` on Discord.

Mod is still being continuously worked on!
Patience is key~

- Special Thanks to..
  - `GEMO#0176` for making all the newer item models
  - `SOM#0001` for doing most of the icons

# Changelog
* v1.6.2 Mod Changes :
  * Made jellied soles store the damage in the master, so when you change scene it still remembers
  * Added sound effects and damage color for jellied soles
  * Gave Zealotry Embrace void damage color
  * Made logbook patch more robust
  * Changed the orb inside voidscrap to not cause flashing when looking at it
  * Made acid soaked blindfold blacklisted for ai
  
* v1.6.1 Mod Changes :
  * Fixed bunny foot crashing when given to something that can fly
  * Fixed crash in dedicated servers
  * Fixed shifted quartz indicator being disjointed as a client

* v1.6.0 Mod Changes :
  * Fixed cooldown for broken clock
  * Ported bunny foot to this mod from my other depreciated mod.
  * Added new red item Acid Soaked Blindfold, spawn a blind vermin ally with random items.   
  * Added new void tier 1 item Scintillating Jet, consumes opals by default
  * Added new void tier 1 item Adrenaline Sprout, consumes cautious slug by default
  * Made the indicator for enemies being inside shifted quartz more useful
  * Buffed shifted quartz from 20m to 18m
  * Added two new config entiries for zealotry embrace, only track your dots, and dots only count for one stack
  * Fixed wildlife cameras effect not working
  * Made camera copy loadout, giving the captured guys the stage variants
  * Made camera copy previous elite effect
  * Jellied soles now stores the damage you would have taken and you use it in your next attack

* v1.5.5 Mod Changes :
  * Exposed the void pairings as a config value
  * Added buff blacklist for hourglass as a config value
  
* v1.5.4 Mod Changes :
  * Re-enabled description in tooltip for my items. Did way too many things last patch and forgot it disabled from when i was debugging
  
* v1.5.3 Mod Changes :
  * Lowered the volume of the camera shutter from wildlife camera by 7(DB?)
  * Fixed tooltips with newer versions of BetterUI
  * Made Escape Plans percent accurate to the amount of health you have in the tooltip.
  * Reworked the escape plans buff to update more when healing.  
  * Fixed bug from scaling function refactor that didnt take into account the amount of item you had in tooltip.
  * Fixed torturer having a poor outline
  * Fixed incompatibility with other mods using SystemInitializer that load after this. (Skills++)

* v1.5.2 Mod Changes :
  * Adjusted all item descriptions
  * Fixed Fuel Array Description
  * Fixed broken clock not firing the cooldown when running the timer to empty

* v1.5.1 Mod Changes :
  * Fixed Shifted Quartz indicator being way too large.
  * Changed zealotry's default debuff count to 3.

* v1.5.0 Mod Changes, GEMO Update _(I'm honored!)_ :
  * 4 new items all modeled by `GEMO#0176`.
  * Jellied Soles, White Item - Reduces fall damage.
  * Recursion Bullets, Void Tier 1 - Increase attack speed for hitting bosses.
  * Shifted Quartz, Void Tier 1 - Damage bonus for having no enemies near you.
  * Zealotry Embrace, Void Tier 2 - Damage bonus on hitting enemies without debuffs on them.  
  * Reworked scaling configs to support multiple per item.
  * Added config for disabling the scaling configs in the tooltip,
  * Disabled Graph in logbook for now,

* v1.4.4 Mod Changes :
  * Fixed disabled items hooks still running often ending up with an NRE when trying to reference `itemdef` that isn't set.

* v1.4.3 Mod Changes :
  * Fixed Void Scrap icon
  * Fixed Void Scraps cost/afford delegates so it works with alchemical cauldrons now.

* v1.4.2 Mod Changes :
  * Fixed bug caused by update to BepinExPack causing system initializers to not work, tokens and sounds not loading.
  * Higher resolution Void Scrap icon.

* v1.4.1 Mod Changes :
  * Fixed Void Scrap not counting as Tier1 scrap, but instead counting as VoidTier1 and not doing anything.
  * Made it priority scrap.

* v1.4.0, Mod Changes, Void Scrap Update :
  * Added new item, Void Scrap
  * Re-added InLobbyConfig support
  * Fixed the invert on WildLife Cameras can do bosses config.

* v1.3.1 Mod Changes :
  * Fixed items/equipment not listening to config file in respect to being disabled.
  * Fixed tokens not being initialized.

* v1.3.0 Mod Changes, CUM2 Update :
  * Updated to support `SOTV` update.
  * temporarily dropped support for Atherium and InLobbyConfig

* v1.2.5 Mod Changes :
  * Added hurtbox requirement to the camera as its needed to get reference to the master anyways.
  * Changed camera to refund a stock if you miss the capture, making it actually work with gesture.

* v1.2.4 Mod Changes :
  * Changed WildLife Camera set equipment to different method so ArtificerExtended does not throw NRE.

* v1.2.3 Mod Changes :
  * Fixed WildLife Camera firing in the wrong scope, causing it to not work. Was on client authoritive, it's now on server.

* v1.2.2 Mod Changes :
  * Re-added `NCalc.Dll` to the zip because somehow that got removed.

* v1.2.1 Mod Changes :
  * Fixed `equipmentbase` only firing `performequipment` on server, it now fires for everyone.
  * Added support for indicators to the `equipmentbase`
  * Added an indicator to Wildlife Camera.
  * Broken Clock now has a much longer cooldown by default and refills a stock when turning on rewind.
  * Sounds fixed for both Broken Clock and Wildlife Camera _`(Problem came from performequipment)`_
  * Broken Clock stops its looping sound upon character death.

* v1.2.0 Mod Changes, Wildlife Camera Update :
  * Added the Wildlife Camera, an equipment to spawn an ally from an enemy.
  * Added sounds to current Equipment.
  * Changed the tooltips to be a bit more descriptive for items.

* v1.1.2 Mod Changes :
  * Updated Escape Plan's automatic tooltip value to reflect your health.

* v1.1.1 Mod Changes :
  * Made InlobbyConfig actually optional.
  * Fixed Escape Plan hardly working.

* v1.1.0 Mod Changes :
  * New Equipment: Broken Clock, rewinds yourself on use.
  * Each item has fully configurable scaling.
  * Support for in lobby config.
  * Scaling graph for items in the logbook, sometimes janky.
  * Tooltips dynamically update to display configuration changes, and stacking values.

* v1.0.0 Mod Changes :
  * Mod release
  * First 4 Items + Bandolier as an orb instead of pickup.