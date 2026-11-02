# Two-player mode: design

This document is the design of the two-player modes. It records the decisions of the user, what the game has now, and the work for a versus mode.

## Decisions

The user made these decisions (2026-11-01):

- Co-op comes first: player 2 plays Tails on the same screen, as in the main game of Sonic 2.
- Player 2 uses a second game pad or a second set of keys on the same keyboard.
- A versus mode comes later. It splits the screen into a top half and a bottom half, as the original does.
- Versus mode has the levels of the original: Emerald Hill, Mystic Cave, Casino Night and the special stage.

## Co-op (done)

Co-op works. PR #257 added a test and the README text.

- `Controller` gives player 2 the keys I, J, K, L (directions) and Q, W, E (jump), and the second game pad.
- `Level` reads the input of the sidekick from player 2 (`Player.SidekickGamepadIndex`).
- `Character.UpdateSidekick` gives Tails to player 2 at the first input. The computer takes Tails back 10 seconds after the last input.
- The camera follows Sonic. Both players collect rings for the same count.
- `S2HD_TEST_PLAYER2_PLAYBACK` plays a recording as the input of player 2, for tests.

## Versus mode in the original

The disassembly (s2disasm, `master`) has these parts:

- **Flow:** a two-player level select (`LevelSelectMenu2P`), then the levels, then results screens (`TwoPlayerResults`). The level select has Emerald Hill, Mystic Cave, Casino Night and the special stage. Each zone has two acts.
- **Results:** `Setup2PResults_Act`, `_Zone`, `_Game`, `_SpecialAct` and `_SpecialZone`. An act compares the score, the time, the rings, the total rings and the monitors that each player broke. A zone that ends in a tie goes to a special stage that breaks the tie.
- **Time limit:** when one player ends the act, the other player gets 60 seconds (`Loser_Time_Left`).
- **Each player** has their own rings, score, time and extra lives (`Ring_count_2P`, `Score_2P`, `Timer_2P` and others).
- **Monitors:** two-player mode has a teleport (`Teleport_flag`): the two players change places.
- **No bosses:** in two-player mode, the level events of act 2 of the three zones (`LevEvents_EHZ2_Routine1` and the others) skip the boss arena. Act 2 ends at a signpost, as act 1 does. These act 2 signposts (`Obj0D`) are in the object layouts only for two-player mode, and the level importer skips them now.
- **Data:** Casino Night has its own object layouts for two players (`level/objects/CNZ_1_2P.bin` and `CNZ_2_2P.bin`). The music has two-player versions (`MusID_EHZ_2P`, `MusID_MCZ_2P`, `MusID_CNZ_2P`). The special stage has two-player palettes (`Pal_SS1_2p` to `Pal_SS3_2p`). The art has the text of the menus and the results (`1Player2VS.nem`, `1P and 2P wins text from 2P mode.nem`).
- The original draws the two halves with a squeezed screen mode of the Mega Drive. The HD game does not need it: each half is 1920 x 540.

## What the engine has now

- One `Camera` in `Level`. `Level.Draw` makes one `Viewport` for the whole screen. `Viewport` has `Bounds` (the area of the level) and `Destination` (the area of the screen), and the object renderer clips to the destination.
- `LevelRenderer` draws through a canvas of the size of the screen, with post effects: water, lighting, and the fade.
- One `Player` in `Level`, with a protagonist and a sidekick. The HUD shows one player.
- Many objects use `Level.Player.Protagonist`, for example the boss arenas, the signpost, the camera locks and the star posts. In versus mode, each must use the nearest player or both players.
- Up to four game pads (`InputState.GamePad`). Players 1 and 2 have keys too.
- The special stage already has two players with their own rings (Tails in the special stage).

## The work for versus mode

1. **Two players in the level (done):** `Level` gets a list of players. Player 2 has their own character (Tails) as a protagonist, not as a sidekick. Each player has their own rings, score and time. Objects that use `Level.Player` change to the player of the character that touches them, or to all players.
2. **Two cameras and a split screen (done):** a camera for each player. `Level.Draw` draws the level two times, into the top half and the bottom half. The post effects (water, lighting) must work in each half. This step has the most risk, because it changes the renderer.
3. **A HUD for each player (done)** in their half of the screen.
4. **The rules:** the 60-second time limit, the end of an act when both players finished or the time ran out, game over in versus mode, and the teleport monitor.
5. **The flow:** the versus level select, the act, zone and game results, and the special stage that breaks a tie.
6. **Data:** the two-player object layouts of Casino Night and the two-player music, in the importer. The special stage in versus mode.

## Status

- Step 1 is done: `--versus` (`LevelPrepareSettings.Versus`) makes player 2 a second protagonist, Tails, with their own `Player`: rings, score and lives. `Level.Players` lists the players. The Casino Night bumper, bonus block and slot cage give their points and rings to the player of the character. The camera and the HUD still show player 1. The death of player 2, the signpost, the star posts and the monitor icons are for step 4.
- Step 2 is done: in versus mode, `Level` has a camera for each player (`Level.Cameras`), each for one half of the screen (1920 x 540). `Level.Draw` draws the level two times, player 1 at the top, and a black line between the halves. Each camera starts zoomed out (a scale of 2/3: 2880 x 810 units of the level in each half). Z or the Select button of pad 1, and R or the Select button of pad 2, change the zoom of a player. Objects stay alive around both cameras. `LevelRenderer` draws a zoomed-out view at the normal scale into a larger canvas and makes the copy smaller, because tiles that are drawn smaller leave thin lines between them. - Step 3 is done: in versus mode, `LevelHud` draws a HUD for each player in their half, at half size: the score, the time and the rings at the top left, and the lives with the icon of the character at the bottom left. The time is the time of the level for both players. A time for each player is part of step 4.

## Decisions for versus mode

The user made these decisions (2026-11-01):

- **Casino Night layouts:** an option when a versus match starts: the two-player layouts of the original, or the layouts of the main game.
- **Zoom:** each player has their own zoom. A match starts zoomed out, so that each half shows more of the level. Each player has a key that changes between zoomed out and zoomed in.
- **Characters:** player 1 is Sonic and player 2 is Tails, as in the original. A choice of character can come later.
