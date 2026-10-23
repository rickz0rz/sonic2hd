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
| `tools/` | Release scripts |

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
which is useful for testing. Keyboard controls match the desktop game. Audio, gamepads and the credits video are
not implemented yet, and a browser with WebGL2 and a keyboard is required.

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
