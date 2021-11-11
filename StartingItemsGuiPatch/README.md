Allows for access to the starting items gui in a lobby, also enables all earning modes to be active at once(configurable)

1.0.0
- Initial version

1.1.0
- Added getting items over time, on by default but has a config.
- Items over time means the first however many items you find spawn as command blocks with your starting items for options.
- Config for caring about what tier the items are, on by default.

1.1.1
- Fixed items over time not working in multiplayer.

1.1.2
- Fixed changing mode in lobby breaking the back button.
- Changed everything over from jank il hooks to sexy harmony patches

1.1.3
- Fixed nre from when players reconnected to a game.

1.1.4
- Added steam cloud support for item profiles.
- Added command tweaks to quickly select which item with the number row on your keyboard.
- Potential fix for joining a in progress match causing the same nre as rejoining.

1.1.5
- Fixed nre with steam cloud support for loading before having saved.

1.2.0
- Fixed issue with steam cloud on new mod profile.
- Updated the join in progress patch, still experimental.
- Added clear all button to quickly reset a profile.

1.3.0
- Rewrote the items over time code.
- Added chat logging to notify people when only one person has items of a tier left.
- Made dead/disconnected players not count towards having items left. aka command orbs wont spawn for them.
- Removed join in progress patch, as it hopefully isnt needed any more.

1.4.0
- Added what the original item was to the pinging of the command orb. (Host only for now.)
- Finally fixed joining in progress.
- Fixed visual bug on the command ui not displaying your items as a client.
