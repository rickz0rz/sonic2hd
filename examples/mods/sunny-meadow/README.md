# Sunny Meadow: an example level

This mod adds an original level, Sunny Meadow. It has rolling grass, slopes, gaps with floating logs, item blocks and a signpost at the end.

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
| `INFO.xml` | The name, the music and the start marker |
| `TILESET` | The tiles: a texture and a list of the tiles in it |
| `MAP.map.xml` | The layers, the tiles of each cell, the collision lines and the markers |
| `BINDING.binding.xml` | The objects: item blocks, rings, a spring and the signpost |

## Make the level again

The script `generate.py` makes all the files of the level. It draws the tiles, puts the ground into the map from a list of runs (flat, slope, gap), and makes the collision lines from the same runs.

1. Change the layout in `build_layout`, the logs in `LOGS`, or the objects in `build_objects`.
2. Run `python3 generate.py`.

The script writes over the files of the level. A change that the level editor saved goes to `mods/editor/MEADOW.dat` in the user data folder. That file replaces the map and the binding of the mod. Thus, after a change in the editor, do not use the script for the map and the binding.

## The art

All the art of this level is plain shapes that `generate.py` draws: the grass, the dirt, the logs, the flowers, the sky, the clouds, the mountains, the hills and the trees.
