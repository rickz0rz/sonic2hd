#!/bin/bash
# Makes all the mods that come from a local copy of the Sonic 2 disassembly: the zones that the
# demo data does not have, their music, the special stage layouts and the attract demos. Run it
# from any folder. The music needs ffmpeg (or S2HD_FFMPEG).
#
#   ./tools/import-all.sh [s2disasm-dir] [mods-dir]
#
# s2disasm-dir: S2DISASM_DIR, or ../../sonicretro/s2disasm or ../s2disasm from the repository.
# mods-dir: S2HD_MODS_DIR, or the mods folder in the user data folder
#   (~/Library/Application Support/SonicOrca/mods on macOS, ~/Documents/SonicOrca/mods on other systems).
#
# The data that this script makes comes from the original game. It writes to bin/imports and to
# the mods folder only, so do not commit it. Run the script again after an update of the importer.
set -euo pipefail

ZONES=(arz cnz mcz ooz mtz scz wfz dez)

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

DISASM="${1:-${S2DISASM_DIR:-}}"
if [[ -z "$DISASM" ]]; then
	for candidate in "$REPO/../../sonicretro/s2disasm" "$REPO/../s2disasm"; do
		if [[ -f "$candidate/s2.asm" ]]; then
			DISASM="$candidate"
			break
		fi
	done
fi
if [[ -z "$DISASM" || ! -f "$DISASM/s2.asm" ]]; then
	echo "Cannot find the Sonic 2 disassembly. Give its folder as the first argument or in S2DISASM_DIR." >&2
	echo "To get it: git clone https://github.com/sonicretro/s2disasm.git ../s2disasm" >&2
	exit 1
fi
DISASM="$(cd "$DISASM" && pwd)"

if [[ "$(uname)" == "Darwin" ]]; then
	DEFAULT_MODS="$HOME/Library/Application Support/SonicOrca/mods"
else
	DEFAULT_MODS="$HOME/Documents/SonicOrca/mods"
fi
MODS="${2:-${S2HD_MODS_DIR:-$DEFAULT_MODS}}"

BASE="$REPO/data/sonicorca.dat"
if [[ ! -f "$BASE" ]]; then
	echo "Cannot find $BASE. Put the game data in the data folder first." >&2
	exit 1
fi

WORK="$REPO/bin/imports"
mkdir -p "$WORK" "$MODS"

echo "Disassembly: $DISASM"
echo "Mods folder: $MODS"

dotnet build "$REPO/tools/s2hd-data" -c Release -v quiet -nologo

s2hd_data() {
	dotnet run --project "$REPO/tools/s2hd-data" -c Release --no-build -- "$@"
}

# Imports one part into bin/imports/<name>, then packs only the changed resources into <name>.dat.
import() {
	local name="$1"
	shift
	echo "== $name"
	rm -rf "$WORK/$name"
	s2hd_data "$@" "$WORK/$name"
	s2hd_data pack "$WORK/$name" "$MODS/$name.dat" --base "$BASE"
}

for zone in "${ZONES[@]}"; do
	import "$zone" import-level "$zone" "$DISASM"
done
import special-stages import-special-stages "$DISASM"
# The ARZ and CNZ demos need the imports of these zones, which the loop above makes
import demos import-demos "$DISASM"
import music import-music "$DISASM"

echo "Done. The game loads the mods from $MODS."
