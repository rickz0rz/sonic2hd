# Emerald Hill flood

This example mod shows a level script for a zone of Sonic 2. It adds one file to Emerald Hill Zone:
`SONICORCA/LEVELS/EHZ/SCRIPT.object.lua`.

In act 1, water comes up from below the start position and stops above it. In act 2, there is no water.

To use the mod, copy the folder `ehz-flood` into the `mods` folder of the user data folder. Then start
Emerald Hill Zone:

```sh
publish/S2HD.app/Contents/MacOS/S2HD --level ehz --act 1
```

The script uses API 9: `self:act()` gives the act that plays. A seamless change from act 1 to act 2 keeps the
object of the script, so the script reads the act on each tick.
