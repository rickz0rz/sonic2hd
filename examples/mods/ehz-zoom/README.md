# Emerald Hill zoom

This example mod shows how a level script changes the zoom of the level. It adds one file to Emerald Hill Zone:
`SONICORCA/LEVELS/EHZ/SCRIPT.object.lua`.

In act 1, the camera zooms out to 0.73 while the player is between x = 3000 and x = 9000. The player then sees more
of the hills. When the player leaves that part of the level, the zoom goes back to the default zoom of the level.
Each change takes 90 ticks. The HUD keeps its size.

To use the mod, copy the folder `ehz-zoom` into the `mods` folder of the user data folder. Then start Emerald Hill
Zone:

```sh
publish/S2HD.app/Contents/MacOS/S2HD --level ehz --act 1
```

The script uses API 11:

- `self:set_zoom(zoom, ticks)` changes the zoom in a number of ticks. The zoom starts slowly and stops slowly, on a
  sine curve. Add `"linear"` after the ticks for a change by the same amount on each tick.
- `self:reset_zoom(ticks)` goes back to the default zoom of the level. The default zoom is the attribute `zoom` of
  the level. Set it in the levels tool of the level editor.
- `self:zoom()` gives the zoom on this tick.

The zoom is from 0.5 to 2. A star post keeps the zoom. A restart at the star post, or a return from a special stage,
starts at that zoom at once.
