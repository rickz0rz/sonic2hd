# Sonic 2 HD (S2HD) — .NET 10 port

A port of the abandoned **Sonic 2 HD 2.0.1012-rc2** demo (originally .NET Framework 4.5, Windows only) to
cross-platform **.NET 10**, with a new native **macOS (Apple Silicon)** build. The game and its SonicOrca engine
were decompiled from the released binaries; all credit for the game goes to the original S2HD team. This is a
non-profit fan project, not affiliated with SEGA or Sonic Team.

## Layout

| Path | Contents |
|---|---|
| `S2HD/`, `SONICORCA/` | Game code (title, menus, levels, objects) |
| `engine/` | Decompiled SonicOrca engine projects |
| `assets/` | Shaders (patched for core-profile OpenGL) and the app icon |
| `lib/` | `SDL2-CS.dll`; FFmpeg for Windows is downloaded into `lib/ffmpeg` |
| `tests/` | Tests: the resource file format and a screenshot regression test |
| `tools/` | Release scripts, the resource tool `s2hd-data` and the local web server |

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
which is useful for testing. Keyboard controls match the desktop game. Sound starts at the first key press or click,
because browsers block audio until then. Gamepads and the credits video are not implemented yet, and a browser
with WebGL2 and a keyboard is required.

To put the game data into the site, add `-p:BundleGameData=true` to the publish command. The build then writes the
`data/` and `mods/` folders to `wwwroot/game.zip`, and the page downloads it and starts without asking for files. It reads the
game files from `GameDir`, as the desktop build does. The zip is about 115 MB. Some hosts limit file size, for
example GitHub Pages (100 MB) and Cloudflare Pages (25 MB). The game data belongs to its owners, so only publish it
where you have permission. A publish without the property removes `game.zip`.

When players choose their own files, the page also loads the `.dat` files in a `mods/` folder of the zip or folder.

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
checkpoints and emerald. The layouts are data from the original game, so do not commit them to this repository.
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

On a flipper or a spring launcher in CNZ, the jump button controls the object. Press jump to flip a flipper. Hold
jump to pull back a launcher, then let go to launch.

The last act of each zone ends with the boss of the original. The camera locks into the arena at the positions of
the original level events. The HD screen is wider than the original screen, so the arena shows 80 more pixels on each
side. In OOZ, the area adds the oil: a character sinks slowly into it and can jump out.

These zones have no music, because the demo data has none for them. A mod can add the music that each area names,
for example `SONICORCA/MUSIC/LEVELS/CNZ` or `SONICORCA/MUSIC/LEVELS/OOZ`. The data also has no sounds for some
objects, for example the bumpers, the arrow shooters and the lasers of the OOZ boss. These objects play the nearest
sound that the data has, or no sound.

### Entering a special stage

Touch a star post with 50 rings or more and fewer than 7 emeralds. A circle of stars then appears above the star
post. Jump into the circle to start the special stage. The circle goes away after about 8 seconds. After the stage,
the player starts again at the star post with 0 rings. The stage score goes into the level score. A won stage
gives an emerald, and the next star circle opens the next stage. A failed stage opens the same stage again.

When Tails follows Sonic in the level, Tails also plays in the special stage, with the ring quotas for two
players. The computer controls Tails: Tails does what Sonic did 16 frames before. When the second pad has input,
the second player controls Tails. The computer takes control again 3 seconds after the last input. Each player
has separate rings, and the goals use the total. A jump or a bomb changes the lanes of the two players.

The behavior of levels and objects is C# code in `SONICORCA/`, not data. The `AREA.area.cs` and `.object.cs`
resources only name the class to use.

## Command-line options

| Option | Effect |
|---|---|
| `--level <zone> [--act <n>]` | Starts a level directly, for example `--level htz --act 2` |
| `--protagonist <name>`, `--sidekick <name>` | Chooses the characters for `--level` (`sonic`, `tails`, `knuckles`) |
| `--debug` | Starts the level with debug mode on |
| `--demo` | Plays the attract demo (the demos of all zones that have one play in turn) |
| `--record <file>` | Records the input of the level to a file, for example for a demo |
| `--playback <file>` | Plays a recorded input in place of the controller |
| `--nologos` | Skips the disclaimer and the logo screens |
| `--credits`, `--options` | Opens the credits or the options screen |
| `--halfpipe [1-7]` | Opens a 3D special stage (in development). Left and right move Sonic around the pipe, and an action button jumps, with the physics of Sonic 2. Collect enough rings for each of the three checkpoints, and avoid the bombs. Tails follows Sonic. Add `--sidekick none` to play Sonic alone. Escape closes it |

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
dotnet test --project tests/S2HD.Tests --filter-not-trait "Category=Regression"   # fast tests
dotnet test --project tests/S2HD.Tests                                             # all tests
```

The resource file tests make their own small files. The game data tests read the real `sonicorca.dat` and are
skipped when the game files are not found. Set `S2HD_GAME_DIR` if they are not in the default location.

The regression tests run the release build and save screenshots at fixed level ticks:

- One test plays the Emerald Hill attract demo, which moves the player through the first part of the level.
- One test for each level that the demo data contains (EHZ, CPZ, HTZ, MTZ and DEZ, acts 1 and 2) starts the level
  with no input.

The tests compare the screenshots with golden screenshots. A run takes about 2 minutes. The golden screenshots come
from the game files, so Git ignores them (`tests/golden/`). Record them once from a known-good build:

1. Build the game for your platform, as the sections above describe.
2. Run `S2HD_RECORD_GOLDEN=1 dotnet test --project tests/S2HD.Tests --filter-trait "Category=Regression"`.

After a change, build the game again and run the tests. The tests copy screenshots that do not match to
`tests/golden/actual`. When a change is meant to change what the game draws, check the new screenshots, then record
the golden screenshots again. Set `S2HD_EXE` to test a build in a different location.

The test uses these environment variables, which you can also set by hand:

| Variable | Effect |
|---|---|
| `S2HD_USER_DATA_DIR` | Uses this folder for the log, settings and screenshots |
| `S2HD_RANDOM_SEED` | Seeds the random number generators of the level and the particles |
| `S2HD_CAPTURE_LEVEL_TICKS` | Saves a screenshot at these level ticks, for example `60,600` |
| `S2HD_EXIT_AFTER_CAPTURES` | With `1`, quits after the last screenshot |
| `S2HD_TEST_START_RINGS` | Gives the player this number of rings at the start of a level |
| `S2HD_TEST_SPECIAL_STAGE_AT_TICK` | Starts the special stage at this level tick, one time only |
