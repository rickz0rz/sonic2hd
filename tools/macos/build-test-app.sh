#!/bin/bash
# Makes a copy of S2HD.app that has the mods of the local user data folder in it, for tests on this Mac or on
# another Mac. Run tools/macos/build-app.sh first.
#
#   ./build-test-app.sh [output-app] [mods-folder]
#
# The default output is publish/S2HD-Test.app. The default mods folder is
# ~/Library/Application Support/SonicOrca/mods.
#
# The script copies each mod (a .dat file or a folder) into Contents/Resources/mods of the copy. The game loads
# that folder before the mods of the user data folder. The saved changes of the level editor (mods/editor/*.dat)
# go in as plain mod files with the prefix "zz-editor-", so that they load after the other mods, as in the game.
# The changes that the editor did not save (mods/editor/unsaved) do not go in.
#
# The mods can have data from the game or from the disassembly. Do not share the copy where that is not allowed.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
SOURCE_APP="$REPO/publish/S2HD.app"
APP="${1:-$REPO/publish/S2HD-Test.app}"
MODS="${2:-$HOME/Library/Application Support/SonicOrca/mods}"

log() { printf '\033[1m==> %s\033[0m\n' "$*"; }
die() { printf 'error: %s\n' "$*" >&2; exit 1; }

[[ -d "$SOURCE_APP" ]] || die "$SOURCE_APP does not exist. Run tools/macos/build-app.sh first."
[[ -d "$MODS" ]] || die "the mods folder $MODS does not exist"
[[ "$(cd "$(dirname "$APP")" && pwd)/$(basename "$APP")" != "$SOURCE_APP" ]] || die "the output must not be $SOURCE_APP"

log "Copying $SOURCE_APP to $APP"
rm -rf "$APP"
# ditto keeps the signatures of the files in Contents/MacOS. They are in extended attributes for files that are
# not programs.
ditto "$SOURCE_APP" "$APP"
TARGET="$APP/Contents/Resources/mods"
mkdir -p "$TARGET"

count=0
shopt -s nullglob
for item in "$MODS"/*; do
  name="$(basename "$item")"
  case "$name" in
    .*) continue ;;
    editor)
      # The saved changes of the editor, one file for each zone
      for file in "$item"/*.dat; do
        cp "$file" "$TARGET/zz-editor-$(basename "$file")"
        log "Editor changes: $(basename "$file")"
        count=$((count + 1))
      done
      ;;
    *)
      if [[ -d "$item" || "$name" == *.dat ]]; then
        cp -R "$item" "$TARGET/"
        log "Mod: $name"
        count=$((count + 1))
      fi
      ;;
  esac
done

# The files in Contents/MacOS keep their signatures. A new signature of the bundle seals the mods in Resources.
log "Signing (ad-hoc)"
codesign --force --sign - "$APP"
codesign --verify --deep --strict "$APP"
log "Done: $APP with $count mods ($(du -sh "$APP" | cut -f1))"
echo "Launch with: open \"$APP\""
echo "The copy also loads the mods of the user data folder, after its own mods."
