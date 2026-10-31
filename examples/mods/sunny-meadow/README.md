# Sunny Meadow: an example level

This mod adds an original level, Sunny Meadow. It has rolling grass, slopes, gaps with floating logs, item blocks, burr beetles and a signpost at the end.

The burr beetle (`SONICORCA/OBJECTS/BURRBEETLE`) is an original enemy with a Lua script. It walks on the ground, and turns at an edge or at a slope that is too steep. A character that rolls or jumps into it breaks it. A character that only touches it gets hurt. Its `speed` property sets how far it walks on each tick, and `start_right` makes it start to the right.

The level has a level script, `SCRIPT.object.lua`. The water of the pits goes up and down slowly, as a tide.

The level uses the item block of the `lua-item-block` mod. Install both mods.

## Play the level

1. Copy the folders `sunny-meadow` and `lua-item-block` into the `mods` folder of the user data folder.
2. Start the game with `--level meadow`.

Add `--editor` to open the level in the level editor.

## The files of the level

The folder `SONICORCA/LEVELS/MEADOW` has these files:

| File | Content |
| --- | --- |
| `AREA.area.cs` | One line: `#use SONICORCA.MODAREA`. The game uses its generic area for the level. |
| `INFO.xml` | The name, the music, the start marker and the animals that the enemies free |
| `TILESET` | The tiles: a texture and a list of the tiles in it |
| `MAP.map.xml` | The layers, the tiles of each cell, the collision lines and the markers |
| `BINDING.binding.xml` | The objects: item blocks, rings, a spring and the signpost |
| `SCRIPT.object.lua` | The level script: the tide of the pits |

## Make the level again

The script `generate.py` makes all the files of the level. It draws the tiles, puts the ground into the map from a list of runs (flat, slope, gap), and makes the collision lines from the same runs.

1. Change the layout in `build_layout`, the logs in `LOGS`, or the objects in `build_objects`.
2. Run `python3 generate.py`.

The script writes over the files of the level. A change that the level editor saved goes to `mods/editor/MEADOW.dat` in the user data folder. That file replaces the map and the binding of the mod. Thus, after a change in the editor, do not use the script for the map and the binding.

## The art

All the art of this level is plain shapes that `generate.py` draws: the grass, the dirt, the logs, the flowers, the sky, the clouds, the mountains, the hills, the trees and the burr beetle. The burr beetle is a new design for this mod.
