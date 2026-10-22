#!/bin/bash
# Builds S2HD.app from the osx-arm64 publish output. Run on an Apple Silicon Mac with the
# Xcode Command Line Tools installed (xcode-select --install).
#
#   ./build-app.sh [publish-dir] [output-dir]
#
# Works from the macOS kit made by tools/publish.ps1 -Runtime osx-arm64 (S2HD-osx-arm64/ and S2HD.png
# beside this script), or from a repository checkout (publish/S2HD-osx-arm64 and assets/S2HD.png).
#
# Bundled native libraries (cached in ~/Library/Caches/S2HD-build between runs):
#   - SDL2: official release framework binary (zlib license)
#   - FFmpeg: minimal LGPL build from source that only decodes H.264 in MP4, for the credits film
set -euo pipefail

SDL2_VERSION=2.32.10
FFMPEG_VERSION=7.1.3
BUNDLE_ID="${S2HD_BUNDLE_ID:-local.s2hd.game}"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ -d "$HERE/S2HD-osx-arm64" ]]; then
  DEFAULT_PUBLISH="$HERE/S2HD-osx-arm64"; ICON_PNG="$HERE/S2HD.png"; DEFAULT_OUT="$HERE"
else
  REPO="$(cd "$HERE/../.." && pwd)"
  DEFAULT_PUBLISH="$REPO/publish/S2HD-osx-arm64"; ICON_PNG="$REPO/assets/S2HD.png"; DEFAULT_OUT="$REPO/publish"
fi
PUBLISH_DIR="$(cd "${1:-$DEFAULT_PUBLISH}" && pwd)"
OUT_DIR="${2:-$DEFAULT_OUT}"
APP="$OUT_DIR/S2HD.app"
CACHE="${S2HD_CACHE:-$HOME/Library/Caches/S2HD-build}"

log() { printf '\033[1m==> %s\033[0m\n' "$*"; }
die() { printf 'error: %s\n' "$*" >&2; exit 1; }

check_prerequisites() {
  [[ "$(uname -s)" == Darwin ]] || die "this script must run on macOS"
  [[ "$(uname -m)" == arm64 ]] || die "this build targets Apple Silicon (arm64)"
  xcode-select -p >/dev/null 2>&1 || die "Xcode Command Line Tools are required: xcode-select --install"
  for tool in clang make curl tar hdiutil codesign install_name_tool sips iconutil; do
    command -v "$tool" >/dev/null || die "missing required tool: $tool"
  done
  [[ -f "$PUBLISH_DIR/S2HD" && -d "$PUBLISH_DIR/data" && -d "$PUBLISH_DIR/shaders" ]] ||
    die "$PUBLISH_DIR does not look like an osx-arm64 publish folder (expected S2HD, data/, shaders/)"
}

fetch_sdl2() {
  local lib="$CACHE/sdl2-$SDL2_VERSION/libSDL2.dylib"
  if [[ -f "$lib" ]]; then log "SDL2 $SDL2_VERSION (cached)"; SDL2_LIB="$lib"; return; fi
  log "Downloading SDL2 $SDL2_VERSION"
  local dir="$CACHE/sdl2-$SDL2_VERSION" dmg="$CACHE/SDL2-$SDL2_VERSION.dmg" mnt
  mkdir -p "$dir"
  curl -fL --retry 3 -o "$dmg" "https://github.com/libsdl-org/SDL/releases/download/release-$SDL2_VERSION/SDL2-$SDL2_VERSION.dmg"
  mnt="$(mktemp -d)"
  hdiutil attach -nobrowse -readonly -mountpoint "$mnt" "$dmg" >/dev/null
  cp "$mnt/SDL2.framework/Versions/A/SDL2" "$dir/libSDL2.dylib.tmp"
  hdiutil detach "$mnt" >/dev/null
  chmod u+w "$dir/libSDL2.dylib.tmp"
  install_name_tool -id @rpath/libSDL2.dylib "$dir/libSDL2.dylib.tmp"
  mv "$dir/libSDL2.dylib.tmp" "$lib"
  rm -f "$dmg"
  SDL2_LIB="$lib"
}

build_ffmpeg() {
  local prefix="$CACHE/ffmpeg-$FFMPEG_VERSION"
  if [[ -f "$prefix/lib/libavcodec.61.dylib" ]]; then log "FFmpeg $FFMPEG_VERSION (cached)"; FFMPEG_LIB_DIR="$prefix/lib"; return; fi
  log "Building minimal FFmpeg $FFMPEG_VERSION (a few minutes, once)"
  local src="$CACHE/src"
  mkdir -p "$src"
  curl -fL --retry 3 "https://ffmpeg.org/releases/ffmpeg-$FFMPEG_VERSION.tar.xz" | tar -xJ -C "$src"
  (
    cd "$src/ffmpeg-$FFMPEG_VERSION"
    # Only what FFMediaToolkit needs for credits.film (H.264 yuv420p in MP4): avutil, avcodec, avformat,
    # swscale, swresample. @rpath install names + @loader_path rpath let the libraries find each other.
    ./configure \
      --prefix="$prefix" --arch=arm64 --cc=clang \
      --enable-shared --disable-static --disable-programs --disable-doc --disable-debug \
      --disable-autodetect --disable-network --disable-everything \
      --disable-avdevice --disable-avfilter --disable-postproc \
      --enable-decoder=h264 --enable-parser=h264 --enable-demuxer=mov --enable-protocol=file \
      --enable-swscale --enable-swresample \
      --install-name-dir='@rpath' --extra-ldflags='-Wl,-rpath,@loader_path'
    make -j"$(sysctl -n hw.ncpu)"
    make install
  )
  rm -rf "$src/ffmpeg-$FFMPEG_VERSION"
  FFMPEG_LIB_DIR="$prefix/lib"
}

write_info_plist() {
  cat > "$APP/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>S2HD</string>
  <key>CFBundleDisplayName</key><string>Sonic 2 HD</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleExecutable</key><string>S2HD</string>
  <key>CFBundleIconFile</key><string>S2HD</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>2.0.1012</string>
  <key>CFBundleVersion</key><string>2.0.1012.10521</string>
  <key>LSMinimumSystemVersion</key><string>14.0</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.arcade-games</string>
  <key>NSHighResolutionCapable</key><false/>
</dict>
</plist>
EOF
}

make_icns() {
  [[ -f "$ICON_PNG" ]] || { log "No icon found at $ICON_PNG; skipping"; return; }
  local iconset; iconset="$(mktemp -d)/S2HD.iconset"
  mkdir -p "$iconset"
  for size in 16 32 128 256; do
    sips -z $size $size "$ICON_PNG" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    sips -z $((size * 2)) $((size * 2)) "$ICON_PNG" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
  done
  iconutil -c icns "$iconset" -o "$APP/Contents/Resources/S2HD.icns"
}

assemble_app() {
  log "Assembling $APP"
  rm -rf "$APP"
  mkdir -p "$APP/Contents/MacOS/ffmpeg" "$APP/Contents/Resources"
  # Code and native libraries go in MacOS; read-only game content in Resources (see ContentDirectory)
  rsync -a --exclude '/data' --exclude '/shaders' --exclude '/credits.film' --exclude '*.pdb' \
    "$PUBLISH_DIR/" "$APP/Contents/MacOS/"
  cp -R "$PUBLISH_DIR/data" "$PUBLISH_DIR/shaders" "$APP/Contents/Resources/"
  [[ -f "$PUBLISH_DIR/credits.film" ]] && cp "$PUBLISH_DIR/credits.film" "$APP/Contents/Resources/"
  cp "$SDL2_LIB" "$APP/Contents/MacOS/libSDL2.dylib"
  # FFmpeg.AutoGen loads lib<name>.<major>.dylib; copy those names as real files (not symlinks)
  for lib in "$FFMPEG_LIB_DIR"/lib*.dylib; do
    [[ "$(basename "$lib")" =~ ^lib[a-z]+\.[0-9]+\.dylib$ ]] && cp -L "$lib" "$APP/Contents/MacOS/ffmpeg/"
  done
  chmod +x "$APP/Contents/MacOS/S2HD"
  write_info_plist
  make_icns
}

sign_app() {
  log "Signing (ad-hoc)"
  # Files copied from Windows or downloaded may carry quarantine attributes
  xattr -cr "$APP"
  find "$APP/Contents/MacOS" -name '*.dylib' -exec codesign --force --sign - {} \;
  codesign --force --sign - "$APP/Contents/MacOS/S2HD"
  codesign --force --sign - "$APP"
  codesign --verify --deep --strict "$APP"
}

check_prerequisites
mkdir -p "$CACHE" "$OUT_DIR"
fetch_sdl2
build_ffmpeg
assemble_app
sign_app
log "Done: $APP ($(du -sh "$APP" | cut -f1))"
echo "Launch with: open \"$APP\"   (logs: ~/Library/Application Support/SonicOrca/sonicorca.log)"
