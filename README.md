# Sonic 2 HD (S2HD) — enhanced .NET 10 port

An enhanced port of the abandoned **Sonic 2 HD 2.0.1012-rc2** demo (originally .NET Framework 4.5, Windows only) to
cross-platform **.NET 10**. The game and its SonicOrca engine were decompiled from the released binaries. All credit
for the game goes to the original S2HD team. This is a non-profit fan project, not affiliated with SEGA or Sonic Team.

The port adds these parts to the demo:

- **Platforms:** Windows, a native macOS (Apple Silicon) app, and an [experimental web build](#web-experimental).
- **Bug fixes:** many fixes for the demo and for the decompiled code. An [Original physics mode](#physics) plays
  closer to Sonic 2 than the physics of the demo.
- **The missing zones:** the game makes the zones, the music and the demos that the demo does not have from the
  [Sonic 2 disassembly](#parts-from-the-sonic-2-disassembly). This works on the desktop and in the browser.
- **Special stages:** the 3D half-pipe [special stages](#entering-a-special-stage) can be played, with the
  [original layouts](#special-stage-layouts) from the disassembly, and [Super Sonic](#super-sonic) after the seven
  emeralds.
- **Mods:** [mod files and mod folders](#modding) that replace or add images, music, levels and objects.
  [Lua scripts](#lua-objects-and-mod-levels) add new objects, and a mod can add a new level.
- **Editors:** a [level editor](#level-editor) and an [animation editor](#animation-editor) in the game.
- **Level select:** a screen of its own, with all the zones, the mod levels and a sound test (see
  [Command-line options](#command-line-options)).
- **Original ending:** after the last level, the [ending cutscene](#ending-cutscene) and the
  [credits of the original game](#original-credits) play before the HD credits film.

## Layout

| Path | Contents |
|---|---|
| `S2HD/`, `SONICORCA/` | Game code (title, menus, levels, objects) |
| `engine/` | Decompiled SonicOrca engine projects |
| `assets/` | Shaders (patched for core-profile OpenGL) and the app icons (`S2HD.icon` for macOS, `S2HD.ico` for Windows) |
| `lib/` | `SDL2-CS.dll`; FFmpeg for Windows is downloaded into `lib/ffmpeg` |
| `tests/` | Tests: the resource file format and a screenshot regression test |
| `tools/` | Release scripts, the resource tool `s2hd-data` and the local web server |
| `examples/` | Example mods: Lua objects and an original level |
| `docs/` | Design documents |

## Game files

Game data is **not** in this repository. Download the 2.0.1012-rc2 demo build from
[sonic2hd.com](https://sonic2hd.com) and extract it. Builds copy `data/` and `credits.film` (and `SDL2.dll` on
Windows) from it, by default from `~/Downloads/S2HD_2.0.1012-rc2/S2HD_2.0.1012-rc2/`.
Point elsewhere with `-p:GameDir=/path/to/S2HD_2.0.1012-rc2/` (keep the trailing slash).

You can also copy `data/` and `credits.film` into the root of the checkout. Git ignores them there.
Then build with `-p:GameDir="$PWD/"` on macOS or `-p:GameDir="$PWD\"` in PowerShell.

## Windows

Requires the .NET 10 SDK.

```powershell
.\tools\get-ffmpeg.ps1          # once: FFmpeg 7.1 DLLs for the credits film
dotnet build                    # development build in bin\Debug\net10.0\win-x64
.\tools\publish.ps1 -Zip        # self-contained release in publish\S2HD-win-x64
```

## macOS (Apple Silicon)

Requires the .NET 10 SDK (`brew install --cask dotnet-sdk` or the installer from https://dot.net) and the Xcode
Command Line Tools (`xcode-select --install`).

```sh
dotnet publish S2HD.csproj -p:PublishProfile=Release-osx-arm64   # add -p:GameDir=... if needed
./tools/macos/build-app.sh                                       # first run builds a minimal FFmpeg (a few minutes)
open publish/S2HD.app
```

`build-app.sh` bundles SDL2 2.32.10 and an LGPL FFmpeg 7.1.3 (H.264/MP4 decoding only) into `S2HD.app` and
ad-hoc signs it for local use. Alternatively, build a kit on Windows with `.\tools\publish.ps1 -Runtime osx-arm64 -Zip`,
copy `publish\S2HD-macos-kit.zip` to the Mac, unzip it and run `./build-app.sh`.

The app icon is `assets/S2HD.icon`, an Icon Composer file. On macOS 26 and later it has the Liquid Glass look.
`build-app.sh` compiles it with `actool`, which needs Xcode 26 or later. Without that version of Xcode, the script
makes a flat icon from `assets/S2HD.png`.

## Web (experimental)

`web/S2HD.Web` builds the game for browsers: .NET compiled ahead of time to WebAssembly, rendering through WebGL2.
The site contains no game data. Players choose their own copy of the S2HD game files (a zip of the game folder,
or the folder itself), and the files are loaded into browser memory without being uploaded.

The web build is online at https://rickz0rz.github.io/sonic2hd/. The workflow `.github/workflows/deploy-web.yml`
builds it and deploys it to GitHub Pages after each push to `main` that changes the game, the engine or the web
code. Pull requests only build it, as a check. The deployed site never includes game data.

Publishing needs the `wasm-tools` workload, installed once from an administrator terminal:

```
dotnet workload install wasm-tools
dotnet publish web/S2HD.Web -c Release -o publish/web
```

The AOT compile takes several minutes. Upload the contents of `publish/web/wwwroot` to any static web host
(GitHub Pages, Netlify, an S3 bucket and so on). The host must serve `.wasm` files as `application/wasm`; most do.
To try a build locally:

```
dotnet run --project tools/web-server -- publish/web/wwwroot 8080
```

Then open http://localhost:8080. The `?data=<url>` query parameter loads a zip from the same site instead of asking,
which is useful for testing. `?args=` gives options as on the desktop command line, for example
`?args=--halfpipe%201`, `?args=--level%20arz` or `?args=--ending%20super`. Write `%20` for each space. `?profile` turns on the frame profiler, as `S2HD_PROFILE=1` does on
the desktop. The green text at the top left shows the frame rate, the CPU time of a game step and the time of the
audio mix, every 5 seconds. Keyboard controls match the desktop game. Sound starts at the first key press or click,
because browsers block audio until then. Gamepads and the credits video are not implemented yet, and a browser
with WebGL2 and a keyboard is required.

To put the game data into the site, add `-p:BundleGameData=true` to the publish command. The build then writes the
`data/` and `mods/` folders to `wwwroot/game.zip`, and the page downloads it and starts without asking for files. It reads the
game files from `GameDir`, as the desktop build does. The zip is about 115 MB. Some hosts limit file size, for
example GitHub Pages (100 MB) and Cloudflare Pages (25 MB). The game data belongs to its owners, so only publish it
where you have permission. A publish without the property removes `game.zip`.

When players choose their own files, the page also loads the `.dat` files in a `mods/` folder of the zip or folder.

### Parts from the Sonic 2 disassembly

The demo data has only Emerald Hill, Chemical Plant and Hill Top. On the desktop, `tools/import-all.sh` makes the
other zones, the special stages, the attract demos, the original credits and the music from the Sonic 2
disassembly. On the web, the page
does the same import in the browser:

1. Download the disassembly as a zip from https://github.com/sonicretro/s2disasm (Code, Download ZIP).
2. On the page, choose this zip with **Choose s2disasm zip**, before the game data.
3. Choose the game data.

The page makes the mods (about 20 seconds in Chrome on an Apple silicon Mac). It keeps them and the zip in the
browser storage (IndexedDB). On later visits, the page uses the stored mods, and only the game data is necessary.
When a new version of the site changes the importers (`OriginalImport.Version`), the page makes the mods again from
the stored zip. **Delete stored files** removes the zip and the mods. Mods in the `mods/` folder of the game data have
priority over the import.

The browser has no ffmpeg, so the web import does not make Ogg Vorbis samples. It keeps each song and its drums
(`SAMPLE.smps`), and the game plays the song live on the emulated sound chips. The music mod is then about 150 KB,
against 21 MB of Ogg Vorbis on the desktop. The desktop keeps the Ogg Vorbis samples, which use less processor time.

For tests, `?disasm=<url>` stores a zip of the disassembly from the same site, as the button does.

## Modding

The game data is in `data/sonicorca.dat`. This file holds 841 resources: PNG images, OGG music and XML files for
levels, tile sets, animations and fonts. The tool `tools/s2hd-data` converts it to normal files and back:

```sh
dotnet run --project tools/s2hd-data -- list   data/sonicorca.dat
dotnet run --project tools/s2hd-data -- unpack data/sonicorca.dat my-mod
dotnet run --project tools/s2hd-data -- pack   my-mod my-mod.dat --base data/sonicorca.dat
```

1. Unpack the game data into an empty folder.
2. Edit the files. Keep the file names: the folders and the name give the resource key, and the extension gives the
   type. For example, `SONICORCA/LEVELS/EHZ/MAP.map.xml` is the Emerald Hill map. A resource that also contains
   other resources is the file `_` in its folder, for example `SONICORCA/FONTS/HUD/_.font.xml`.
3. Pack the folder with `--base`. The tool then writes only the resources that are new or changed.
4. Put the `.dat` file in a `mods` folder. The game loads mods from two folders:
   - `mods/` next to `data/` (on macOS, in `S2HD.app/Contents/Resources`).
   - `mods/` in the user data folder (see "Logs and settings"). This folder is outside the macOS app bundle, so
     the app signature stays valid and an update does not remove the mod.

The game loads `data/`, then each `mods/` folder, and each folder in file name order. A resource in a later file
replaces the resource with the same key. A mod cannot remove a resource. To turn mods off, add `use_mods=false` to
the `[general]` section of `sonicorca.cfg`.

Without `--base`, `pack` writes all resources of the folder. That file can replace `data/sonicorca.dat`.

### Mod folders

A folder in a `mods` folder is also a mod. It has the same files as an unpacked folder, for example
`mods/my-mod/SONICORCA/OBJECTS/RING/TEXTURE.png`. The game reads the files from the disk when it starts, so you can
change a file and start the game again, with no pack step. The game loads the folders and the `.dat` files of a
`mods` folder together, in name order. The `editor` folder and the folders whose names start with a dot are not
mods.

To change images, unpack all the images into a mod folder, change the images that you want, and then delete the
images that you did not change:

```sh
MODS="$HOME/Library/Application Support/SonicOrca/mods"
dotnet run --project tools/s2hd-data -- unpack data/sonicorca.dat "$MODS/my-mod" --images
# Change images in "$MODS/my-mod", and start the game to see them
dotnet run --project tools/s2hd-data -- prune "$MODS/my-mod" data/sonicorca.dat
```

`unpack` and `prune` accept more than one `.dat` file. A later file replaces the resources of an earlier file, as
in the game. Add the files of the imported zones to get their images too, for example
`unpack data/sonicorca.dat "$MODS/cnz.dat" "$MODS/my-mod" --images`. Without `--images`, `unpack` writes all the
resources. `prune` deletes the files that are the same as in the `.dat` files, so the folder keeps only your
changes. It is good to prune: a file that you did not change still replaces the resource of a later update of the
game data or of an import. To share the mod as one file, pack the folder with `--base`.

### Lua objects and mod levels

A mod can add an object with a Lua script, with no change to the code of the game. The object is a folder in
`SONICORCA/OBJECTS` with the script `_.object.lua`, its animations and its textures. The script gives the name and
the properties of the object, and the functions that the game calls on each tick. The level editor shows the new
objects, and the game loads a script again when its file changes.

A mod can also add a level in `SONICORCA/LEVELS/NAME`, with a tile set, a map, a binding and an `INFO.xml` file.
`--level NAME` starts it, and the level select shows it after the zones of the game. With the attribute `tileset`
in `INFO.xml`, for example `tileset="SONICORCA/LEVELS/EHZ/TILESET"`, the level uses the tile set of another level
and needs no tile set of its own.

Two example mods are in `examples/mods`. Their READMEs describe the files and the Lua API:

- `lua-item-block`: an item block that a character hits from below, and a coin.
- `sunny-meadow`: an original level with slopes, gaps, item blocks and a walking enemy. A script, `generate.py`,
  makes all its files. It needs `lua-item-block`.

The design of the Lua API is in `docs/lua-modding-design.md`.

### Import all

The build does not make the data of the missing zones. One script makes all the mods that come from the
disassembly: the zones, their music, the special stage layouts, the sprites of Super Sonic, the demos and the
original credits. The music needs `ffmpeg` on the
`PATH` (or set `S2HD_FFMPEG`). The sections below tell what each import does.

```sh
git clone https://github.com/sonicretro/s2disasm.git ../s2disasm
./tools/import-all.sh
```

The script finds the disassembly at `S2DISASM_DIR`, `../s2disasm` or `../../sonicretro/s2disasm`. It writes the
`.dat` files to the `mods` folder in the user data folder. To use other folders, give them as arguments:
`./tools/import-all.sh <s2disasm> <mods>`. Run the script again after an update of this repository.

### Ending cutscene

The import also makes the art of the ending cutscene of the original game into `SONICORCA/ENDING`. Each image is a
normal resource at four times the size of the original, so a mod can replace it:

| Resource | Contents |
|---|---|
| `PICTURES/1` to `4` | The four photos at the start |
| `SONIC` | Sonic who falls, and Sonic who waits on the plane |
| `SUPERSONIC/...`, `TAILS/...` | The same art for the ending of Super Sonic and of Tails: `CHARACTER`, `BIRDS`, `PLANE`, `CLOSEUP`, and `TAILS/PILOT` (Sonic flies the plane) |
| `CLOUDS` | The clouds that fly past |
| `TORNADO`, `TORNADO/PILOT` | The plane that comes for Sonic, and Tails in it |
| `BIRDS` | The birds that fly with the plane |
| `PLANE` | The plane that flies away, the propeller, and Sonic who jumps (the frames of ObjCF) |
| `CLOSEUP` | The close-up of the plane at the end |

The background is the background layer of Death Egg Zone, so it needs the import of DEZ. The ending music
(`95 - Ending`) comes with the music import.

The cutscene follows the objects of the original (ObjCA to ObjCF): the photos fade in and out, Sonic falls past the
stars and the edge of the Earth, the Tornado comes with the birds, and Sonic jumps in front of the close-up. The fades
of the palette become fades of the images to white. It plays after the last level of the story, before the credits.
`--ending` plays it and then the credits.

The ending has three versions, as in the original. Tails alone gets the ending of Tails: no photos, and Sonic flies
the plane. Else, with the 7 emeralds, Super Sonic flies next to the plane and past it. Else, Sonic gets the normal
ending. The original checks only the emeralds, not the rings. `--ending super` and `--ending tails` play the other
two versions.

### Original credits

The import also makes the credits of the original game: 21 slides of text and the Sonic 2 logo at the end. It reads
the text and the layout of the slides from `s2.asm`, and draws them with the credits font of the disassembly. The
credits music (`9E - Credits`) comes with the music import.

```sh
dotnet run --project tools/s2hd-data -- import-ending ../s2disasm ending
```

After the last level, and with `--credits`, the original credits play first. The slides fade in and out with the
fades of the original, and the logo flashes with its palette cycle. Then the HD credits film plays. Enter, Start, or
a face button of the game pad goes from the slides to the logo. On the logo, the same buttons end the original credits
after the flash. Without the import, only the HD credits film plays.

### Music

The demo data has no music for ARZ, CNZ, MCZ, OOZ, SCZ, WFZ and DEZ. It also has no Super Sonic music and no sound
for the transformation into Super Sonic. The import command makes this music from the songs of the disassembly
(`sound/music`), and the sound from the sound effects (`sound/sfx`):

```sh
dotnet run --project tools/s2hd-data -- import-music ../s2disasm music
dotnet run --project tools/s2hd-data -- pack music music.dat --base data/sonicorca.dat
```

The command plays each song with a copy of the Sonic 2 sound driver in C#. The driver sends the notes to emulated
YM2612 (FM) and SN76489 (PSG) chips, and the DAC drums come from `sound/DAC`. The command finds the point where the
song repeats, and `ffmpeg` encodes the sound as Ogg Vorbis at 48000 Hz. The sample info of each song gives the loop
point, so the game repeats the song without a gap. It takes about 10 seconds. The songs use the fixes of
`FixMusicAndSFXDataBugs`, so they play as their composers meant. A sound effect has no loop point.

Put `music.dat` in the `mods` folder. The music comes from the original game, so do not commit it.

### Special stage layouts

The demo data has no special stage layouts, so `--halfpipe` plays a generated layout. The import command converts
the original Sonic 2 layouts from a local copy of the [Sonic 2 disassembly](https://github.com/sonicretro/s2disasm)
into a mod:

```sh
git clone https://github.com/sonicretro/s2disasm.git ../s2disasm
dotnet run --project tools/s2hd-data -- import-special-stages ../s2disasm special-stages
dotnet run --project tools/s2hd-data -- pack special-stages special-stages.dat --base data/sonicorca.dat
```

Put `special-stages.dat` in a `mods` folder. The 7 stages then have the original track segments, rings, bombs,
checkpoints and emerald. The import also makes the art of the original that the demo data does not have: the START
banner, the winged hand of the checkpoint messages, the letters of the messages, the big digits of the ring
counters, and the white results screen with the emeralds. The letters of a message fly away from the centre of the
screen when it ends, as in the original. Without the art, the stage shows text messages and a dark results screen.
An import from before the big digits shows the messages and the counters with text: run the import again to get
them. The music import adds the jingle of the results screen. The layouts are data from the original game, so do not commit them to this repository.
The tests use the disassembly when they find it next to the checkout or at `S2DISASM_DIR`.

### Level imports

The demo data has the tiles of Aquatic Ruin (ARZ), Casino Night (CNZ), Mystic Cave (MCZ), Oil Ocean (OOZ),
Metropolis (MTZ), Sky Chase (SCZ), Wing Fortress (WFZ) and Death Egg (DEZ), but no collision, no objects and no
music. The import command makes the rest of a zone from a local copy of the Sonic 2
disassembly. Only the importer is in this repository. The zone data that it makes comes from the original game, so
keep it local.

```sh
git clone https://github.com/sonicretro/s2disasm.git ../s2disasm
dotnet run --project tools/s2hd-data -- import-level cnz ../s2disasm cnz
dotnet run --project tools/s2hd-data -- pack cnz cnz.dat --base data/sonicorca.dat
```

Do the same for `arz`, `mcz`, `ooz`, `mtz`, `scz`, `wfz` and `dez`. Put the `.dat` files in a `mods` folder. They
can be there together. Then start a zone with `--level cnz` (or one of the other names), and add `--act 2` for the
second act (MTZ also has `--act 3`). Import again after
an update of this repository. The import does these steps:

- It copies the blocks of both acts into the HD map, at the positions that the HD map uses.
- It traces the collision of both paths from the height maps of the original.
- It adds the start positions, the origins and bounds of the acts, and the water levels as map markers.
- It converts the rings and the objects. The command lists the object types that it cannot convert yet.
- It converts the art of the new objects and the bosses from the original sprites and level art, at four times the
  size, as the HD tiles of these zones are.
- It adds a layer for the parts of the blocks that the original draws in front of the characters. The layer
  switches move a character in front of this layer or behind it, as the original sets the sprite priority.
- It makes animated tiles from the animated level art of the original, for example the oil of OOZ and the
  flipping panels of CNZ. The HD tiles do not have this art.
- In CNZ, it also adds the bumpers that are part of the landscape, and the reels of the slot machines.
- It writes the collision data of the original for the front layer (`TILECOLLISION`), so that the level editor can
  trace the collision again after a change of tiles.

On a flipper or a spring launcher in CNZ, the jump button controls the object. Press jump to flip a flipper. Hold
jump to pull back a launcher, then let go to launch.

The last act of each zone ends with the boss of the original. The camera locks into the arena at the positions of
the original level events. The HD screen is wider than the original screen, so the arena shows 80 more pixels on each
side. In OOZ, the area adds the oil: a character sinks slowly into it and can jump out.

These zones have no music, because the demo data has none for them. A mod can add the music that each area names,
for example `SONICORCA/MUSIC/LEVELS/CNZ` or `SONICORCA/MUSIC/LEVELS/OOZ`. The data also has no sounds for some
objects, for example the bumpers, the arrow shooters and the lasers of the OOZ boss. These objects play the nearest
sound that the data has, or no sound.

### Physics

The PHYSICS setting in Options > Game selects how the characters and the objects move:

- ORIGINAL (the default) follows the code of the original Sonic 2. For example, Sonic can stand still on a slope,
  a held direction limits the speed in the air, and the drowning numbers start 12 seconds before Sonic drowns.
- HD keeps the behavior of Sonic 2 HD where it is different.

The attract demos always use the original physics. With `--level`, the `--physics` option overrides the setting.
The test variable `S2HD_PHYSICS` also overrides the setting, but not the demos or `--physics`.

### Entering a special stage

Touch a star post with 50 rings or more and fewer than 7 emeralds. A circle of stars then appears above the star
post. Jump into the circle to start the special stage. The circle goes away after about 8 seconds. After the stage,
the player starts again at the star post with 0 rings. The stage score goes into the level score. A won stage
gives an emerald, and the next star circle opens the next stage. A failed stage opens the same stage again.

When Tails follows Sonic in the level, Tails also plays in the special stage, with the ring quotas for two
players. The computer controls Tails: Tails does what Sonic did 16 frames before. When the second pad has input,
the second player controls Tails. The computer takes control again 3 seconds after the last input. Each player
has separate rings, and the goals use the total. A jump or a bomb changes the lanes of the two players.

### Super Sonic

With 7 emeralds and 50 rings or more, Sonic becomes Super Sonic at the top of a jump, as in the original. Super
Sonic is faster, jumps higher and cannot be hurt. A pit, a crush or drowning can still kill him. He loses 1 ring
each second. He becomes normal again when he has no rings, at the end of the act or when he dies. Tails cannot
become super. The data has no Super Sonic art, so the game colours Sonic gold. The import (`import-super-sonic`, or
`./tools/import-all.sh`) makes the sprites of Super Sonic from the art of Sonic in the disassembly. With them,
Super Sonic uses the original sprites and the animations of the original. His colours fade in from blue and then
cycle, as in the original. At high speed, he leaves a trail of stars. An import from before import version 5 has
no colour cycle and no stars. Run the import again to get them. To use the gold HD sprites instead, add this to
`sonicorca.cfg`:

```ini
[graphics]
original_super_sonic=0
```

The data also has no Super Sonic music or sound. With the music import (see [Music](#music)), the transformation
plays the sound of the original, and the Super Sonic music replaces the level music. As in the original, the Super
Sonic music continues after Sonic becomes normal again. Without the import, the level music continues. Super Sonic
has the values of the original in the two physics modes.

### Level editor

The level editor changes the objects, the rings, the markers, the tiles and the collision of a level. Start the game
with `--editor` and `--level`: the editor opens when the level starts. F2 in a level also opens it. With `--editor`
alone, the editor opens on the first level of the story with the form of a new level (see "New levels"):

```sh
publish/S2HD.app/Contents/MacOS/S2HD --level cnz --editor
```

The level stops, and the editor shows all the objects of the level. The editor has five tools. Select a tool with
its button or with its key:

- **Objects (1)** changes the objects and the rings. The list has all the object types of the game. A type that the
  level does not use shows "not loaded". It loads when you click it. To make a new object, click "+ New object"
  (see "New objects").
- **Markers (2)** changes the markers of the map. A point marker has a name, for example `startpos_stk_1` (the start
  position of act 1), `bounds_1_rb` (the bottom right camera bound of act 1) or `boss_left`. An area marker has a
  tag, for example `inside`. The level reads the markers when it starts. Click a marker in the list to go to it.
- **Tiles (3)** paints the tiles of a layer. Click a layer in the list, then click a tile in the palette, or hold
  Ctrl and drag in the level to take a rectangle of tiles. Click or drag in the level to paint. X and Y flip the
  brush. The first tile of the palette is empty, to erase. On the front layer, a brush also paints the priority
  tiles (`front_high`) and the solidity of the cells. Set the solidity of a palette tile for each path: none, top
  only, or all sides.
- **Collision (4)** changes the collision lines. A line is solid on its right side, from its start to its arrow,
  with Y down. So a floor goes from left to right, and a ceiling from right to left. Green lines are on path 1,
  blue lines on path 2, and white lines on both. The layer switches select the path. Grey lines are the lines of
  layout changes. Click "Draw lines", then click the points of the lines. Drag the end of a selected line to move
  the point. The other lines that end at that point move with it.
- **Levels (5)** opens a level and changes the order of the story. The list shows the levels of the story in their
  order, then the other levels. Click a level, then click "Open" to edit that level. With changes that are not saved,
  the first click only warns, and a second click opens the level without the changes. Click "Move up" or "Move down"
  to change the position of the level. Click "In the story" to add the level to the end of the story or to remove
  it. A mod level can also be in the story. Click "Name" to type a new name. Click "Music" to change the music of a
  mod level (right click goes back). The area of a Sonic 2 level sets its music. After a save, the title card shows
  the new name at the next play. New music plays when the level opens again. Undo does not change these values. To make a level, click "+ New level".
  Type a name, and change the short name if necessary. Click "Tiles from" to choose the level with the tiles. Then
  click "Create" (see "New levels").

In the imported zones, the collision follows the tiles: after you paint on the front layer, the editor traces the
collision of the changed area again, as the import does. Lines that you drew by hand in other areas stay. "Trace
paths 1 and 2 again from the tiles" in the collision tool traces all the lines again. This needs the tile collision
data that the import writes (`TILECOLLISION`), so run `./tools/import-all.sh` again after an update. The zones of
the game data (EHZ, CPZ, HTZ) have no tile collision data. In these zones, draw the collision lines with the
collision tool.

The editor shows the map without the layout changes of the play, for example the walls that close a boss arena.

Use the mouse and the keyboard:

| Input | Effect |
|---|---|
| Click an object or a marker | Selects it. Shift adds items to the selection or removes them |
| Drag an object or a marker | Moves the selected items on the grid |
| Drag the corner of an area marker | Changes the size of the area |
| Drag in an empty area | Selects the items in a box |
| Click a type in the list, then click in the level | Adds an object of this type. Hold Shift to add more than one. Esc stops |
| Click "Add a point marker" or "Add an area marker", then click in the level | Adds a marker |
| Click a value in the properties | Changes the value. A yes or no value and a list value change on a click (right click goes back). For a number or a text, type the value and press Enter |
| Right drag, middle drag or Space and drag | Moves the view |
| Wheel, Shift and wheel | Moves the view up and down, or left and right |
| Ctrl and wheel, `-` and `=` | Zooms out and in |
| Arrow keys | Move the selected items by the grid size (Shift: 4 times). Without a selection, they move the view |
| Del or Backspace | Deletes the selected items |
| Ctrl+Z, Ctrl+Shift+Z or Ctrl+Y | Undo and redo |
| Ctrl+C, Ctrl+X, Ctrl+V, Ctrl+D | Copy, cut, paste at the mouse, and duplicate |
| Ctrl+A | Selects all the items in the view |
| G | Changes the grid size: 4, 16, 32, 64 or 128 (1 to 32 pixels of the original) |
| Home | Moves the view to the start position |
| N | Opens the animation editor for the selected object, or for Sonic (see "Animation editor") |
| H | Shows or hides the hit boxes of the objects, in the editor and in the play |
| F5 | Plays the level from the mouse position |
| F2 or Shift+F5 | Plays the level from the start position |
| Ctrl+S | Saves the level |

On macOS, Cmd also works in place of Ctrl.

To play, the level starts again with the changes. Press F2 in the level to go back to the editor. Undo also works
after you play. While you play with changes that are not saved, a note shows at the top right of the screen.

Save writes the changed parts of the zone to `mods/editor/<ZONE>.dat` in the user data folder (see "Logs and
settings"): the objects, the map, or both. The game loads this folder after all the other mods, so the changes
replace the objects and the map of the game data and of the imports. The game loads the file also without
`--editor`. To remove the changes of a zone, delete its file.

When the level unloads with changes that are not saved, for example when you quit the level or close the game, the
editor writes them to `mods/editor/unsaved/<ZONE>.dat`. The game does not load this folder. To keep the changes,
move the file to `mods/editor`.

The editor saves the map in a way that keeps all the other data of the map file.

A new level is a mod folder, `mods/<short name>/`, in the user data folder. It uses the tile set of the level that
you chose, so the folder has no tiles. The map has an empty background layer, an empty front layer, a floor line, and
a start position. A signpost near the right end ends the level. To end it with a boss, delete the signpost and add a
Lua boss, for example `LUAWISP` of the example mod `lua-item-block`. The level is not in the story until you add it. The
editor saves the changes of the level to `mods/editor/<SHORT NAME>.dat`, as for the other levels.

A new object is a Lua object (see "Lua objects and mod levels"). Click "+ New object" at the top of the list of
the objects tool, and type a name. The key follows the name, for example `SONICORCA/OBJECTS/BIGROCK`. Click "Kind"
to choose the script:

| Kind | Script |
|---|---|
| scenery | Shows its image. |
| solid block | A solid box. The characters can stand on it. |
| collectible | Gives rings to a character that touches it, then goes away. The `rings` property sets the count. |
| enemy | Walks on the ground and turns at edges and walls. A character that rolls or jumps into it breaks it. A touch hurts the character. The `speed` and `start_right` properties change the walk. An enemy image looks to the left. |

To use your art, click "Image" and type the path of a PNG file. Without an image, the object gets a placeholder
image. One pixel of the image is one unit of the level, and four units are one pixel of the original game. Click
"Create". The editor writes `_.object.lua`, `ANIGROUP.anigroup.xml` and `TEXTURE.png` in a mod folder, and you can
then click in the level to add the object. The mod folder is the folder of the open level if it is a new level.
Otherwise it is `mods/objects/` in the user data folder.

To change the object, open `_.object.lua` in a text editor. When you select the object in the level, click "Script"
in the properties to show the file. Save the file, and the game loads the script again. To add frames, change
`ANIGROUP.anigroup.xml` and `TEXTURE.png`, and start the game again.

Save writes the order of the story and the new names of the Sonic 2 levels to `mods/editor/LEVELS.dat`. The name and
the music of a mod level go into its `INFO` file in `mods/editor/<SHORT NAME>.dat`. The title card shows the new
name of a Sonic 2 level, and adds "ZONE" to it. The story, the level select and the next level
after `--level` use this order. The level select shows the levels that are not in the story after the other levels,
with a star. To go back to the order and the names of Sonic 2, delete the file.

### Animation editor

The animation editor changes the animations of the sprites. Start the game with `--animations`, and give the key of
an animation group to open it:

```sh
publish/S2HD.app/Contents/MacOS/S2HD --animations SONICORCA/OBJECTS/SONIC/ANIGROUP
```

The left panel lists all the animation groups of the game and of the mods. Click the filter row and type to find a
group. After the groups, the list has the images that are not in an animation group, marked "image": for example the
title, the photos and the close-ups of the ending (`ENDING/...`), the slides of the credits (`CREDITS/ORIGINAL/...`)
and the fonts. `--animations SONICORCA/ENDING/PICTURES/1` opens an image. The editor shows the image, and "Edit 0" and
"Replace 0" change it in the same way as a texture of a group (see below). An image has no animations to change. A
new image can have another size: the game draws it at the place and size of the old image. The centre plays the selected animation at the speed of the game, with a cross at the position of the object.
The strip under it shows the frames, with the time of each frame in ticks (1/60 s). The right panel lists the
animations of the group and the properties of the animation and of the selected frame. Click a property to type a
new value, then press Enter.

The centre has two views. The preview shows the frame at the position of the object, as in the game. Drag the frame
to change its offset; with Ctrl, the offsets of all the frames of the animation move together. When the animation
does not play, the previous frame shows faintly behind the frame (onion skin, O). The texture view (T) shows the
texture of the frame, with the rectangles of the frames of the animation. Drag in the yellow rectangle of the
selected frame to move it, drag an edge or a corner to change its size, or drag outside the rectangles to draw a new
rectangle. Click the rectangle of another frame to select that frame. Drag with the right or the middle button to
pan, and use the mouse wheel to zoom about the mouse. Each drag is one step of the undo history.

| Key | Action |
|---|---|
| Space | Play or stop the animation |
| Left, Right | The previous or the next frame |
| Up, Down | The previous or the next animation |
| Insert | Add a frame after the selected frame |
| Ctrl+D | Duplicate the selected frame |
| Delete | Delete the selected frame |
| Ctrl+Left, Ctrl+Right | Move the selected frame |
| T | Change between the preview and the texture view |
| O | Show or hide the onion skin |
| Alt+arrows | Move the offset (preview) or the rectangle (texture view) by one pixel |
| Alt+Shift+arrows | Change the size of the rectangle by one pixel (texture view) |
| Mouse wheel on the centre, Z | Zoom |
| Ctrl+Z, Ctrl+Y | Undo and redo |
| Ctrl+S | Save |
| Escape | Leave the editor, or go back to the level editor |
| F5 | Play the level (from the level editor only) |

The game finds an animation by its number, not by its name. So the editor adds a new animation after the last one,
and it can delete only the last animation. The name of an animation is the comment before it in the file. The loop
frame of an animation is the frame that comes after its last frame. Without a loop frame, the game stays on the last
frame, or goes to the next animation.

The textures section at the end of the properties changes the images of the group. **Edit** copies a texture as a
PNG file into `mods/editor/animations/` (for example `SONICORCA/OBJECTS/RING/TEXTURE.png`) and shows the file in
Finder or Explorer. Open the file in an image editor: when you save it, the editor shows the new image at once, and a
level that starts later in the session uses it too. **Replace** and **Add texture** take the path of a PNG file and
copy it into the same folder; a new texture gets its name from the file name, and you set it on the frames that use
it with the texture property of the frame. These files are written at once, so undo does not remove them. Delete a
file to use the texture of the game again.

To see an animation in a level, open the level editor (`--editor`, then F2) and press N. The animation editor opens
for the selected object, or for Sonic when no object is selected. It shows the animation that the object plays now.
The red boxes in the preview are the hit boxes of the object. Escape goes back to the level editor, and F5 plays the
level from the view. The level shows the changes at once, saved or not. Press H to see the hit boxes of the objects
in the play, then F2 to go back to the editor, and N to open the animation editor again. The animation editor keeps
its changes and its undo history while the level is loaded.

Ctrl+S writes each changed group as a file into `mods/editor/animations/` in the user data folder, in the layout of
`s2hd-data unpack`, for example `mods/editor/animations/SONICORCA/OBJECTS/SONIC/ANIGROUP.anigroup.xml`. The game
loads this folder after the other mods. A level that starts later in the same session already has the changes. To
remove the changes, delete the file.

The behavior of the levels and the objects of the game is C# code in `SONICORCA/`, not data. The `AREA.area.cs` and
`.object.cs` resources only name the class to use. A mod adds new behavior with
[Lua objects](#lua-objects-and-mod-levels).

## Command-line options

| Option | Effect |
|---|---|
| `--level <zone> [--act <n>]` | Starts a level directly, for example `--level htz --act 2` |
| `--protagonist <name>`, `--sidekick <name>` | Chooses the characters for `--level` (`sonic`, `tails`, `knuckles`). Use `--sidekick none` with `--protagonist` to play alone |
| `--startpos <x> <y>` | Starts `--level` at this position, in level units |
| `--levelselect` | Opens the level select at once. On the title screen, press up, down, left and right, then hold the jump button and press Start |
| `--debug` | Starts the level with debug mode on |
| `--animations [KEY]` | Opens the animation editor, for example `--animations SONICORCA/OBJECTS/SONIC/ANIGROUP` |
| `--editor` | Turns on the level editor: F2 in a level opens it. With `--level`, the editor opens when the level starts. Alone, it opens the editor with the form of a new level |
| `--physics original`, `--physics hd` | Plays the `--level` level with this physics, whatever the PHYSICS setting is |
| `--demo` | Plays the attract demo (the demos of all zones that have one play in turn) |
| `--record <file>` | Records the input of the level to a file, for example for a demo |
| `--playback <file>` | Plays a recorded input in place of the controller |
| `--nologos` | Skips the disclaimer and the logo screens |
| `--no-error-dialog` | Writes an error to the console and the log, not to a dialog box. Test runs with `S2HD_EXIT_AFTER_CAPTURES=1` do this too |
| `--credits`, `--options` | Opens the credits or the options screen |
| `--ending [super\|tails]` | Plays the ending cutscene of the original, then the credits. `super` plays the ending of Super Sonic, and `tails` plays the ending of Tails |
| `--halfpipe [1-7]` | Opens a 3D special stage. Left and right move Sonic around the pipe, and an action button jumps, with the physics of Sonic 2. Collect enough rings for each of the three checkpoints, and avoid the bombs. Tails follows Sonic. Add `--sidekick none` to play Sonic alone. Escape closes it |

On macOS, pass options to the executable in the app bundle, for example
`publish/S2HD.app/Contents/MacOS/S2HD --level cpz`.

### Recording an attract demo

The title screen plays a demo when nobody presses a button. Each zone can have a demo: the input recording
`SONICORCA/RECORDINGS/<ZONE>` of act 1 with Sonic and Tails. The demos of all the zones that can be played (with
their import, for the imported zones) play in turn, as in the original. A demo shows the title card and 1640 frames
of play (about 27 seconds), as in the original.

The original game has demos of EHZ, CPZ, ARZ and CNZ. The import command converts them into recordings. The HD maps
of these zones have the layout of the original, so the demos take their original routes.

```sh
dotnet run --project tools/s2hd-data -- import-demos ../s2disasm demos
dotnet run --project tools/s2hd-data -- pack demos demos.dat
```

Put `demos.dat` in the `mods` folder. The ARZ and CNZ demos also need the imports of these zones.

To record a demo of your own:

1. Record a play of act 1:

   ```sh
   publish/S2HD.app/Contents/MacOS/S2HD --nologos --level mcz --record mcz.rec
   ```

2. Check it: `--level mcz --playback mcz.rec` plays the same level again. While a recording plays or records, the
   random numbers of the level start from the same seed, so the play is the same each time.
3. Put the file in a mod folder at `SONICORCA/RECORDINGS/MCZ.rec`, then pack the folder as above.

A recording plays the input only, so a change to the physics or to the objects can make a recording take another
route. Record it again then.

## Logs and settings

`sonicorca.log` and `sonicorca.cfg` live in `Documents\SonicOrca` on Windows and
`~/Library/Application Support/SonicOrca` on macOS. Screenshots (F10) are saved there too.

Vsync is off by default (windowed play is composited without tearing, and the frame timer paces more evenly on
slow GPUs). To turn it on, for example in exclusive fullscreen, add this to `sonicorca.cfg`:

```ini
[graphics]
vsync=1
```

## Profiling

Set the environment variable `S2HD_PROFILE=1` before launching. The game then logs a frame-time summary every
300 frames (update, CPU draw, GPU wait, swap and pacing wait), notes shader compiles and slow texture uploads, and
writes one row per frame to `profile.csv` next to the log.

## Tests

```sh
dotnet test --project tests/S2HD.Tests --filter-not-trait "Category=Regression" --filter-not-trait "Category=Story"   # fast tests
dotnet test --project tests/S2HD.Tests --filter-not-trait "Category=Story"                                             # all tests except the story test
```

The resource file tests make their own small files. The game data tests read the real `sonicorca.dat` and are
skipped when the game files are not found. Set `S2HD_GAME_DIR` if they are not in the default location.

The regression tests run the release build and save screenshots at fixed level ticks:

- One test plays the Emerald Hill attract demo, which moves the player through the first part of the level.
- One test for each complete level of the demo data (EHZ, CPZ and HTZ, acts 1 and 2) starts the level with no
  input. MTZ and DEZ are only stubs in the demo data, so the tests do not use them.
- One test plays the first special stage with no input.
- One test for each act of the imported zones starts the act with no input. These tests need the Sonic 2
  disassembly, as the import section describes. The tests import the zones into a temporary folder and record
  their golden screenshots in `tests/golden/levels-imported/`. When the disassembly is not found, the tests are
  skipped.
- Three tests open the level editor in Emerald Hill with scripted input. One adds a monitor and plays. One moves
  the start position marker and plays from the start. One paints tiles, draws a collision line and plays on it.
- Three tests open the animation editor with scripted input. One changes the delay of a frame of Sonic and adds a
  frame. One drags the edge of a rectangle in the texture view and drags a frame in the preview. One adds a PNG file
  as a new texture of the ring. All save, and check the saved files.

The tests compare the screenshots with golden screenshots. A run takes about 2 minutes. The golden screenshots come
from the game files, so Git ignores them (`tests/golden/`). Record them once from a known-good build:

1. Build the game for your platform, as the sections above describe.
2. Run `S2HD_RECORD_GOLDEN=1 dotnet test --project tests/S2HD.Tests --filter-trait "Category=Regression"`.

After a change, build the game again and run the tests. The tests copy screenshots that do not match to
`tests/golden/actual`. When a change is meant to change what the game draws, check the new screenshots, then record
the golden screenshots again. Set `S2HD_EXE` to test a build in a different location.

The story test plays the full story with the imported zones and no input. It starts in Emerald Hill and plays to
the end of Death Egg. At the start of each act, the player waits, so that the test can find a bad start position.
Then the test moves the player to a point before the signpost and holds right. The test examines the start of each
act, the seamless change from act 1 to act 2, and the change to the next zone. It also finds deaths and acts that
start again. A run takes about 8 minutes, so the fast tests and the regression tests do not include it:

```sh
dotnet test --project tests/S2HD.Tests --filter-trait "Category=Story"
```

When the test fails, it keeps the log and a screenshot of each act. The message gives the folder.

The test uses these environment variables, which you can also set by hand:

| Variable | Effect |
|---|---|
| `S2HD_USER_DATA_DIR` | Uses this folder for the log, settings and screenshots |
| `S2HD_RANDOM_SEED` | Seeds the random number generators of the level and the particles |
| `S2HD_CAPTURE_LEVEL_TICKS` | Saves a screenshot at these level ticks, for example `60,600`. In the special stage, use 20000 plus the tick of the stage |
| `S2HD_EXIT_AFTER_CAPTURES` | With `1`, quits after the last screenshot |
| `S2HD_TEST_START_RINGS` | Gives the player this number of rings at the start of a level |
| `S2HD_TEST_EMERALDS` | Gives the player this number of emeralds at the start of a level, or of `--halfpipe`. With `7` and 50 rings, Sonic can become Super Sonic |
| `S2HD_TEST_SPECIAL_STAGE_AT_TICK` | Starts the special stage at this level tick, one time only |
| `S2HD_TEST_SPECIAL_STAGE_RESULTS` | Shows the results screen of `--halfpipe` at once. The value is the rings, then 1 or 0 for the emerald, then 1 or 0 for all the rings, for example `20,1,1`. With `S2HD_TEST_EMERALDS=6` and `--halfpipe 7`, the Super Sonic message shows |
| `S2HD_TEST_EDITOR_INPUT` | Replaces the mouse and keyboard of the level editor with commands, for example `5:mouse 900 500;6:down left;7:up left`. See `TestHooks.cs` for the commands. While the editor is open, the screenshot numbers are 10000 plus the editor frame |
| `S2HD_TEST_STORY` | With `1`, plays the story with no input, as the story test describes. Each act saves a screenshot `story-ZONE-ACT.png` |
| `S2HD_TEST_LOG` | With `S2HD_TEST_STORY`, writes the log of the story test to this file |
| `S2HD_PHYSICS` | With `original` or `hd`, plays the levels with this physics. With `hd`, the tests use the golden screenshots in `tests/golden-hd/`, not `tests/golden/` |
