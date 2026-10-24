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
public sealed record OriginalAct(int Number, string LayoutFile, string ObjectsFile, string RingsFile, string StartFile,
	Vector2i Offset, int FirstColumn, Vector2i Size);

/// <summary>A zone of the original game, as the files of the Sonic 2 disassembly (github.com/sonicretro/s2disasm) give it.</summary>
/// <param name="Key">The resource key of the zone, for example "SONICORCA/LEVELS/CNZ".</param>
/// <param name="SecondaryCollisionFile">The collision index of the second path, or null when the zone has one path.</param>
/// <param name="MapSize">The size of the front layer, in blocks (16 original pixels).</param>
/// <param name="PaletteFile">The zone palette, for the sprites of its objects.</param>
/// <param name="Sprites">The art of the objects that the game data does not have.</param>
public sealed record OriginalZone(string Name, string Key, string ChunksFile, string PrimaryCollisionFile,
	string SecondaryCollisionFile, IReadOnlyList<OriginalAct> Acts, Vector2i MapSize, string PaletteFile,
	IReadOnlyList<OriginalSprite> Sprites)
{
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
					new Vector2i(0, 0), 0, new Vector2i(0x27A0 + 320, 0x720 + 224)),
				new OriginalAct(2, "level/layout/CNZ_2.kos", "level/objects/CNZ_2.bin", "level/rings/CNZ_2.bin", "startpos/CNZ_2.bin",
					new Vector2i(85 * 128, 2 * 128), 85 * 8, new Vector2i(0x2A80 + 320, 0x720 + 224)),
			},
			new Vector2i(1400, 144), "art/palettes/CNZ.bin",
			new[]
			{
				new OriginalSprite("SONICORCA/OBJECTS/CNZBUMPER", "art/nemesis/Round bumper from CNZ.nem", "mappings/sprite/obj44.asm", 2),
				new OriginalSprite("SONICORCA/OBJECTS/CNZHEXBUMPER", "art/nemesis/Hexagonal bumper from CNZ.nem", "mappings/sprite/objD7.asm", 2),
				// Each hit on a bonus block moves it to the next palette line: 2, 1, then 0
				new OriginalSprite("SONICORCA/OBJECTS/CNZBONUSBLOCK", "art/nemesis/Drop target from CNZ.nem", "mappings/sprite/objD8.asm", new[] { 2, 1, 0 }),
			}),
	};
}

/// <summary>The result of a level import: the map, the binding, and notes about what was not imported.</summary>
/// <param name="Sprites">The art of the objects that the game data does not have.</param>
/// <param name="NewObjects">The objects that the binding uses and that the game data does not have: the resource key and the namespace of the code.</param>
public sealed record ImportedLevel(LevelMap Map, LevelBinding Binding, IReadOnlyList<string> Notes,
	IReadOnlyList<ImportedSprite> Sprites, IReadOnlyDictionary<string, string> NewObjects);

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
		var back = new LevelLayer(map) { Name = "back", WrapX = true, ParallaxY = 0.5 };
		back.LayerRowDefinitions.Add(new LayerRowDefinition { Height = LayoutRows * ChunkSize * Scale, Parallax = 0.5 });
		back.Resize(LayoutColumns * BlocksPerChunk, LayoutRows * BlocksPerChunk);
		for (int row = 0; row < back.Rows; row++)
		{
			for (int column = 0; column < back.Columns; column++)
			{
				back.Tiles[column, row] = ToTile(GetBlock(chunks, layouts[0], column, row, background: true));
			}
		}
		var front = new LevelLayer(map) { Name = "front" };
		front.Resize(columns, rows);
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				front.Tiles[column, row] = ToTile(blocks[column, row]);
			}
		}
		foreach (LevelLayer layer in new[] { back, front })
		{
			map.LayerTree.Children.Add(layer);
			map.Layers.Add(layer);
		}

		// Path 0 is the primary collision and path 1 the secondary collision, as the layer switches expect
		map.CollisionPathLayers.Add(FrontLayerIndex);
		map.CollisionPathLayers.Add(FrontLayerIndex);
		var primaryLines = TraceCollision(blocks, primary, heights, topBit: 12);
		var secondaryLines = TraceCollision(blocks, secondary, heights, topBit: 14);
		var secondarySet = secondaryLines.ToHashSet();
		foreach ((Vector2i a, Vector2i b) in primaryLines)
		{
			map.CollisionVectors.Add(new CollisionVector(a, b, secondarySet.Remove((a, b)) ? 3u : 1u));
		}
		foreach ((Vector2i a, Vector2i b) in secondaryLines.Where(secondarySet.Contains))
		{
			map.CollisionVectors.Add(new CollisionVector(a, b, 2u));
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
			AddMarker(map, front, $"bounds_{n}_lt", act.Offset * Scale);
			AddMarker(map, front, $"bounds_{n}_rb", (act.Offset + act.Size) * Scale);

			int ring = 0;
			foreach (Vector2i position in RingPositions.FromFile(Path(act.RingsFile)))
			{
				binding.ObjectPlacements.Add(new ObjectPlacement(RingResourceKey, MakeUid(zone, "ring", act, ring), $"Ring {n}-{ring}",
					FrontLayerIndex, (act.Offset + position) * Scale, new { }));
				ring++;
			}
			int index = 0;
			foreach (ObjectPlacements.OriginalObjectPlacement original in ObjectPlacements.FromFile(Path(act.ObjectsFile)))
			{
				ObjectPlacement converted = ObjectPlacements.ConvertOriginalObjectPlacement(original);
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
				index++;
			}
		}
		foreach (KeyValuePair<int, int> pair in unsupported)
		{
			notes.Add($"Object type ${pair.Key:X2}: {pair.Value} not imported, because SonicOrca has no object for it.");
		}
		uint[] palette = SpriteImport.ReadPalette(disassembly, zone.PaletteFile);
		var sprites = zone.Sprites.Select(sprite => SpriteImport.Import(sprite, disassembly, palette, Scale)).ToList();
		var newObjects = binding.ObjectPlacements.Select(p => p.Key).Distinct().Where(RepositoryObjects.ContainsKey)
			.ToDictionary(key => key, key => RepositoryObjects[key]);
		return new ImportedLevel(map, binding, notes, sprites, newObjects);
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
