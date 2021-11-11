0.1.0 - Initial Release
- Could very well contain bugs, was origionally doing some internal testing with this but have been encouraged to release it because someone wants to use it.

0.2.0 - Display Prefabs
- Added support for display prefabs, basically checks if the body is null in the charactermodel and uses that to assume its a display skin.

0.3.0
- Fixed the checking for the component to actually check on the new skin model instead of the old.

0.3.1
- Included both the old model and the new model for the component check, this is to clean up the old model if switching to a skin that doesnt use full prefab skins.