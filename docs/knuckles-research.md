# Knuckles mode: research

This document is research for a later Knuckles mode, as in "Knuckles in Sonic 2" (KiS2). It is not a plan of work yet. It records what the game changes and where the data is, so that later work starts from facts.

## The source

The `knuckles-in-sonic-2` branch of s2disasm is a disassembly of KiS2. It has its own history: it has no commit in common with `master`.

- The branch can build KiS2 alone, with `standaloneKiS2 = 1` in `s2.asm`. Then it takes all its data from its own files.
- The normal lock-on build reads four items from fixed addresses of the Sonic & Knuckles ROM: Knuckles' art, his mappings and DPLC, the final image of the ending, and the object layouts (`Off_Objects_KiS2`). `s2.lockon.asm` has the addresses. The branch also has all four items as files, so an import does not need the Sonic & Knuckles ROM.
- Each change has a comment that starts with `KiS2 (topic):`. `s2.asm` has 417 of these comments. The topics are listed at the start of `s2.asm`.

| Topic | Comments | Content |
| --- | --- | --- |
| Knuckles | 124 | Knuckles himself: art, mappings, animations, gliding, climbing, collision, and the objects that react to him |
| no 2P | 103 | Two-player mode is removed. |
| title | 60 | The KiS2 title screen |
| no Tails | 33 | Tails is removed: Knuckles is always alone. |
| bugfix | 33 | Bug fixes of Sonic 2 |
| ending | 16 | The ending |
| results | 11 | The results of the special stage |
| others | 37 | Lock-on, branch, mappings format, no options menu, unused code |

## Data files that differ from master

- Knuckles' art: `art/uncompressed/Knuckles' art.bin`.
- Knuckles' mappings and DPLC: `mappings/sprite/Knuckles.asm` and `mappings/spriteDPLC/Knuckles.asm`.
- His special stage art, the title screen, his life counter, his continue screen, and the final image of the ending.
- Knuckles versions of the monitor, the signpost and the slot machine pictures. A title card letter "K".
- Palettes: `SonicAndTails.bin` has Knuckles' colors. There are palettes for Super Knuckles and for the banner of the ending. The palettes of the ending, the special stage, the title, and the water of ARZ and CPZ changed.
- The object layouts, `level/objects/*.bin`, of 15 of the 17 acts. SCZ and DEZ did not change.
- The level layouts, the collision and the rings did not change.

## The changes in the levels

The levels have the same shape. Only the object placements are different. The table counts the placements that KiS2 adds, over all the acts:

| Object | ID | Added | Removed |
| --- | --- | --- | --- |
| Invisible solid block | `$74` | 233 | 31 |
| Layer switch (path swapper) | `$03` | 89 | 25 |
| Monitor | `$26` | 33 | 8 |
| Spikes | `$36` | 13 | 0 |
| Other objects (fans, platforms, blocks) | | 27 | 21 |

The invisible blocks are walls that stop Knuckles where his gliding or climbing could break the level. The layer switches give him the correct collision path in the places that he can climb to. The monitors reward him for exploring. The acts with the most changes are ARZ 2 (88 added), CPZ 2 (61) and CPZ 1 (52). MCZ and MTZ change very little, and SCZ and DEZ do not change.

Our engine already has these object types: `INVISIBLEBLOCK`, `LAYERSWITCH`, `MONITOR` and the others. The importer reads `level/objects/*.bin` and makes the bindings. The HD maps of EHZ, CPZ and HTZ keep the original layouts (see the notes of the level import), so the KiS2 placements also fit them.

## Knuckles' movement

KiS2 says that Knuckles is Sonic with small changes, and one extra routine for gliding and climbing. The routines in `s2.asm` of the branch:

- `Obj01_MdAir` calls `Obj01_MdAir_Gliding` while `double_jump_flag` is set.
- `Knuckles_BeginGlide` starts the glide. A double jump also starts the Super form (`Knuckles_TurnSuper`), not the top of a jump as for Sonic.
- `Knuckles_GlideControl`, `Knuckles_GlideSpeedControl` and `Knuckles_DoGlidingAnimation`: the glide.
- `Knuckles_BeginSlide`, `Knuckles_Sliding`, `Knuckles_FallingFromGlide`: the landing and the fall after a glide.
- `Knuckles_BeginClimb`, `Knuckles_Climbing_Wall`, `Knuckles_ClimbUp`, `Knuckles_LetGoOfWall`, `Knuckles_DoLedgeClimbingAnimation`, `Knuckles_Climbing_Onto_Ledge`: climbing.
- `Knuckles_DoLevelCollision2`: new level collision for the glide.

These routines are lines 38160 to about 38930, and 40108 to 40140, of the branch.

Other values and rules:

- The jump speed is `$600`, and `$300` under water. Sonic has `$680`. Super Knuckles does not jump higher.
- Super Knuckles moves slower than Super Sonic, has no art of his own, and has his own palette cycle.
- Knuckles balances in a simpler way, pushes faster, and starts his tumble at another frame.
- `KnucklesAni_*` are his animation scripts, with four new animations: glide, fall after a glide, climb a ledge, and hard landing.
- Monitors and enemies break when he glides into them, and he bounces out of the glide. Bosses and some objects stop his glide.
- `Obj08` (the dust) makes dust when he slides after a glide.

## Other changes

- **No Tails:** Knuckles is always alone. There is no sidekick and no "Sonic and Tails" mode.
- **Special stages:** Knuckles has his own art. The ring requirements are lower in `SpecialStage_RingReq_Alone`. Only the KiS2 table is different. The results screen shows "Knuckles".
- **Ending:** all the endings play in a similar way. A banner "KNUCKLES THE ECHIDNA IN" shows, and the Sonic 2 logo moves down. The final image is Knuckles. The chickens of the Tails ending are removed.
- **Slot machine:** the CNZ slot machine has a Knuckles picture.
- **Cutscenes:** some cutscenes changed their timing for Knuckles' physics. Some objects stop him from gliding there.
- **Title screen:** KiS2 has its own title screen, with no menu.
- **Bug fixes:** KiS2 fixes 33 bugs of Sonic 2, for example a shorter wind tunnel. A Knuckles mode can choose which of these to use.

## What the engine has now

- `CharacterType.Knuckles` and the key `SONICORCA/OBJECTS/KNUCKLES` in `Level`. `--protagonist knuckles` exists.
- `LevelHud` has a Knuckles triangle. It uses Sonic's art when a character has no art of its own.
- There is no `KNUCKLES` object, no Knuckles art, and no Knuckles animation group.
- `Character.cs` has all the movement of the characters. Sonic and Tails are small classes (`SonicInstance`, `TailsInstance`) that draw the body and set values such as `CanFly`.
- `SpriteImport.ParseMappings` and `SuperSonicArt.ParseDplc` read the `spritePiece` and `dplcEntry` macros. The Knuckles files use the same macros. The format difference of Sonic & Knuckles is in the macros, not in the files. So the importer can read Knuckles' sprites as it reads Super Sonic's.
- The Lua abilities (`ICharacterAbility`) have a hook for a jump press in the air. A glide can use it. Climbing needs new wall collision in `Character.cs`.

## A possible order of work

1. **Import:** the importer reads a second folder, the KiS2 branch, as it reads s2disasm. It makes Knuckles' sprites and his animation group from his art, mappings, DPLC and palette, as for Super Sonic. It also makes the KiS2 object placements as a second binding for each act.
2. **The character:** a `KNUCKLES` object with his jump speed, his animations, no sidekick, and the HUD.
3. **Glide:** the glide, the slide after a glide, and the fall. Monitors and enemies break when he glides into them.
4. **Climb:** climbing walls and ledges, with new wall collision in `Character.cs`. This step has the most risk, because it changes shared physics.
5. **Levels:** Knuckles mode uses the KiS2 bindings.
6. **The rest:** Super Knuckles, the special stage art and ring requirements, the results, the ending, and the title.

## Open questions

- How does the user give the importer the KiS2 branch? A second folder or zip next to s2disasm is the simplest. The web build can take a second zip in the same way.
- Does Knuckles mode replace the bindings of an act, or does the level choose the binding when it loads?
- Which bug fixes of KiS2 does the game also use for Sonic?
