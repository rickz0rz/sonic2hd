using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

/// <summary>One act of a zone of the original game, and where it goes in the SonicOrca map of the zone.</summary>
/// <param name="Offset">The position of the act in the zone map, in original pixels. A multiple of 128 (one chunk).</param>
/// <param name="FirstColumn">The first map column, in blocks, that shows this act. The columns before it show the previous act.</param>
/// <param name="Size">The size of the act, in original pixels: the camera limits of LevelSize plus one screen.</param>
/// <param name="Top">The top camera limit of LevelSize, in original pixels from the top of the act.</param>
/// <param name="BumpersFile">The bumpers in the landscape (CNZ only), or null.</param>
/// <param name="WaterHeight">The top of the water (WaterHeight), in original pixels from the top of the act, or -1 without water.</param>
public sealed record OriginalAct(int Number, string LayoutFile, string ObjectsFile, string RingsFile, string StartFile,
	Vector2i Offset, int FirstColumn, Vector2i Size, string BumpersFile = null, int Top = 0, int WaterHeight = -1);

/// <summary>
/// A change to the level layout that the code of the original makes during play, for example a wall that closes a
/// boss arena. The import writes the changed tiles and collision lines into the map, switched off. The object or
/// the area that makes the change switches them on with <c>LayoutChanges.Apply</c>.
/// </summary>
/// <param name="Name">The name of the change, for example "boss_walls".</param>
/// <param name="Act">The act of the layout.</param>
/// <param name="Chunks">The new chunks: the column and the row in the layout of the act, and the chunk number.</param>
public sealed record LayoutChange(string Name, int Act, IReadOnlyList<(int Column, int Row, int Chunk)> Chunks);

/// <summary>The background layer of an HD map: its name, its height in blocks and its parallax.</summary>
/// <param name="RowHeight">The height of the parallax row, in HD pixels.</param>
public sealed record BackgroundLayer(string Name, int Rows, double ParallaxX, double ParallaxY, int RowHeight);

/// <summary>A zone of the original game, as the files of the Sonic 2 disassembly (github.com/sonicretro/s2disasm) give it.</summary>
/// <param name="Key">The resource key of the zone, for example "SONICORCA/LEVELS/CNZ".</param>
/// <param name="SecondaryCollisionFile">The collision index of the second path, or null when the zone has one path.</param>
/// <param name="MapSize">The size of the front layer, in blocks (16 original pixels).</param>
/// <param name="PaletteFile">The zone palette, for the sprites of its objects.</param>
/// <param name="Sprites">The art of the objects that the game data does not have.</param>
/// <param name="ArtFile">The level art (Kosinski), for the tiles in front of the characters.</param>
/// <param name="BlocksFile">The 16 x 16 block mappings (Kosinski).</param>
/// <param name="HdTiles">The number of tiles in the HD tile set of the zone.</param>
/// <param name="Background">The background layer of the HD map, or null for a plain background.</param>
/// <param name="FrontName">The name of the front layer of the HD map.</param>
public sealed record OriginalZone(string Name, string Key, string ChunksFile, string PrimaryCollisionFile,
	string SecondaryCollisionFile, IReadOnlyList<OriginalAct> Acts, Vector2i MapSize, string PaletteFile,
	IReadOnlyList<OriginalSprite> Sprites, string ArtFile = null, string BlocksFile = null, int HdTiles = 0,
	BackgroundLayer Background = null, string FrontName = "front")
{
	/// <summary>Objects without art that the area adds, for example the oil of Oil Ocean.</summary>
	public IReadOnlyList<string> AreaObjects { get; init; } = Array.Empty<string>();

	/// <summary>
	/// Object types of the layout that the import leaves out on purpose, for example an object that the area makes
	/// itself. The notes do not list them.
	/// </summary>
	public IReadOnlyList<int> SkippedObjects { get; init; } = Array.Empty<int>();

	/// <summary>The changes to the layout that the code of the original makes during play.</summary>
	public IReadOnlyList<LayoutChange> LayoutChanges { get; init; } = Array.Empty<LayoutChange>();

	/// <summary>More level art that the game loads over <see cref="ArtFile"/>, each from its first pattern.</summary>
	public IReadOnlyList<(string File, int Tile)> MoreArt { get; init; } = Array.Empty<(string, int)>();

	/// <summary>
	/// The width of a boss arena after its camera X: the HD screen shows 480 original pixels, 80 more on each side of
	/// the 320 pixel screen of the original. An act that ends at the arena needs this room for the camera to lock.
	/// </summary>
	private const int HdArenaWidth = 320 + 80;

	/// <summary>The zones that the importer knows.</summary>
	public static IReadOnlyDictionary<string, OriginalZone> All { get; } = new Dictionary<string, OriginalZone>(StringComparer.OrdinalIgnoreCase)
	{
		// The HD map of Casino Night has act 2 at chunk (85, 2), after act 1. LevelSize: act 1 $27A0 x $720, act 2
		// $2A80 x $720, plus a 320 x 224 screen.
		["CNZ"] = new OriginalZone("CNZ", "SONICORCA/LEVELS/CNZ", "mappings/128x128/CNZ.kos",
			"collision/CNZ primary 16x16 collision index.kos", "collision/CNZ secondary 16x16 collision index.kos",
			new[]
			{
				new OriginalAct(1, "level/layout/CNZ_1.kos", "level/objects/CNZ_1.bin", "level/rings/CNZ_1.bin", "startpos/CNZ_1.bin",
					new Vector2i(0, 0), 0, new Vector2i(0x27A0 + 320, 0x720 + 224), "level/objects/CNZ 1 bumpers.bin"),
				new OriginalAct(2, "level/layout/CNZ_2.kos", "level/objects/CNZ_2.bin", "level/rings/CNZ_2.bin", "startpos/CNZ_2.bin",
					new Vector2i(85 * 128, 2 * 128), 85 * 8, new Vector2i(0x2A80 + 320, 0x720 + 224), "level/objects/CNZ 2 bumpers.bin"),
			},
			new Vector2i(1400, 144), "art/palettes/CNZ.bin",
			new[]
			{
				new OriginalSprite("SONICORCA/OBJECTS/CNZBUMPER", "art/nemesis/Round bumper from CNZ.nem", "mappings/sprite/obj44.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/CNZHEXBUMPER", "art/nemesis/Hexagonal bumper from CNZ.nem", "mappings/sprite/objD7.asm", 2),
				// Each hit on a bonus block moves it to the next palette line: 2, 1, then 0
				new OriginalSprite("SONICORCA/OBJECTS/CNZBONUSBLOCK", "art/nemesis/Drop target from CNZ.nem", "mappings/sprite/objD8.asm", new[] { 2, 1, 0 }),
				new OriginalSprite("SONICORCA/OBJECTS/CNZFLIPPER", "art/nemesis/Flippers.nem", "mappings/sprite/obj86.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/CNZLAUNCHER", "art/nemesis/Vertical impulse spring.nem", "mappings/sprite/obj85_a.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/CNZLAUNCHER/DIAGONAL", "art/nemesis/Diagonal impulse spring from CNZ.nem", "mappings/sprite/obj85_b.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/CRAWL", "art/nemesis/Bouncer badnik from CNZ.nem", "mappings/sprite/objC8.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/CNZBIGBLOCK", "art/nemesis/Moving block from CNZ and CPZ.nem", "mappings/sprite/objD4.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/CNZELEVATOR", "art/nemesis/CNZ elevator.nem", "mappings/sprite/objD5.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/CNZFLASHINGBLOCK", "art/nemesis/Caterpiller platforms from CNZ.nem", "mappings/sprite/objD2.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/CNZSLOTCAGE", "art/nemesis/CNZ slot machine bars.nem", "mappings/sprite/objD6_b.asm", 0),
				// The mappings start 96 tiles before the boss art, and Eggman's ship is at tile $159. LevEvents_CNZ2 loads the
				// boss palette into line 1.
				new OriginalSprite("SONICORCA/OBJECTS/CNZBOSS", "art/nemesis/CNZ boss.nem", "mappings/sprite/obj51.asm", 0)
				{
					ArtTile = 0x60,
					MoreArt = new[] { ("art/nemesis/Eggpod.nem", 0x159) },
					PaletteOverrides = new Dictionary<int, string> { [1] = "art/palettes/CNZ Boss.bin" }
				},
				new OriginalSprite("SONICORCA/OBJECTS/CNZSLOTCAGE/BOMB", "art/nemesis/Spikey ball from CNZ slots.nem", "mappings/sprite/objD6_a.asm", 0),
			},
			"art/kosinski/CNZ.kos", "mappings/16x16/CNZ.kos", 1023)
		{
			// LevEvents_CNZ2 closes the boss arena with chunk $F9: the right wall at the trigger, the left wall at the lock
			LayoutChanges = new[]
			{
				new LayoutChange("boss_right_wall", 2, new[] { (0x54, 12, 0xF9) }),
				new LayoutChange("boss_left_wall", 2, new[] { (0x50, 12, 0xF9) }),
			}
		},
		// The HD map of Aquatic Ruin has act 2 at block (704, 4), after act 1. LevelSize: act 1 $28C0 x ($200 to
		// $600), act 2 ($180 to $710). The level events of act 2 lock the camera at $2A40 for the boss. The
		// water tops are $410 and $510.
		["ARZ"] = new OriginalZone("ARZ", "SONICORCA/LEVELS/ARZ", "mappings/128x128/ARZ.kos",
			"collision/ARZ primary 16x16 collision index.kos", "collision/ARZ secondary 16x16 collision index.kos",
			new[]
			{
				new OriginalAct(1, "level/layout/ARZ_1.kos", "level/objects/ARZ_1.bin", "level/rings/ARZ_1.bin", "startpos/ARZ_1.bin",
					new Vector2i(0, 0), 0, new Vector2i(0x28C0 + 320, 0x600 + 224), Top: 0x200, WaterHeight: 0x410),
				new OriginalAct(2, "level/layout/ARZ_2.kos", "level/objects/ARZ_2.bin", "level/rings/ARZ_2.bin", "startpos/ARZ_2.bin",
					new Vector2i(704 * 16, 4 * 16), 704, new Vector2i(0x2A40 + HdArenaWidth, 0x710 + 224), Top: 0x180, WaterHeight: 0x510),
			},
			new Vector2i(1448, 132), "art/palettes/ARZ.bin",
			new[]
			{
				new OriginalSprite("SONICORCA/OBJECTS/WHISP", "art/nemesis/Blowfly from ARZ.nem", "mappings/sprite/obj8C.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/GROUNDER", "art/nemesis/Grounder from ARZ.nem", "s2.asm#Obj8D_MapUnc_36CF0", 1),
				// The rocks use palette line 2, and the wall bricks are level art
				new OriginalSprite("SONICORCA/OBJECTS/GROUNDER/ROCK", "art/nemesis/Grounder from ARZ.nem", "s2.asm#Obj90_MapUnc_36CFA", 2),
				new OriginalSprite("SONICORCA/OBJECTS/GROUNDER/BRICK", "art/kosinski/ARZ.kos", "s2.asm#Obj90_MapUnc_36D00", 0),
				new OriginalSprite("SONICORCA/OBJECTS/CHOPCHOP", "art/nemesis/Shark from ARZ.nem", "mappings/sprite/obj91.asm", 1),
				// The platforms and the pillars are level art
				new OriginalSprite("SONICORCA/OBJECTS/ARZPLATFORM", "art/kosinski/ARZ.kos", "mappings/sprite/obj18_b.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/ARZSWINGPLATFORM", "art/kosinski/ARZ.kos", "mappings/sprite/obj83.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/ARZCOLLAPSEPLATFORM", "art/kosinski/ARZ.kos", "mappings/sprite/obj1F_d.asm", 2) { SplitFrames = new[] { 1 } },
				new OriginalSprite("SONICORCA/OBJECTS/ARZFALLINGPILLAR", "art/kosinski/ARZ.kos", "mappings/sprite/obj23.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/ARZRISINGPILLAR", "art/kosinski/ARZ.kos", "mappings/sprite/obj2B.asm", 1) { SplitFrames = new[] { 7, 8, 9, 10, 11, 12, 13 } },
				new OriginalSprite("SONICORCA/OBJECTS/ARZPILLAR", "art/kosinski/ARZ.kos", "mappings/sprite/obj82.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/ARZROTATINGPLATFORM", "art/kosinski/ARZ.kos", "mappings/sprite/obj83.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/ARZARROWSHOOTER", "art/nemesis/Arrow shooter and arrow from ARZ.nem", "mappings/sprite/obj22.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/ARZBUBBLES", "art/nemesis/Bubble generator.nem", "s2.asm#Obj24_MapUnc_1FBF6", 0),
				// Eggman's ship is at tile $120 from the boss art
				new OriginalSprite("SONICORCA/OBJECTS/ARZBOSS", "art/nemesis/ARZ boss.nem", "mappings/sprite/obj89_b.asm", 0)
				{
					MoreArt = new[] { ("art/nemesis/Eggpod.nem", 0x120) }
				},
				new OriginalSprite("SONICORCA/OBJECTS/ARZBOSS/PILLAR", "art/nemesis/ARZ boss.nem", "mappings/sprite/obj89_a.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/ARZLEAVES", "art/nemesis/Leaves in ARZ.nem", "mappings/sprite/obj2C.asm", 3),
			},
			"art/kosinski/ARZ.kos", "mappings/16x16/ARZ.kos", 1023,
			new BackgroundLayer("bg", 96, 0.03125, 0.5, 6144), "fg"),
		// The HD map of Mystic Cave has act 1 at block (0, 4) and act 2 at block (592, 0). LevelSize: act 1 $2380 x
		// ($3C0 to $720), act 2 ($60 to $720). The level events of act 2 lock the camera at $20F0 for the boss.
		["MCZ"] = new OriginalZone("MCZ", "SONICORCA/LEVELS/MCZ", "mappings/128x128/MCZ.kos",
			"collision/MCZ primary 16x16 collision index.kos", null,
			new[]
			{
				new OriginalAct(1, "level/layout/MCZ_1.kos", "level/objects/MCZ_1.bin", "level/rings/MCZ_1.bin", "startpos/MCZ_1.bin",
					new Vector2i(0, 4 * 16), 0, new Vector2i(0x2380 + 320, 0x720 + 224), Top: 0x3C0),
				new OriginalAct(2, "level/layout/MCZ_2.kos", "level/objects/MCZ_2.bin", "level/rings/MCZ_2.bin", "startpos/MCZ_2.bin",
					new Vector2i(592 * 16, 0), 592, new Vector2i(0x20F0 + HdArenaWidth, 0x720 + 224), Top: 0x60),
			},
			new Vector2i(1200, 132), "art/palettes/MCZ.bin",
			new OriginalSprite[]
			{
				// The boss art, with Eggman's ship at VRAM $500 (tile $140 from the boss art at $3C0), and the boss palette
				// that LevEvents_MCZ2 loads into line 1
				new OriginalSprite("SONICORCA/OBJECTS/MCZBOSS", "art/nemesis/MCZ boss.nem", "mappings/sprite/obj57.asm", 0)
				{
					MoreArt = new[] { ("art/nemesis/Eggpod.nem", 0x140) },
					PaletteOverrides = new Dictionary<int, string> { [1] = "art/palettes/MCZ Boss.bin" }
				},
				new OriginalSprite("SONICORCA/OBJECTS/MCZBOSS/ROCK", "art/uncompressed/Falling rocks and stalactites from MCZ.bin", "mappings/sprite/obj57.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/CRAWLTON", "art/nemesis/Snake badnik from MCZ.nem", "mappings/sprite/obj9E.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/FLASHER", "art/nemesis/Firefly from MCZ.nem", "mappings/sprite/objA3.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/MCZSWINGPLATFORM", "art/kosinski/MCZ.kos", "s2.asm#Obj15_Obj7A_MapUnc_10256", 0),
				new OriginalSprite("SONICORCA/OBJECTS/MCZSWINGPLATFORM/TRAP", "art/kosinski/MCZ.kos", "s2.asm#Obj15_MapUnc_102DE", 0),
				new OriginalSprite("SONICORCA/OBJECTS/MCZCOLLAPSEPLATFORM", "art/nemesis/Collapsing platform from MCZ.nem", "mappings/sprite/obj1F_c.asm", 3) { SplitFrames = new[] { 1 } },
				new OriginalSprite("SONICORCA/OBJECTS/MCZSTOMPER", "art/kosinski/MCZ.kos", "mappings/sprite/obj2A.asm", 2),
				// The spike tips are not level art: the object draws them from the art of the long horizontal spikes
				new OriginalSprite("SONICORCA/OBJECTS/MCZSLIDINGSPIKES", "art/kosinski/MCZ.kos", "mappings/sprite/obj76.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/MCZSLIDINGSPIKES/SPIKE", "art/nemesis/Long horizontal spike.nem", "mappings/sprite/obj36.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/MCZSIDESTEPPLATFORM", "art/kosinski/MCZ.kos", "s2.asm#Obj15_Obj7A_MapUnc_10256", 0),
				new OriginalSprite("SONICORCA/OBJECTS/MCZPLATFORM", "art/nemesis/Large wooden box from MCZ.nem", "mappings/sprite/obj6A.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MCZSPIKEBALLCHAIN", "art/kosinski/MCZ.kos", "mappings/sprite/obj75.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/MCZBRIDGE", "art/nemesis/Drawbridge logs from MCZ.nem", "mappings/sprite/obj77.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MCZVINESWITCH", "art/nemesis/Pull switch from MCZ.nem", "mappings/sprite/obj7F.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MCZVINE", "art/nemesis/Vine that lowers from MCZ.nem", "mappings/sprite/obj80_a.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MCZDRAWBRIDGE", "art/nemesis/Drawbridge logs from MCZ.nem", "mappings/sprite/obj81.asm", 3),
			},
			"art/kosinski/MCZ.kos", "mappings/16x16/MCZ.kos", 1023),
		// The HD map of Oil Ocean has act 2 at block (792, 0). LevelSize: act 1 $2F80 x $680, act 2 $2D00 x $680. The
		// level events of act 2 lock the camera at $2880 to $28C0 for the boss.
		["OOZ"] = new OriginalZone("OOZ", "SONICORCA/LEVELS/OOZ", "mappings/128x128/OOZ.kos",
			"collision/OOZ primary 16x16 collision index.kos", null,
			new[]
			{
				new OriginalAct(1, "level/layout/OOZ_1.kos", "level/objects/OOZ_1.bin", "level/rings/OOZ_1.bin", "startpos/OOZ_1.bin",
					new Vector2i(0, 0), 0, new Vector2i(0x2F80 + 320, 0x680 + 224)),
				new OriginalAct(2, "level/layout/OOZ_2.kos", "level/objects/OOZ_2.bin", "level/rings/OOZ_2.bin", "startpos/OOZ_2.bin",
					new Vector2i(792 * 16, 0), 792, new Vector2i(0x28C0 + HdArenaWidth, 0x680 + 224)),
			},
			new Vector2i(1536, 128), "art/palettes/OOZ.bin",
			new OriginalSprite[]
			{
				// The laser and the floor waves use the boss palette that LevEvents_OOZ2 loads into line 1
				new OriginalSprite("SONICORCA/OBJECTS/OOZBOSS", "art/nemesis/OOZ boss.nem", "mappings/sprite/obj55.asm", 0)
				{
					PaletteOverrides = new Dictionary<int, string> { [1] = "art/palettes/OOZ Boss.bin" }
				},
				new OriginalSprite("SONICORCA/OBJECTS/OCTUS", "art/nemesis/Octopus badnik from OOZ.nem", "mappings/sprite/obj4A.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/AQUIS", "art/nemesis/Seahorse from OOZ.nem", "mappings/sprite/obj50.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/OOZBURNER", "art/nemesis/Burner Platform from OOZ.nem", "mappings/sprite/obj33_a.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/OOZBURNER/FLAME", "art/nemesis/Green flame from OOZ burners.nem", "mappings/sprite/obj33_b.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/OOZFAN", "art/nemesis/Fan from OOZ.nem", "mappings/sprite/obj3F_a.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/OOZFAN/VERTICAL", "art/nemesis/Fan from OOZ.nem", "mappings/sprite/obj3F_b.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/OOZLAUNCHBALL", "art/nemesis/Transporter ball from OOZ.nem", "mappings/sprite/obj48.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/OOZLAUNCHERBLOCK", "art/nemesis/Striped blocks from CPZ.nem", "mappings/sprite/obj3D.asm", 3) { SplitFrames = new[] { 1 } },
				new OriginalSprite("SONICORCA/OBJECTS/OOZLAUNCHERBLOCK/HORIZONTAL", "art/nemesis/4 stripy blocks from OOZ.nem", "mappings/sprite/obj3D.asm", 3) { SplitFrames = new[] { 3 } },
				new OriginalSprite("SONICORCA/OBJECTS/OOZPLATFORM", "art/nemesis/Rising platform from OOZ.nem", "mappings/sprite/obj19.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/OOZSCENERY", "art/nemesis/Cascading oil from OOZ.nem", "mappings/sprite/obj1C_c.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/OOZSCENERY/WIDE", "art/nemesis/Cascading oil from OOZ.nem", "mappings/sprite/obj1C_d.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/OOZCOLLAPSEPLATFORM", "art/nemesis/OOZ collapsing platform.nem", "mappings/sprite/obj1F_b.asm", 3) { SplitFrames = new[] { 1 } },
				new OriginalSprite("SONICORCA/OBJECTS/OOZSPIKEBALL", "art/nemesis/Spiked ball from OOZ.nem", "mappings/sprite/obj43.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/OOZSPRING", "art/nemesis/Push spring from OOZ.nem", "mappings/sprite/obj45.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/OOZSWINGPLATFORM", "art/nemesis/Swinging platform from OOZ.nem", "mappings/sprite/obj15_a.asm", 2),
			},
			"art/kosinski/OOZ.kos", "mappings/16x16/OOZ.kos", 1023,
			new BackgroundLayer("bg", 128, 0.5, 0.5, 16 * 128 * 4), "fg")
		{
			AreaObjects = new[] { "SONICORCA/OBJECTS/OOZOIL" }
		},
		// The HD map of Metropolis has act 1 at block (0, 66), act 2 at (576, 6) and act 3 at (1088, 0). LevelSize:
		// act 1 $2280, act 2 $1E80, act 3 $2A80, all from y -$100 to $800. The level events of act 3 lock the camera
		// at $2AB0 for the boss.
		["MTZ"] = new OriginalZone("MTZ", "SONICORCA/LEVELS/MTZ", "mappings/128x128/MTZ.kos",
			"collision/MTZ primary 16x16 collision index.kos", null,
			new[]
			{
				new OriginalAct(1, "level/layout/MTZ_1.kos", "level/objects/MTZ_1.bin", "level/rings/MTZ_1.bin", "startpos/MTZ_1.bin",
					new Vector2i(0, 66 * 16), 0, new Vector2i(0x2280 + 320, 0x800 + 224), Top: -0x100),
				new OriginalAct(2, "level/layout/MTZ_2.kos", "level/objects/MTZ_2.bin", "level/rings/MTZ_2.bin", "startpos/MTZ_2.bin",
					new Vector2i(576 * 16, 6 * 16), 576, new Vector2i(0x1E80 + 320, 0x800 + 224), Top: -0x100),
				new OriginalAct(3, "level/layout/MTZ_3.kos", "level/objects/MTZ_3.bin", "level/rings/MTZ_3.bin", "startpos/MTZ_3.bin",
					new Vector2i(1088 * 16, 0), 1088, new Vector2i(0x2AB0 + HdArenaWidth, 0x800 + 224), Top: -0x100),
			},
			new Vector2i(1872, 194), "art/palettes/MTZ.bin",
			new OriginalSprite[]
			{
				// The boss art at VRAM $37C, Eggman's ship at $500 (tile $184) and the jet at $560 (tile $1E4), as
				// PlrList_MtzBoss loads them. The boss uses the zone palette.
				new OriginalSprite("SONICORCA/OBJECTS/MTZBOSS", "art/nemesis/MTZ boss.nem", "mappings/sprite/obj54.asm", 0)
				{
					MoreArt = new[] { ("art/nemesis/Eggpod.nem", 0x184), ("art/nemesis/Horizontal jet.nem", 0x1E4) }
				},
				new OriginalSprite("SONICORCA/OBJECTS/MTZSTOMPER", "art/kosinski/MTZ.kos", "mappings/sprite/obj64.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/MTZLONGPLATFORM", "art/kosinski/MTZ.kos", "mappings/sprite/obj65_a.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZLONGPLATFORM/COG", "art/nemesis/Small cog from MTZ.nem", "mappings/sprite/obj65_b.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZSPINTUBE", "art/nemesis/Spin tube flash from MTZ.nem", "mappings/sprite/obj67.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZSPIKEBLOCK", "art/nemesis/MTZ spike block.nem", "mappings/sprite/obj68.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZSPIKEBLOCK/SPIKE", "art/nemesis/Spike from MTZ.nem", "mappings/sprite/obj68.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/MTZNUT", "art/nemesis/Similarly shaded blocks from MTZ.nem", "mappings/sprite/obj69.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/MTZPLATFORM", "art/kosinski/MTZ.kos", "mappings/sprite/obj65_a.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZBLOCK", "art/kosinski/MTZ.kos", "mappings/sprite/obj65_a.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZSCENERY", "art/nemesis/Bolt end and rope from MTZ.nem", "mappings/sprite/obj1C_e.asm", new[] { 2, 1 }),
				new OriginalSprite("SONICORCA/OBJECTS/MTZDOOR", "art/kosinski/MTZ.kos", "mappings/sprite/obj2D.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZSTEAMSPRING", "art/kosinski/MTZ.kos", "mappings/sprite/obj42.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZSTEAMSPRING/STEAM", "art/nemesis/Steam from MTZ.nem", "mappings/sprite/obj42.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/MTZBUTTON", "art/nemesis/Button.nem", "mappings/sprite/obj47.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/MTZPULLEYPLATFORM", "art/nemesis/Lava cup from MTZ.nem", "mappings/sprite/obj6C.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZFLOORSPIKE", "art/nemesis/Spike from MTZ.nem", "mappings/sprite/obj68.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/MTZMACHINE", "art/kosinski/MTZ.kos", "mappings/sprite/obj6E.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZMACHINE/INDENT", "art/nemesis/Large spinning wheel from MTZ - indent.nem", "mappings/sprite/obj6E.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZCOG", "art/nemesis/Large spinning wheel from MTZ.nem", "mappings/sprite/obj70.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/MTZLAVABUBBLE", "art/nemesis/Lava bubble from MTZ.nem", "mappings/sprite/obj71_b.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/SHELLCRACKER", "art/nemesis/Shellcracker badnik from MTZ.nem", "mappings/sprite/objA0.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/SLICER", "art/nemesis/Praying mantis badnik from MTZ.nem", "mappings/sprite/objA2.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/ASTERON", "art/nemesis/Exploding star badnik from MTZ.nem", "mappings/sprite/objA4.asm", 0),
			},
			"art/kosinski/MTZ.kos", "mappings/16x16/MTZ.kos", 1023),
		// Sky Chase, Wing Fortress and Death Egg have one act each, at the start of the HD map. LevelSize: SCZ y 0 (the
		// camera does not move up or down), WFZ $720, DEZ y $C8.
		["SCZ"] = new OriginalZone("SCZ", "SONICORCA/LEVELS/SCZ", "mappings/128x128/WFZ_SCZ.kos",
			"collision/WFZ and SCZ primary 16x16 collision index.kos", "collision/WFZ and SCZ secondary 16x16 collision index.kos",
			new[]
			{
				new OriginalAct(1, "level/layout/SCZ.kos", "level/objects/SCZ_1.bin", "level/rings/SCZ_1.bin", "startpos/SCZ_1.bin",
					new Vector2i(0, 0), 0, new Vector2i(344 * 16, 224)),
			},
			new Vector2i(344, 96), "art/palettes/SCZ.bin",
			new OriginalSprite[]
			{
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO", "art/nemesis/The Tornado.nem", "mappings/sprite/objB2_a.asm", 0),
				// The pilot: the first 6 tiles of a Tails frame (Tails' DPLC), at tile $2A0 of the mappings. Without the
				// Tornado art, frame 0 of each sprite shows only the pilot.
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT0", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0x96 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT1", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT2", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0xC },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT3", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0x12 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT4", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0x18 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/THRUSTER", "art/nemesis/Rocket thruster for Tornado.nem", "mappings/sprite/objB2_b.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/PROPELLER", "art/nemesis/Vertical spinning blades in WFZ.nem", "mappings/sprite/objB4.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/PROPELLER/HORIZONTAL", "art/nemesis/Horizontal spinning blades in WFZ.nem", "mappings/sprite/objB5.asm", 1),
				// The bomb blinks between palette lines 1 and 0
				new OriginalSprite("SONICORCA/OBJECTS/NEBULA", "art/nemesis/Bomber badnik from SCZ.nem", "mappings/sprite/obj99.asm", new[] { 1, 0 }),
				new OriginalSprite("SONICORCA/OBJECTS/TURTLOID", "art/nemesis/Turtle badnik from SCZ.nem", "mappings/sprite/obj9C.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/BALKIRY", "art/nemesis/Balkrie (jet badnik) from SCZ.nem", "mappings/sprite/objAC.asm", 0),
				// The jet flame of Balkiry is in the art of Turtloid
				new OriginalSprite("SONICORCA/OBJECTS/BALKIRY/JET", "art/nemesis/Turtle badnik from SCZ.nem", "mappings/sprite/obj9C.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/SCZCLOUD", "art/nemesis/Clouds.nem", "mappings/sprite/objB3.asm", 2),
			},
			"art/kosinski/WFZ_SCZ.kos", "mappings/16x16/WFZ_SCZ.kos", 1023),
		["WFZ"] = new OriginalZone("WFZ", "SONICORCA/LEVELS/WFZ", "mappings/128x128/WFZ_SCZ.kos",
			"collision/WFZ and SCZ primary 16x16 collision index.kos", "collision/WFZ and SCZ secondary 16x16 collision index.kos",
			new[]
			{
				new OriginalAct(1, "level/layout/WFZ.kos", "level/objects/WFZ_1.bin", "level/rings/WFZ_1.bin", "startpos/WFZ_1.bin",
					new Vector2i(0, 0), 0, new Vector2i(1024 * 16, 0x720 + 224)),
			},
			new Vector2i(1024, 128), "art/palettes/WFZ.bin",
			new OriginalSprite[]
			{
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO", "art/nemesis/The Tornado.nem", "mappings/sprite/objB2_a.asm", 0),
				// The pilot: the first 6 tiles of a Tails frame (Tails' DPLC), at tile $2A0 of the mappings. Without the
				// Tornado art, frame 0 of each sprite shows only the pilot.
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT0", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0x96 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT1", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT2", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0xC },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT3", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0x12 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/PILOT4", "art/uncompressed/Tails's art.bin", "mappings/sprite/objB2_a.asm", 0) { ArtTile = 0x2A0 - 0x18 },
				new OriginalSprite("SONICORCA/OBJECTS/TORNADO/THRUSTER", "art/nemesis/Rocket thruster for Tornado.nem", "mappings/sprite/objB2_b.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/PROPELLER", "art/nemesis/Vertical spinning blades in WFZ.nem", "mappings/sprite/objB4.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/PROPELLER/HORIZONTAL", "art/nemesis/Horizontal spinning blades in WFZ.nem", "mappings/sprite/objB5.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/WFZPLATFORM", "art/nemesis/Moving platform from WFZ.nem", "mappings/sprite/obj19.asm", 1),
				// Obj2D has no WFZ art: its art tile ($426) is 12 tiles into the art of the retracting platform ($41A)
				new OriginalSprite("SONICORCA/OBJECTS/WFZBARRIER", "art/nemesis/Retracting platform from WFZ.nem", "mappings/sprite/obj2D.asm", 1) { ArtTile = -0xC },
				// ArtTile_ArtNem_WfzHook_Fudge: the mappings start 4 tiles into the art
				new OriginalSprite("SONICORCA/OBJECTS/WFZHOOK", "art/nemesis/Hook on chain from WFZ.nem", "mappings/sprite/obj80_b.asm", 1) { ArtTile = -4 },
				new OriginalSprite("SONICORCA/OBJECTS/WFZLAUNCHER", "art/nemesis/Catapult that shoots Sonic to the side from WFZ.nem", "mappings/sprite/objC0.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/WFZPLATING", "art/nemesis/Breakaway panels from WFZ.nem", "mappings/sprite/objC1.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/WFZRIVET", "art/nemesis/WFZ boss chamber switch.nem", "mappings/sprite/objC2.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/WFZBOSS", "art/nemesis/WFZ boss.nem", "mappings/sprite/objC5_a.asm", 0),
				// The mappings of Robotnik use the VRAM tiles of PlrList_WfzBoss
				new OriginalSprite("SONICORCA/OBJECTS/WFZBOSS/ROBOTNIK", "art/nemesis/Robotnik's head.nem", "mappings/sprite/objC6_a.asm", 0)
				{
					ArtTile = 0x500,
					MoreArt = new[] { ("art/nemesis/Robotnik.nem", 0x518), ("art/nemesis/Robotnik's lower half.nem", 0x564) }
				},
				new OriginalSprite("SONICORCA/OBJECTS/WFZBOSS/STAND", "art/nemesis/Moving platform from WFZ.nem", "mappings/sprite/objC5_b.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/CLUCKER", "art/nemesis/Scratch from WFZ.nem", "mappings/sprite/objAE.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/WFZTILTPLATFORM", "art/nemesis/Tilting plaforms in WFZ.nem", "mappings/sprite/objB6.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/WFZTURRET", "art/nemesis/Wall turret from WFZ.nem", "mappings/sprite/objB8.asm", 0),
				new OriginalSprite("SONICORCA/OBJECTS/WFZLASER", "art/nemesis/Red horizontal laser from WFZ.nem", "mappings/sprite/objB9.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/WFZWHEEL", "art/nemesis/Wheel for belt in WFZ.nem", "mappings/sprite/objBA.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/WFZTHRUST", "art/nemesis/Thrust from Robotnik's getaway ship in WFZ.nem", "mappings/sprite/objBC.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/WFZBELTPLATFORM", "art/nemesis/Platform on belt in WFZ.nem", "mappings/sprite/objBD.asm", 3),
				new OriginalSprite("SONICORCA/OBJECTS/WFZGUNPLATFORM", "art/nemesis/Retracting platform from WFZ.nem", "mappings/sprite/objBE.asm", 3),
			},
			"art/kosinski/WFZ_SCZ.kos", "mappings/16x16/WFZ_SCZ.kos", 1023)
		{
			// The patterns of WFZ that are not in the SCZ art (ArtKos_WFZ)
			MoreArt = new[] { ("art/kosinski/WFZ_Supp.kos", 0x307) },
			AreaObjects = new[] { "SONICORCA/OBJECTS/CLUCKER/SHOT", "SONICORCA/OBJECTS/WFZBOSS/LASERWALL", "SONICORCA/OBJECTS/WFZBOSS/PLATFORM" },
			// ObjC2_Bust: the rivet opens the deck into the boss room
			LayoutChanges = new[]
			{
				new LayoutChange("rivet", 1, new[]
				{
					(0x50, 8, 0x8A), (0x51, 8, 0x70), (0x52, 8, 0x71), (0x53, 8, 0x72), (0x54, 8, 0x73), (0x55, 8, 0x74),
					(0x50, 9, 0x6E), (0x51, 9, 0x78), (0x52, 9, 0x79), (0x53, 9, 0x78), (0x54, 9, 0x78), (0x55, 9, 0x7A),
				}),
			},
			// $8B only changes the palette cycle of the fire. The area makes the boss ($C5) when the player reaches it.
			SkippedObjects = new[] { 0x8B, 0xC5 }
		},
		["DEZ"] = new OriginalZone("DEZ", "SONICORCA/LEVELS/DEZ", "mappings/128x128/CPZ_DEZ.kos",
			"collision/CPZ and DEZ primary 16x16 collision index.kos", "collision/CPZ and DEZ secondary 16x16 collision index.kos",
			new[]
			{
				new OriginalAct(1, "level/layout/DEZ.kos", "level/objects/DEZ_1.bin", "level/rings/DEZ_1.bin", "startpos/DEZ_1.bin",
					new Vector2i(0, 0), 0, new Vector2i(0x1000 + 320, 0xC8 + 224), Top: 0xC8),
			},
			new Vector2i(288, 48), "art/palettes/DEZ.bin",
			new OriginalSprite[]
			{
				new OriginalSprite("SONICORCA/OBJECTS/DEZDOOR", "art/nemesis/Stripy blocks from CPZ.nem", "mappings/sprite/obj2D.asm", 1),
				// Eggman uses the art of his head, his body and his legs at VRAM $500, $518 and $564
				new OriginalSprite("SONICORCA/OBJECTS/DEZEGGMAN", "art/nemesis/Robotnik's head.nem", "mappings/sprite/objC6_a.asm", 0)
				{
					ArtTile = 0x500,
					MoreArt = new[] { ("art/nemesis/Robotnik.nem", 0x518), ("art/nemesis/Robotnik's lower half.nem", 0x564) }
				},
				new OriginalSprite("SONICORCA/OBJECTS/DEZEGGMAN/DOOR", "art/nemesis/Stripy blocks from CPZ.nem", "mappings/sprite/objC6_b.asm", 1),
				new OriginalSprite("SONICORCA/OBJECTS/SILVERSONIC", "art/nemesis/Silver Sonic.nem", "mappings/sprite/objAF_a.asm", 1),
				// The window art is at VRAM $378, and Eggman's head at $500 (tile $188 from the window art)
				new OriginalSprite("SONICORCA/OBJECTS/SILVERSONIC/WINDOW", "art/nemesis/Window in back that Robotnik looks through in DEZ.nem", "mappings/sprite/objAF_b.asm", 0)
				{
					MoreArt = new[] { ("art/nemesis/Robotnik's head.nem", 0x188) }
				},
				new OriginalSprite("SONICORCA/OBJECTS/DEATHEGGROBOT", "art/nemesis/Eggrobo.nem", "mappings/sprite/objC7.asm", 0),
				// The lock mark of the sensor flashes between palette lines 0 and 1
				new OriginalSprite("SONICORCA/OBJECTS/DEATHEGGROBOT/SENSOR", "art/nemesis/Eggrobo.nem", "mappings/sprite/objC7.asm", new[] { 0, 1 }),
			},
			"art/kosinski/CPZ_DEZ.kos", "mappings/16x16/CPZ_DEZ.kos", 1023),
	};
}

/// <summary>The result of a level import: the map, the binding, and notes about what was not imported.</summary>
/// <param name="Sprites">The art of the objects that the game data does not have. An object can have more than one, under its key.</param>
/// <param name="NewObjects">The objects that the binding uses and that the game data does not have: the resource key and the namespace of the code.</param>
/// <param name="TileSet">The tile set with the tiles in front of the characters, or null.</param>
/// <param name="PriorityTexture">The texture of the tiles in front of the characters, or null.</param>
/// <param name="Textures">Other textures, by resource key, for example the slot machine reels.</param>
public sealed record ImportedLevel(LevelMap Map, LevelBinding Binding, IReadOnlyList<string> Notes,
	IReadOnlyList<ImportedSprite> Sprites, IReadOnlyDictionary<string, string> NewObjects,
	string TileSet = null, byte[] PriorityTexture = null, IReadOnlyDictionary<string, byte[]> Textures = null);

/// <summary>
/// Makes the map and the binding of a SonicOrca level from a zone of the original game. The tiles are the blocks of
/// the original (the HD tile sets use the block numbers). The collision is traced from the height maps of the
/// original, separately for each of the two paths. The objects that SonicOrca has are converted, with the rings, the
/// start positions and the act bounds.
/// </summary>
public static class LevelImport
{
	/// <summary>SonicOrca levels have four times the resolution of the original.</summary>
	public const int Scale = 4;

	private const int SignpostType = 0x0D;

	private const int BlockSize = 16;

	private const int ChunkSize = 128;

	private const int BlocksPerChunk = ChunkSize / BlockSize;

	private const int LayoutColumns = 128;

	private const int LayoutRows = 16;

	private const int FlipXFlag = 0x4000;

	private const int FlipYFlag = 0x8000;

	private const int BackLayerIndex = 0;

	private const int FrontLayerIndex = 1;

	private const string RingResourceKey = "SONICORCA/OBJECTS/RING";

	/// <summary>
	/// The objects of this repository that the game data does not have. A level that uses them needs a resource
	/// that names the code: "#use" and the namespace.
	/// </summary>
	public static IReadOnlyDictionary<string, string> RepositoryObjects { get; } = new Dictionary<string, string>
	{
		["SONICORCA/OBJECTS/CNZBUMPER"] = "SONICORCA.OBJECTS.CNZBUMPER",
		["SONICORCA/OBJECTS/INVISIBLEBLOCK"] = "SONICORCA.OBJECTS.INVISIBLEBLOCK",
		["SONICORCA/OBJECTS/CNZHEXBUMPER"] = "SONICORCA.OBJECTS.CNZHEXBUMPER",
		["SONICORCA/OBJECTS/CNZBONUSBLOCK"] = "SONICORCA.OBJECTS.CNZBONUSBLOCK",
		["SONICORCA/OBJECTS/CNZFLIPPER"] = "SONICORCA.OBJECTS.CNZFLIPPER",
		["SONICORCA/OBJECTS/CNZLAUNCHER"] = "SONICORCA.OBJECTS.CNZLAUNCHER",
		["SONICORCA/OBJECTS/CNZCONVEYOR"] = "SONICORCA.OBJECTS.CNZCONVEYOR",
		["SONICORCA/OBJECTS/CRAWL"] = "SONICORCA.OBJECTS.CRAWL",
		["SONICORCA/OBJECTS/CNZBIGBLOCK"] = "SONICORCA.OBJECTS.CNZBIGBLOCK",
		["SONICORCA/OBJECTS/CNZELEVATOR"] = "SONICORCA.OBJECTS.CNZELEVATOR",
		["SONICORCA/OBJECTS/CNZFLASHINGBLOCK"] = "SONICORCA.OBJECTS.CNZFLASHINGBLOCK",
		["SONICORCA/OBJECTS/CNZMAPBUMPER"] = "SONICORCA.OBJECTS.CNZMAPBUMPER",
		["SONICORCA/OBJECTS/CNZSLOTCAGE"] = "SONICORCA.OBJECTS.CNZSLOTCAGE",
		["SONICORCA/OBJECTS/CNZSLOTREELS"] = "SONICORCA.OBJECTS.CNZSLOTREELS",
		["SONICORCA/OBJECTS/WHISP"] = "SONICORCA.OBJECTS.WHISP",
		["SONICORCA/OBJECTS/GROUNDER"] = "SONICORCA.OBJECTS.GROUNDER",
		["SONICORCA/OBJECTS/CHOPCHOP"] = "SONICORCA.OBJECTS.CHOPCHOP",
		["SONICORCA/OBJECTS/ARZPLATFORM"] = "SONICORCA.OBJECTS.ARZPLATFORM",
		["SONICORCA/OBJECTS/ARZSWINGPLATFORM"] = "SONICORCA.OBJECTS.ARZSWINGPLATFORM",
		["SONICORCA/OBJECTS/ARZCOLLAPSEPLATFORM"] = "SONICORCA.OBJECTS.ARZCOLLAPSEPLATFORM",
		["SONICORCA/OBJECTS/ARZFALLINGPILLAR"] = "SONICORCA.OBJECTS.ARZFALLINGPILLAR",
		["SONICORCA/OBJECTS/ARZRISINGPILLAR"] = "SONICORCA.OBJECTS.ARZRISINGPILLAR",
		["SONICORCA/OBJECTS/ARZPILLAR"] = "SONICORCA.OBJECTS.ARZPILLAR",
		["SONICORCA/OBJECTS/ARZROTATINGPLATFORM"] = "SONICORCA.OBJECTS.ARZROTATINGPLATFORM",
		["SONICORCA/OBJECTS/ARZARROWSHOOTER"] = "SONICORCA.OBJECTS.ARZARROWSHOOTER",
		["SONICORCA/OBJECTS/ARZBUBBLES"] = "SONICORCA.OBJECTS.ARZBUBBLES",
		["SONICORCA/OBJECTS/ARZLEAVES"] = "SONICORCA.OBJECTS.ARZLEAVES",
		["SONICORCA/OBJECTS/ARZBOSS"] = "SONICORCA.OBJECTS.ARZBOSS",
		["SONICORCA/OBJECTS/CNZBOSS"] = "SONICORCA.OBJECTS.CNZBOSS",
		["SONICORCA/OBJECTS/MCZBOSS"] = "SONICORCA.OBJECTS.MCZBOSS",
		["SONICORCA/OBJECTS/MCZBOSS/ROCK"] = "SONICORCA.OBJECTS.MCZBOSS.ROCK",
		["SONICORCA/OBJECTS/CRAWLTON"] = "SONICORCA.OBJECTS.CRAWLTON",
		["SONICORCA/OBJECTS/MTZBOSS"] = "SONICORCA.OBJECTS.MTZBOSS",
		["SONICORCA/OBJECTS/MTZCYLINDER"] = "SONICORCA.OBJECTS.MTZCYLINDER",
		["SONICORCA/OBJECTS/MTZSTOMPER"] = "SONICORCA.OBJECTS.MTZSTOMPER",
		["SONICORCA/OBJECTS/MTZLONGPLATFORM"] = "SONICORCA.OBJECTS.MTZLONGPLATFORM",
		["SONICORCA/OBJECTS/MTZSPRINGWALL"] = "SONICORCA.OBJECTS.MTZSPRINGWALL",
		["SONICORCA/OBJECTS/MTZSPINTUBE"] = "SONICORCA.OBJECTS.MTZSPINTUBE",
		["SONICORCA/OBJECTS/MTZSPIKEBLOCK"] = "SONICORCA.OBJECTS.MTZSPIKEBLOCK",
		["SONICORCA/OBJECTS/MTZNUT"] = "SONICORCA.OBJECTS.MTZNUT",
		["SONICORCA/OBJECTS/MTZPLATFORM"] = "SONICORCA.OBJECTS.MTZPLATFORM",
		["SONICORCA/OBJECTS/MTZBLOCK"] = "SONICORCA.OBJECTS.MTZBLOCK",
		["SONICORCA/OBJECTS/MTZSCENERY"] = "SONICORCA.OBJECTS.MTZSCENERY",
		["SONICORCA/OBJECTS/MTZDOOR"] = "SONICORCA.OBJECTS.MTZDOOR",
		["SONICORCA/OBJECTS/MTZLAVA"] = "SONICORCA.OBJECTS.MTZLAVA",
		["SONICORCA/OBJECTS/MTZSTEAMSPRING"] = "SONICORCA.OBJECTS.MTZSTEAMSPRING",
		["SONICORCA/OBJECTS/MTZBUTTON"] = "SONICORCA.OBJECTS.MTZBUTTON",
		["SONICORCA/OBJECTS/MTZPULLEYPLATFORM"] = "SONICORCA.OBJECTS.MTZPULLEYPLATFORM",
		["SONICORCA/OBJECTS/MTZFLOORSPIKE"] = "SONICORCA.OBJECTS.MTZFLOORSPIKE",
		["SONICORCA/OBJECTS/MTZMACHINE"] = "SONICORCA.OBJECTS.MTZMACHINE",
		["SONICORCA/OBJECTS/MTZCOG"] = "SONICORCA.OBJECTS.MTZCOG",
		["SONICORCA/OBJECTS/MTZLAVABUBBLE"] = "SONICORCA.OBJECTS.MTZLAVABUBBLE",
		["SONICORCA/OBJECTS/SHELLCRACKER"] = "SONICORCA.OBJECTS.SHELLCRACKER",
		["SONICORCA/OBJECTS/SLICER"] = "SONICORCA.OBJECTS.SLICER",
		["SONICORCA/OBJECTS/ASTERON"] = "SONICORCA.OBJECTS.ASTERON",
		["SONICORCA/OBJECTS/CLUCKER"] = "SONICORCA.OBJECTS.CLUCKER",
		["SONICORCA/OBJECTS/DEZDOOR"] = "SONICORCA.OBJECTS.DEZDOOR",
		["SONICORCA/OBJECTS/DEZEGGMAN"] = "SONICORCA.OBJECTS.DEZEGGMAN",
		["SONICORCA/OBJECTS/DEZEGGMAN/DOOR"] = "SONICORCA.OBJECTS.DEZEGGMAN.DOOR",
		["SONICORCA/OBJECTS/SILVERSONIC"] = "SONICORCA.OBJECTS.SILVERSONIC",
		["SONICORCA/OBJECTS/SILVERSONIC/WINDOW"] = "SONICORCA.OBJECTS.SILVERSONIC.WINDOW",
		["SONICORCA/OBJECTS/DEATHEGGROBOT"] = "SONICORCA.OBJECTS.DEATHEGGROBOT",
		["SONICORCA/OBJECTS/DEATHEGGROBOT/SENSOR"] = "SONICORCA.OBJECTS.DEATHEGGROBOT.SENSOR",
		["SONICORCA/OBJECTS/TORNADO"] = "SONICORCA.OBJECTS.TORNADO",
		["SONICORCA/OBJECTS/NEBULA"] = "SONICORCA.OBJECTS.NEBULA",
		["SONICORCA/OBJECTS/TURTLOID"] = "SONICORCA.OBJECTS.TURTLOID",
		["SONICORCA/OBJECTS/BALKIRY"] = "SONICORCA.OBJECTS.BALKIRY",
		["SONICORCA/OBJECTS/SCZCLOUD"] = "SONICORCA.OBJECTS.SCZCLOUD",
		["SONICORCA/OBJECTS/PROPELLER"] = "SONICORCA.OBJECTS.PROPELLER",
		["SONICORCA/OBJECTS/WFZPLATFORM"] = "SONICORCA.OBJECTS.WFZPLATFORM",
		["SONICORCA/OBJECTS/WFZBARRIER"] = "SONICORCA.OBJECTS.WFZBARRIER",
		["SONICORCA/OBJECTS/WFZHOOK"] = "SONICORCA.OBJECTS.WFZHOOK",
		["SONICORCA/OBJECTS/WFZLAUNCHER"] = "SONICORCA.OBJECTS.WFZLAUNCHER",
		["SONICORCA/OBJECTS/WFZPLATING"] = "SONICORCA.OBJECTS.WFZPLATING",
		["SONICORCA/OBJECTS/WFZRIVET"] = "SONICORCA.OBJECTS.WFZRIVET",
		["SONICORCA/OBJECTS/WFZGRAB"] = "SONICORCA.OBJECTS.WFZGRAB",
		["SONICORCA/OBJECTS/WFZBOSS"] = "SONICORCA.OBJECTS.WFZBOSS",
		["SONICORCA/OBJECTS/WFZBOSS/LASERWALL"] = "SONICORCA.OBJECTS.WFZBOSS.LASERWALL",
		["SONICORCA/OBJECTS/WFZBOSS/PLATFORM"] = "SONICORCA.OBJECTS.WFZBOSS.PLATFORM",
		["SONICORCA/OBJECTS/CLUCKER/BASE"] = "SONICORCA.OBJECTS.CLUCKER.BASE",
		["SONICORCA/OBJECTS/CLUCKER/SHOT"] = "SONICORCA.OBJECTS.CLUCKER.SHOT",
		["SONICORCA/OBJECTS/WFZTILTPLATFORM"] = "SONICORCA.OBJECTS.WFZTILTPLATFORM",
		["SONICORCA/OBJECTS/WFZTURRET"] = "SONICORCA.OBJECTS.WFZTURRET",
		["SONICORCA/OBJECTS/WFZLASER"] = "SONICORCA.OBJECTS.WFZLASER",
		["SONICORCA/OBJECTS/WFZWHEEL"] = "SONICORCA.OBJECTS.WFZWHEEL",
		["SONICORCA/OBJECTS/WFZTHRUST"] = "SONICORCA.OBJECTS.WFZTHRUST",
		["SONICORCA/OBJECTS/WFZBELTPLATFORM"] = "SONICORCA.OBJECTS.WFZBELTPLATFORM",
		["SONICORCA/OBJECTS/WFZGUNPLATFORM"] = "SONICORCA.OBJECTS.WFZGUNPLATFORM",
		["SONICORCA/OBJECTS/FLASHER"] = "SONICORCA.OBJECTS.FLASHER",
		["SONICORCA/OBJECTS/MCZSWINGPLATFORM"] = "SONICORCA.OBJECTS.MCZSWINGPLATFORM",
		["SONICORCA/OBJECTS/MCZCOLLAPSEPLATFORM"] = "SONICORCA.OBJECTS.MCZCOLLAPSEPLATFORM",
		["SONICORCA/OBJECTS/MCZSTOMPER"] = "SONICORCA.OBJECTS.MCZSTOMPER",
		["SONICORCA/OBJECTS/MCZSLIDINGSPIKES"] = "SONICORCA.OBJECTS.MCZSLIDINGSPIKES",
		["SONICORCA/OBJECTS/MCZSIDESTEPPLATFORM"] = "SONICORCA.OBJECTS.MCZSIDESTEPPLATFORM",
		["SONICORCA/OBJECTS/MCZPLATFORM"] = "SONICORCA.OBJECTS.MCZPLATFORM",
		["SONICORCA/OBJECTS/MCZSPIKEBALLCHAIN"] = "SONICORCA.OBJECTS.MCZSPIKEBALLCHAIN",
		["SONICORCA/OBJECTS/MCZBRIDGE"] = "SONICORCA.OBJECTS.MCZBRIDGE",
		["SONICORCA/OBJECTS/MCZVINESWITCH"] = "SONICORCA.OBJECTS.MCZVINESWITCH",
		["SONICORCA/OBJECTS/MCZVINE"] = "SONICORCA.OBJECTS.MCZVINE",
		["SONICORCA/OBJECTS/MCZDRAWBRIDGE"] = "SONICORCA.OBJECTS.MCZDRAWBRIDGE",
		["SONICORCA/OBJECTS/OOZBOSS"] = "SONICORCA.OBJECTS.OOZBOSS",
		["SONICORCA/OBJECTS/OCTUS"] = "SONICORCA.OBJECTS.OCTUS",
		["SONICORCA/OBJECTS/AQUIS"] = "SONICORCA.OBJECTS.AQUIS",
		["SONICORCA/OBJECTS/OOZBURNER"] = "SONICORCA.OBJECTS.OOZBURNER",
		["SONICORCA/OBJECTS/OOZFAN"] = "SONICORCA.OBJECTS.OOZFAN",
		["SONICORCA/OBJECTS/OOZLAUNCHBALL"] = "SONICORCA.OBJECTS.OOZLAUNCHBALL",
		["SONICORCA/OBJECTS/OOZLAUNCHERBLOCK"] = "SONICORCA.OBJECTS.OOZLAUNCHERBLOCK",
		["SONICORCA/OBJECTS/OOZOIL"] = "SONICORCA.OBJECTS.OOZOIL",
		["SONICORCA/OBJECTS/OOZPLATFORM"] = "SONICORCA.OBJECTS.OOZPLATFORM",
		["SONICORCA/OBJECTS/OOZSCENERY"] = "SONICORCA.OBJECTS.OOZSCENERY",
		["SONICORCA/OBJECTS/OOZCOLLAPSEPLATFORM"] = "SONICORCA.OBJECTS.OOZCOLLAPSEPLATFORM",
		["SONICORCA/OBJECTS/OOZSPIKEBALL"] = "SONICORCA.OBJECTS.OOZSPIKEBALL",
		["SONICORCA/OBJECTS/OOZSPRING"] = "SONICORCA.OBJECTS.OOZSPRING",
		["SONICORCA/OBJECTS/OOZSWINGPLATFORM"] = "SONICORCA.OBJECTS.OOZSWINGPLATFORM",
	};

	public static ImportedLevel Import(OriginalZone zone, string disassembly)
	{
		string Path(string file) => System.IO.Path.Combine(disassembly, file);
		byte[] chunks = Kosinski.Decompress(File.ReadAllBytes(Path(zone.ChunksFile)));
		byte[] primary = Kosinski.Decompress(File.ReadAllBytes(Path(zone.PrimaryCollisionFile)));
		byte[] secondary = zone.SecondaryCollisionFile != null ? Kosinski.Decompress(File.ReadAllBytes(Path(zone.SecondaryCollisionFile))) : primary;
		byte[] heights = File.ReadAllBytes(Path("collision/Collision array - Vertical.bin"));
		byte[][] layouts = zone.Acts.Select(act => Kosinski.Decompress(File.ReadAllBytes(Path(act.LayoutFile)))).ToArray();

		int columns = zone.MapSize.X;
		int rows = zone.MapSize.Y;
		// The chunk entry of each block of the front layer: the block number, the flips and the solidity of both paths
		var blocks = new ushort[columns, rows];
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				int act = zone.Acts.Count - 1;
				while (act > 0 && column < zone.Acts[act].FirstColumn)
				{
					act--;
				}
				int x = column - zone.Acts[act].Offset.X / BlockSize;
				int y = row - zone.Acts[act].Offset.Y / BlockSize;
				blocks[column, row] = GetBlock(chunks, layouts[act], x, y, background: false);
			}
		}

		var map = new LevelMap();
		BackgroundLayer background = zone.Background ?? new BackgroundLayer("back", LayoutRows * BlocksPerChunk, 0.5, 0.5, LayoutRows * ChunkSize * Scale);
		var back = new LevelLayer(map) { Name = background.Name, WrapX = true, ParallaxY = background.ParallaxY };
		back.LayerRowDefinitions.Add(new LayerRowDefinition { Height = background.RowHeight, Parallax = background.ParallaxX });
		back.Resize(LayoutColumns * BlocksPerChunk, background.Rows);
		for (int row = 0; row < back.Rows; row++)
		{
			for (int column = 0; column < back.Columns; column++)
			{
				back.Tiles[column, row] = ToTile(GetBlock(chunks, layouts[0], column, row, background: true));
			}
		}
		var front = new LevelLayer(map) { Name = zone.FrontName };
		front.Resize(columns, rows);
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				front.Tiles[column, row] = ToTile(blocks[column, row]);
			}
		}
		var layers = new List<LevelLayer> { back, front };
		string tileSet = null;
		byte[] priorityTexture = null;
		uint[] palette = SpriteImport.ReadPalette(disassembly, zone.PaletteFile);
		byte[] art = null;
		byte[] blockMappings = null;
		HashSet<int> priorityBlocks = null;
		HashSet<int> animatedBlocks = null;
		IReadOnlyList<AnimatedArt> animations = null;
		var used = new HashSet<int>();
		var usedAnimated = new HashSet<int>();
		LevelLayer high = null;
		if (zone.ArtFile != null)
		{
			// The high-priority patterns go on a layer after the front layer, so they cover the characters
			art = Kosinski.Decompress(File.ReadAllBytes(Path(zone.ArtFile)));
			foreach ((string file, int tile) in zone.MoreArt)
			{
				byte[] more = Kosinski.Decompress(File.ReadAllBytes(Path(file)));
				if (art.Length < tile * 32 + more.Length)
				{
					Array.Resize(ref art, tile * 32 + more.Length);
				}
				more.CopyTo(art, tile * 32);
			}
			blockMappings = AnimatedArt.AddAnimatedBlocks(disassembly, zone.Name, Kosinski.Decompress(File.ReadAllBytes(Path(zone.BlocksFile))));
			priorityBlocks = PriorityTiles.GetPriorityBlocks(blockMappings);
			animations = AnimatedArt.Read(disassembly, zone.Name);
			animatedBlocks = PriorityTiles.GetAnimatedBlocks(blockMappings, animations);
			high = new LevelLayer(map) { Name = "front_high" };
			high.Resize(columns, rows);
			for (int row = 0; row < rows; row++)
			{
				for (int column = 0; column < columns; column++)
				{
					front.Tiles[column, row] = GetFrontTile(blocks[column, row]);
					high.Tiles[column, row] = GetHighTile(blocks[column, row]);
				}
			}
			layers.Add(high);
		}

		// The front tile of a block: the HD tile, or the animated tile of a block with animated art
		int GetFrontTile(ushort entry)
		{
			int block = entry & 0x3FF;
			if (block != 0 && animatedBlocks != null && animatedBlocks.Contains(block))
			{
				usedAnimated.Add(block);
				if (PriorityTiles.HasPatterns(blockMappings, block, priority: false))
				{
					return PriorityTiles.AnimatedFirstTile + ToTile(entry);
				}
			}
			return ToTile(entry);
		}

		// The tile of the priority layer: the high-priority patterns of the block, or nothing
		int GetHighTile(ushort entry)
		{
			int block = entry & 0x3FF;
			if (block == 0 || priorityBlocks == null || !priorityBlocks.Contains(block))
			{
				return 0;
			}
			used.Add(block);
			return PriorityTiles.FirstTile + ToTile(entry);
		}

		// Path 0 is the primary collision and path 1 the secondary collision, as the layer switches expect
		map.CollisionPathLayers.Add(FrontLayerIndex);
		map.CollisionPathLayers.Add(FrontLayerIndex);
		foreach ((Vector2i a, Vector2i b, uint paths) in GetCollisionLines(blocks, primary, secondary, heights))
		{
			map.CollisionVectors.Add(new CollisionVector(a, b, paths));
		}
		AddLayoutChanges(zone, map, layers, chunks, layouts, blocks, primary, secondary, heights, GetFrontTile, GetHighTile);

		if (zone.ArtFile != null)
		{
			(tileSet, priorityTexture) = PriorityTiles.Create(zone.HdTiles, art, blockMappings, palette, used, usedAnimated, animations, Scale);
		}
		foreach (LevelLayer layer in layers)
		{
			map.LayerTree.Children.Add(layer);
			map.Layers.Add(layer);
		}

		var binding = new LevelBinding();
		var notes = new List<string>();
		var unsupported = new SortedDictionary<int, int>();
		foreach (OriginalAct act in zone.Acts)
		{
			string n = act.Number.ToString();
			byte[] start = File.ReadAllBytes(Path(act.StartFile));
			var startPosition = new Vector2i((start[0] << 8) | start[1], (start[2] << 8) | start[3]);
			AddMarker(map, front, "startpos_stk_" + n, (act.Offset + startPosition) * Scale);
			// The origin of the original act, for the level events and the bosses
			AddMarker(map, front, "origin_" + n, act.Offset * Scale);
			AddMarker(map, front, $"bounds_{n}_lt", (act.Offset + new Vector2i(0, act.Top)) * Scale);
			if (act.WaterHeight >= 0)
			{
				AddMarker(map, front, "water_" + n, (act.Offset + new Vector2i(0, act.WaterHeight)) * Scale);
			}
			AddMarker(map, front, $"bounds_{n}_rb", (act.Offset + act.Size) * Scale);

			int ring = 0;
			foreach (Vector2i position in RingPositions.FromFile(Path(act.RingsFile)))
			{
				binding.ObjectPlacements.Add(new ObjectPlacement(RingResourceKey, MakeUid(zone, "ring", act, ring), $"Ring {n}-{ring}",
					FrontLayerIndex, (act.Offset + position) * Scale, new { }));
				ring++;
			}
			if (act.BumpersFile != null)
			{
				AddMapBumpers(zone, act, Path(act.BumpersFile), binding);
			}
			int index = 0;
			foreach (ObjectPlacements.OriginalObjectPlacement original in ObjectPlacements.FromFile(Path(act.ObjectsFile)))
			{
				// The signpost of act 2 deletes itself (Obj0D_Init): it is only for two player mode
				if ((original.Type == SignpostType && act.Number == 2) || zone.SkippedObjects.Contains(original.Type))
				{
					continue;
				}
				ObjectPlacement converted = ObjectPlacements.ConvertOriginalObjectPlacement(original, zone.ArtFile != null, zone.Name);
				if (converted == null)
				{
					unsupported[original.Type] = unsupported.GetValueOrDefault(original.Type) + 1;
					continue;
				}
				IDictionary<string, object> state = new ExpandoObject();
				foreach (KeyValuePair<string, object> value in converted.Behaviour)
				{
					state[value.Key] = value.Value;
				}
				string name = converted.Key.Substring(converted.Key.LastIndexOf('/') + 1).ToLowerInvariant();
				binding.ObjectPlacements.Add(new ObjectPlacement(converted.Key, MakeUid(zone, "object", act, index), $"{name} {n}-{index}",
					FrontLayerIndex, converted.Position + act.Offset * Scale, state));
				// The three rotating platforms of the original are one object: add the other two arms
				if (original.Type == 0x83)
				{
					for (int arm = 1; arm <= 2; arm++)
					{
						IDictionary<string, object> armState = new ExpandoObject();
						foreach (KeyValuePair<string, object> value in state)
						{
							armState[value.Key] = value.Value;
						}
						armState["Arm"] = arm;
						binding.ObjectPlacements.Add(new ObjectPlacement(converted.Key, MakeUid(zone, "arm" + arm, act, index), $"{name} {n}-{index}-{arm}",
							FrontLayerIndex, converted.Position + act.Offset * Scale, armState));
					}
				}
				index++;
			}
		}
		// The slot machine reels: one object on each reel window, where the map has the reel blocks
		var textures = new Dictionary<string, byte[]>();
		if (zone.Name == "CNZ" && File.Exists(Path(SlotReels.ArtFile)))
		{
			int windows = 0;
			for (int row = 0; row < rows; row++)
			{
				for (int column = 0; column < columns; column++)
				{
					if ((blocks[column, row] & 0x3FF) == SlotReels.WindowBlock)
					{
						var position = new Vector2i(column * BlockSize, row * BlockSize) * Scale;
						binding.ObjectPlacements.Add(new ObjectPlacement("SONICORCA/OBJECTS/CNZSLOTREELS", MakeUid(zone, "reels", zone.Acts[0], windows),
							$"cnzslotreels {windows}", FrontLayerIndex, position, new { }));
						windows++;
					}
				}
			}
			textures["SONICORCA/OBJECTS/CNZSLOTREELS/TEXTURE"] = SlotReels.RenderTexture(disassembly, palette, Scale);
		}
		foreach (KeyValuePair<int, int> pair in unsupported)
		{
			notes.Add($"Object type ${pair.Key:X2}: {pair.Value} not imported, because SonicOrca has no object for it.");
		}
		var sprites = zone.Sprites.Select(sprite => SpriteImport.Import(sprite, disassembly, palette, Scale)).ToList();
		// The objects of the binding, and the objects that only other objects or the area make (for example the bosses)
		var newObjects = binding.ObjectPlacements.Select(p => p.Key).Concat(zone.Sprites.Select(s => s.ObjectKey)).Concat(zone.AreaObjects).Distinct()
			.Where(RepositoryObjects.ContainsKey).ToDictionary(key => key, key => RepositoryObjects[key]);
		return new ImportedLevel(map, binding, notes, sprites, newObjects, tileSet, priorityTexture, textures);
	}

	/// <summary>
	/// Adds the bumpers in the landscape: 6-byte entries of kind (0 to $A), x and y. The file of act 1 has no start
	/// marker, and both files end with x = $FFFF.
	/// </summary>
	private static void AddMapBumpers(OriginalZone zone, OriginalAct act, string file, LevelBinding binding)
	{
		byte[] data = File.ReadAllBytes(file);
		int count = 0;
		for (int i = 0; i + 6 <= data.Length; i += 6)
		{
			int kind = (data[i] << 8) | data[i + 1];
			int x = (data[i + 2] << 8) | data[i + 3];
			int y = (data[i + 4] << 8) | data[i + 5];
			if (x == 0 || x == 0xFFFF || kind > 0xA)
			{
				continue;
			}
			binding.ObjectPlacements.Add(new ObjectPlacement("SONICORCA/OBJECTS/CNZMAPBUMPER", MakeUid(zone, "bumper", act, count),
				$"cnzmapbumper {act.Number}-{count}", FrontLayerIndex, (act.Offset + new Vector2i(x, y)) * Scale, new { Kind = kind >> 1 }));
			count++;
		}
	}

	/// <summary>The chunk entry of a block of an act layout, or 0 outside the layout.</summary>
	private static ushort GetBlock(byte[] chunks, byte[] layout, int x, int y, bool background)
	{
		if (x < 0 || y < 0 || x >= LayoutColumns * BlocksPerChunk || y >= LayoutRows * BlocksPerChunk)
		{
			return 0;
		}
		// Each layout row has 128 foreground chunks, then 128 background chunks
		int chunk = layout[256 * (y / BlocksPerChunk) + x / BlocksPerChunk + (background ? LayoutColumns : 0)];
		int offset = chunk * ChunkSize + (y % BlocksPerChunk) * 16 + (x % BlocksPerChunk) * 2;
		return (ushort)((chunks[offset] << 8) | chunks[offset + 1]);
	}

	/// <summary>A tile of the HD map: the block number, with the flips of the chunk entry.</summary>
	private static int ToTile(ushort block)
	{
		int tile = block & 0x3FF;
		if ((block & 0x400) != 0)
		{
			tile |= FlipXFlag;
		}
		if ((block & 0x800) != 0)
		{
			tile |= FlipYFlag;
		}
		return tile;
	}

	/// <summary>
	/// Draws the solid pixels of one path from the height maps (FindFloor in s2.asm), then traces them.
	/// </summary>
	/// <param name="collisionIndex">The collision number of each block number, for this path.</param>
	/// <param name="topBit">The bit of the chunk entry for top solidity on this path. The next bit is side solidity.</param>
	private static List<(Vector2i A, Vector2i B)> TraceCollision(ushort[,] blocks, byte[] collisionIndex, byte[] heights, int topBit)
	{
		int columns = blocks.GetLength(0);
		int rows = blocks.GetLength(1);
		int width = columns * BlockSize;
		int height = rows * BlockSize;
		var grid = new byte[width * height];
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				ushort block = blocks[column, row];
				byte flags = 0;
				if ((block & (1 << topBit)) != 0)
				{
					flags |= CollisionTracer.TopSolid;
				}
				if ((block & (1 << (topBit + 1))) != 0)
				{
					flags |= CollisionTracer.SidesSolid;
				}
				int collision = (block & 0x3FF) < collisionIndex.Length ? collisionIndex[block & 0x3FF] : 0;
				if (flags == 0 || collision == 0)
				{
					continue;
				}
				bool flipX = (block & 0x400) != 0;
				bool flipY = (block & 0x800) != 0;
				for (int x = 0; x < BlockSize; x++)
				{
					int columnHeight = (sbyte)heights[collision * BlockSize + (flipX ? BlockSize - 1 - x : x)];
					if (flipY)
					{
						columnHeight = -columnHeight;
					}
					// A positive height is solid from the bottom up, a negative height from the top down
					int top = columnHeight > 0 ? BlockSize - columnHeight : 0;
					int bottom = columnHeight > 0 ? BlockSize : -columnHeight;
					for (int y = top; y < bottom; y++)
					{
						grid[(row * BlockSize + y) * width + column * BlockSize + x] = flags;
					}
				}
			}
		}
		return CollisionTracer.Trace(grid, width, height, Scale);
	}

	/// <summary>The collision lines of both paths: the line, and the paths that use it (1, 2 or both).</summary>
	private static List<(Vector2i A, Vector2i B, uint Paths)> GetCollisionLines(ushort[,] blocks, byte[] primary, byte[] secondary, byte[] heights)
	{
		var primaryLines = TraceCollision(blocks, primary, heights, topBit: 12);
		var secondaryLines = TraceCollision(blocks, secondary, heights, topBit: 14);
		var secondarySet = secondaryLines.ToHashSet();
		var result = new List<(Vector2i, Vector2i, uint)>();
		foreach ((Vector2i a, Vector2i b) in primaryLines)
		{
			result.Add((a, b, secondarySet.Remove((a, b)) ? 3u : 1u));
		}
		foreach ((Vector2i a, Vector2i b) in secondaryLines.Where(secondarySet.Contains))
		{
			result.Add((a, b, 2u));
		}
		return result;
	}

	/// <summary>
	/// Adds the layout changes of the zone to the map, switched off. Change N has a hidden layer
	/// "change_N_name" with the new front tiles and "change_N_name_high" with the new priority tiles, a marker
	/// "change_N_name" at the top left of each changed chunk, and collision lines with the paths of the change in
	/// other bits (see <see cref="LayoutChangeBits"/>).
	/// </summary>
	private static void AddLayoutChanges(OriginalZone zone, LevelMap map, List<LevelLayer> layers, byte[] chunks, byte[][] layouts,
		ushort[,] blocks, byte[] primary, byte[] secondary, byte[] heights, Func<ushort, int> getFrontTile, Func<ushort, int> getHighTile)
	{
		LevelLayer front = layers[FrontLayerIndex];
		List<(Vector2i A, Vector2i B, uint Paths)> lines = null;
		for (int index = 0; index < zone.LayoutChanges.Count; index++)
		{
			LayoutChange change = zone.LayoutChanges[index];
			int actIndex = zone.Acts.ToList().FindIndex(a => a.Number == change.Act);
			OriginalAct act = zone.Acts[actIndex];
			byte[] layout = (byte[])layouts[actIndex].Clone();
			foreach ((int column, int row, int chunk) in change.Chunks)
			{
				layout[256 * row + column] = (byte)chunk;
			}
			ushort[,] changed = (ushort[,])blocks.Clone();
			string name = $"change_{index}_{change.Name}";
			var changeFront = new LevelLayer(map) { Name = name, Visible = false };
			var changeHigh = new LevelLayer(map) { Name = name + "_high", Visible = false };
			changeFront.Resize(front.Columns, front.Rows);
			changeHigh.Resize(front.Columns, front.Rows);
			foreach ((int column, int row, int _) in change.Chunks)
			{
				int left = act.Offset.X / BlockSize + column * BlocksPerChunk;
				int top = act.Offset.Y / BlockSize + row * BlocksPerChunk;
				AddMarker(map, front, name, new Vector2i(left, top) * BlockSize * Scale);
				for (int y = 0; y < BlocksPerChunk; y++)
				{
					for (int x = 0; x < BlocksPerChunk; x++)
					{
						if (left + x >= front.Columns || top + y >= front.Rows)
						{
							continue;
						}
						ushort entry = GetBlock(chunks, layout, column * BlocksPerChunk + x, row * BlocksPerChunk + y, background: false);
						changed[left + x, top + y] = entry;
						changeFront.Tiles[left + x, top + y] = getFrontTile(entry);
						changeHigh.Tiles[left + x, top + y] = getHighTile(entry);
					}
				}
			}
			layers.Add(changeFront);
			layers.Add(changeHigh);

			// The lines that only the changed layout has are added switched off. The lines that only the original
			// layout has get a mark, so that the change can switch them off.
			lines ??= GetCollisionLines(blocks, primary, secondary, heights);
			var changedLines = GetCollisionLines(changed, primary, secondary, heights);
			var before = lines.ToHashSet();
			var after = changedLines.ToHashSet();
			(int shift, uint removedBit) = LayoutChangeBits(index);
			foreach (CollisionVector vector in map.CollisionVectors.Where(v => (v.Paths & 3) != 0 && !after.Contains((v.RelativeA, v.RelativeB, v.Paths & 3))))
			{
				vector.Paths |= removedBit;
			}
			foreach ((Vector2i a, Vector2i b, uint paths) in changedLines.Where(l => !before.Contains(l)))
			{
				map.CollisionVectors.Add(new CollisionVector(a, b, paths << shift));
			}
		}
	}

	/// <summary>
	/// The bits of the collision paths of layout change N: the paths of its new lines, from bit <c>shift</c>, and the
	/// mark of the lines that it takes away. The engine uses only bits 0 and 1, so these lines are not active.
	/// </summary>
	public static (int Shift, uint RemovedBit) LayoutChangeBits(int index) => (2 + index * 3, 1u << (4 + index * 3));

	private static void AddMarker(LevelMap map, LevelLayer layer, string name, Vector2i position)
	{
		map.Markers.Add(new LevelMarker(name, null, position, layer));
	}

	/// <summary>A stable unique ID, so that an import gives the same file each time.</summary>
	private static Guid MakeUid(OriginalZone zone, string kind, OriginalAct act, int index)
	{
		byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes($"{zone.Key}/{kind}/{act.Number}/{index}"));
		return new Guid(hash);
	}
}
