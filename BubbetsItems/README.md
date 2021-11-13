Repulsion Armor Mk2
- Red Item
- Has two modes, configurable via config.
- Primary mode acts similar to mk1 armor plate but gives extra reduction per mk1 plate.
- Secondary mode gives armor, and armor per mk1.

Escape Plan
- Green Item
- The lower your health the faster you move.

Torturer
- Green Item
- Heal for damage you deal via damage over time.

Abundant Hourglass
- Green Item
- Increases the duration of timed buffs.

Broken Clock
- Equipment
- Rewinds time, rolling back health, shield, barrier, position and velocity.
- Will later also rewind look position and potentially but unlikely skill usages.

Wildlife Camera
- Equipment
- Take a photo of a enemy storing them in your camera.
- Deploy that stored enemy as your friend.

1.0.0 - Initial Release
- First 4 items + bandolier as an orb instead of pickup.

1.1.0 - Broken Clock Update
- Added new equipment: Broken clock. Rewinds time upon use.
- Made every item have a fully configurable scaling function in the config.
- Added support for in lobby config.
- Added a scaling graph for the items to the logbook, is a bit jank with some items.
- Tooltips dynamically update to display the scaling functions values, and what value it is currently at given your inventory.

1.1.1
- Made in lobby config actually optional.
- Fixed escape plan hardly working.

1.1.2
- Updated escape plans automatic tooltip value to reflect your health.

1.2.0 Wildlife Camera Update
- Added the wildlife camera, a equipment.
- Added sounds to broken clock and wildlife camera.
- Changed the tooltips to be a bit more descriptive for items.

1.2.1
- Fixed equipmentbase only firing performequipment on server, it now fires for everyone
- Added support for indicators to the equipmentbase
- Added an indicator to wildlife camera
- Broken Clock now has a much longer cooldown by default and refills a stock when turning on rewind
- Sounds fixed for both broken clock and wildlife camera (problem came from performequipment)
- Broken clock stops its looping sound upon character death.

1.2.2
- Re-Added NCalc.Dll to the zip because somehow that got removed.

1.2.3
- Fixed wildlifecamera firing in the wrong scope, causing it to not work. Was on client authoritive, is now on server.

1.2.4
- Changed WildLifeCamera set equipment to different method so ArtificerExtended does not throw nre.