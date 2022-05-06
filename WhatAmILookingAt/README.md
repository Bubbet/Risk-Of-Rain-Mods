1.0.0 - Initial Release
- Support for Items, Equipment, Skills, Unlockables, and Artifacts
- Support for all of the above through r2api mods

1.0.1
- Fixed r2api being optional dependency
- Fixed bug with betterui in the loadout screen for skills

1.0.2
- Wrote catch for duplicate content pack identifiers

1.0.3
- Fixed bug that made every skill appear as one mod if that mod added a skill without a description token

1.1.0 - BetterAPI Support
- Added support for BetterAPI Items
- Added support for BetterUI's buffInfos

1.1.1
- Support for BetterAPI 2.0
- Made that support actually optional

1.2.0
- Support for DLC 1
- Probably support for r2api when that comes too

1.3.0
- Initial Tiler2 Support

1.3.1
- Fixed nre in tiler2 support

1.3.2
- Dropped support for tiler2 because it was somehow breaking systemintializers and i dont care enough to find out why

1.4.0
- Added in world support for waila
- You can now look at interactables, shops for which item is inside, enemies, players, and scenes to figure out where they are from.

1.4.1
- Fixed shops that are ? showing what they actually are
- Fixed some maps not being found
- Changed the tips when looking at duplicators and shops to be better

1.4.2
- Fixed nre in pickup check, i should not be programming when im this tired

1.4.3
- Fixed some vanilla things not being found
- Added config to disable in world, hide it so it only shows when tab is pressed, or have it always on (default)

1.4.4
- Fixed more vanilla things not found
- Added config to disable only scene display to tab only

1.5.0
- Log book support
- Log book for items and equipment

1.6.0
- Use child locator to position waila in world
- Add half support for modded skins, any more support would be nearly impossible.
- Try to get the name from manifest so it matches what is shown in r2modman.

1.6.1
- Fix nre when a assembly has multiple plugins
