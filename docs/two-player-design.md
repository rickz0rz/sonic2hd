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
- **No bosses:** in two-player mode, the level events of act 2 of the three zones (`LevEvents_EHZ2_Routine1` and the others) skip the boss arena. Act 2 ends at a signpost, as act 1 does. These act 2 signposts (`Obj0D`) are in the object layouts only for two-player mode. The level importer marks them as versus only.
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
4. **The rules (done):** the death and the respawn of each player, the star posts and the time of each player (4a), the 60-second time limit, the end of an act when both players finished or the time ran out, game over in versus mode, and the teleport monitor.
5. **The flow (done):** the versus level select, the act, zone and game results (5a), and the special stage that breaks a tie (5b).
6. **Data:** the two-player object layouts of Casino Night and the two-player music, in the importer. The special stage in versus mode.

## Status

- Step 1 is done: `--versus` (`LevelPrepareSettings.Versus`) makes player 2 a second protagonist, Tails, with their own `Player`: rings, score and lives. `Level.Players` lists the players. The Casino Night bumper, bonus block and slot cage give their points and rings to the player of the character. The camera and the HUD still show player 1. The death of player 2, the signpost, the star posts and the monitor icons are for step 4.
- Step 2 is done: in versus mode, `Level` has a camera for each player (`Level.Cameras`), each for one half of the screen (1920 x 540). `Level.Draw` draws the level two times, player 1 at the top, and a black line between the halves. Each camera starts zoomed out (a scale of 2/3: 2880 x 810 units of the level in each half). Z or the Select button of pad 1, and R or the Select button of pad 2, change the zoom of a player. Objects stay alive around both cameras. `LevelRenderer` draws a zoomed-out view at the normal scale into a larger canvas and makes the copy smaller, because tiles that are drawn smaller leave thin lines between them.
- Step 3 is done: in versus mode, `LevelHud` draws a HUD for each player in their half, at half size: the score, the time and the rings at the top left, and the lives with the icon of the character at the bottom left. The time is the time of the level for both players. A time for each player is part of step 4.
- Step 4a is done: in versus mode, a dead player does not restart the level, as `Obj01_ResetLevel_Part2` of the original. The player loses a life and their rings (the total of the act stays), and comes back at their own star post or their start, in the air. Their camera goes to them at once: the original scrolls back, but the HD levels are large. A player with no lives left starts the game over of the level. Each player activates each star post for themselves. The star glows when any player activated it, and versus mode opens no star circles. `Player.StoppedTime` holds the time of a player who finished, and the HUD shows it. Step 4b sets it. `S2HD_TEST_KILL_PLAYER2_AT_TICK` kills player 2 for tests.
- Step 4b is done: in versus mode, the signpost stops the time of each player who passes it (`VersusRules.FinishPlayer`). The sign turns to the face of the first player. The other player then has 60 seconds (`VersusRules.TimeLeft`), and their HUD counts down in red. The act ends when both players finished or the time ran out. Act 1 then goes on to act 2, and act 2 ends the level. In act 2 of Emerald Hill, Mystic Cave and Casino Night, versus mode has no boss. The importer marks the act 2 signposts of the original as `VersusOnly`, and only versus mode starts them. Emerald Hill 2 adds its signpost from the area script. The teleport monitor is step 4c.
- Step 4c is done: in versus mode, each monitor shows a question mark, as Obj26_Init of the original. When a player breaks it, the level tick chooses the item (`Level_frame_counter & 7` in Obj2E_Init): the extra life of Sonic or of Tails, Robotnik, rings, speed shoes, a barrier, invincibility or the teleport. Each extra life goes to the player of its icon, whoever breaks the monitor. The teleport (`VersusRules.SwapPlayers`) changes the places of the players: the position, the layer, the path, the speed and the spin. Nothing happens when a player is dead or comes back. The power-ups stay with each player. The original swaps them with the places. The level then stops for 64 ticks, and the screen flashes white. A monitor gives invincibility in place of the teleport when a player has finished the act. The HD data has no teleport sound, so the game plays the sound of the special stage ring. `S2HD_TEST_VERSUS_MONITOR_ITEM` (1 to 8) chooses the item for tests. The option "teleport only" of the original is part of step 5.
- Step 5a is done: "2P VS" on the title screen, or `--versus`, starts a versus match (`VersusGameState`). The setup screen (`VersusSetupScreen`, as LevelSelectMenu2P of the original) shows the zones with the winner of each zone that the players played, and the items option: all kinds or teleport only. Each act is a level of its own: at the end of act 1, the level fades out, and act 2 starts at its own start, as in the original. The results screens (`VersusResultsScreen`) show the act, the zone and the game. `VersusResults.cs` has the rules of Setup2PResults_Act: each of the score, the time (lower wins), the rings, the total rings and the monitors gives a point. The player with more points wins the act, and the player with more acts wins the zone and then the game. A tie shows as "DRAW" until step 5b. A game over ends the match and clears the results, as in the original. The lives of each player go on from act to act. A zone shows only when its level is available: a new user folder has only Emerald Hill. `S2HD_TEST_VERSUS_RESULTS_FRAMES` ends each results screen after a number of frames, for tests. The tests capture the versus screens from capture number 100000 (`TestHooks.VersusScreenCaptureOffset`).
- Step 5b is done: when the acts of a zone end in a tie, the special stage of the zone breaks the tie, as in the original (Emerald Hill plays stage 2, Mystic Cave stage 3, Casino Night stage 4). `HalfPipeSpecialStageScreen` has a versus mode, as SS_2p_Flag of the original: player 2 plays Tails on game pad 2 with no help of the computer, there are no ring goals and no emerald, and each checkpoint ends a segment (Obj5A_VSReset). The screen keeps the rings of each player in each segment and shows the winner of the segment. `VersusSpecialStageResult` gives the segments to the player with more rings, and the stage to the player with more segments. It counts as one more act of the zone, and the zone results show it. `--halfpipe 2 --versus` plays the versus special stage for tests. The special stage as a zone of its own (the fourth zone of the original level select) is part of step 6.

- Step 6a is done: versus mode plays the two-player songs. Emerald Hill plays `SONICORCA/MUSIC/LEVELS/EHZ/COMPETITIVE` of the game data in both acts. The import of the music makes `MCZ/COMPETITIVE` and `CNZ/COMPETITIVE` from `83 - MCZ 2P.asm` and `88 - CNZ 2P.asm`. A zone without its two-player song plays its normal song. In versus mode, the speed shoes of Emerald Hill play their jingle, because the fast songs are copies of the songs of the main game. The results screens play `SONICORCA/MENU/COMPETITIVE/MUSIC`, as MusID_2PResult of the original. The setup screen keeps the options song, as the original menu does. `SoundManager.PlayMusic` writes "Music: KEY" to the log.

- Step 6b is done: the import of Casino Night also writes `BINDING2P`, the binding of versus mode with the two-player object layouts of the original (`CNZ_1_2P.bin`, `CNZ_2_2P.bin`). It has the same rings, map bumpers and slot reels, and fewer bumpers and bonus blocks. The setup screen has the option "CASINO NIGHT: 2P OBJECTS / 1P OBJECTS" (`VersusMatch.TwoPlayerLayouts`, `LevelPrepareSettings.VersusTwoPlayerLayouts`). The default is the two-player layouts, as in the original. The option shows only when the match has Casino Night. The level editor changes the binding of the main game only. The import version is 21.

- Step 6c is done: the setup screen has a fourth zone, "SPECIAL STAGE", as the level select of two-player mode of the original (`loc_8E3A`). The zone has no area. It plays special stage 5, then stage 6, and stage 7 only when the first two stages gave each player one win (`SS_Total_Won` after the second stage). `VersusZoneResult.SpecialStages` keeps the result of each stage, and the zone results show a row for each stage. The tie-break of the other zones uses the same list. `S2HD_TEST_VERSUS_ZONE` starts a zone at once, and `S2HD_TEST_VERSUS_SPECIAL_STAGE_SEGMENTS` ends each versus special stage at once with the rings of the test. With this step, the plan of versus mode is done.

- The rules of versus mode in a level are in `VersusRules` (`engine/SonicOrca/SonicOrca.Core/VersusRules.cs`): the zoom, the end of an act, the time limit, the deaths and the teleport. `Level` makes the rules after it made the players and the cameras, and `Level.VersusRules` is null outside versus mode.

## Decisions for versus mode

The user made these decisions (2026-11-01):

- **Casino Night layouts:** an option when a versus match starts: the two-player layouts of the original, or the layouts of the main game.
- **Zoom:** each player has their own zoom. A match starts zoomed out, so that each half shows more of the level. Each player has a key that changes between zoomed out and zoomed in.
- **Characters:** player 1 is Sonic and player 2 is Tails, as in the original. A choice of character can come later.
