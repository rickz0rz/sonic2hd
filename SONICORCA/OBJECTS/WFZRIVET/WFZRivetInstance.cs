using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZRIVET;

/// <summary>
/// The rivet on the deck of the ship at the end of Wing Fortress Zone, as in Sonic 2 (ObjC2). It is a solid block
/// $10 pixels to each side and 8 pixels up and down. When the main player lands on it in a ball, it explodes and
/// opens the deck. The original writes other chunks into the level layout: chunks $70 to $73 and $78, $79 in place of
/// $8A to $8F, at columns $51 to $54 of rows 8 and 9. This object makes the same change to the tiles and the
/// collision of a map from "s2hd-data import-level wfz": a hole in the deck below the rivet, the inside of the ship
/// and the floor of the boss room.
/// </summary>
/// <remarks>
/// The tiles and the collision lines of the change come from an import of the changed layout. The map is a shared
/// resource, so the object keeps what it changed. A new rivet (after a restart of the level) puts the map back.
/// </remarks>
public class WFZRivetInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0x10;

	private const int HalfHeight = 8;

	private const int BlockSize = 16 * Scale;

	/// <summary>The first block of the change: column $51 of the layout ($2880), row 8 ($400) plus 4 blocks.</summary>
	private const int FirstColumn = 0x2880 / 16;

	private const int FirstRow = 0x400 / 16 + 4;

	private const int Columns = 32;

	/// <summary>The tiles of the HD tile set for each block. The part of the priority layer is at tile + 1024.</summary>
	private const int PriorityTile = 1024;

	/// <summary>The blocks that have high-priority patterns, so they also have a tile on the priority layer.</summary>
	private static readonly HashSet<int> PriorityBlocks = new HashSet<int> { 0x007, 0x011, 0x1DD, 0x1DE, 0x1DF, 0x1E9, 0x1F3 };

	/// <summary>The front tiles of rows 4 to 15 of the four chunks. The top 4 rows do not change. $4000 is a flip in x.</summary>
	private static readonly int[][] Tiles =
	{
		Row(Chunk(0x1DD), HoleTop(0x1DD), Chunk(0x1DD), Chunk(0x1DD)),
		Row(Chunk(0x1DE, 0x1DF), HoleTop(0x1DE, 0x1DF), Chunk(0x1DE, 0x1DF), Chunk(0x1DE, 0x1DF)),
		Row(Hull(0x1E5, 0x1E6, 0x41E5, 0x1E7, 0x1E8, 0x1E8, 0x1E8, 0x1E7), 0x1E7),
		Row(Hull(0x1EF, 0x1F0, 0x41EF, 0x1F1, 0x1F2, 0x1F2, 0x1F2, 0x1F1), 0x1F1),
		Row(Hull(0x1EF, 0x1F0, 0x41EF, 0x1F1, 0x202, 0x202, 0x202, 0x1F1), 0x1F1),
		Repeat(0x028, 0x029, 0x4028, 0x1F1),
		Repeat(0x8028, 0x8029, 0xC028, 0x1F1),
		Row(Hull(0x1EF, 0x1F0, 0x41EF, 0x1F1, 0x202, 0x202, 0x202, 0x1F1), 0x1F1),
		Repeat(0x1DD),
		Repeat(0x1E9, 0x1F3),
		Repeat(0x190, 0x191, 0x192, 0x193, 0x4004, 0x006, 0x007, 0x4007),
		Repeat(0x19A, 0x19B, 0x19C, 0x19D, 0x400E, 0x006, 0x011, 0x4011),
	};

	/// <summary>The collision lines that the change removes, in map pixels from the act origin (a, b, paths).</summary>
	private static readonly (int AX, int AY, int BX, int BY, uint Paths)[] RemovedLines =
	{
		(39516, 4266, 39648, 4310, 1), (39648, 4310, 39788, 4338, 1), (39788, 4338, 39936, 4350, 1),
		(39936, 4350, 42496, 4352, 1), (42496, 4352, 42496, 3968, 1), (43200, 5120, 43520, 5120, 1),
		(43520, 5120, 43520, 4992, 1), (43968, 4480, 39040, 4480, 1),
	};

	/// <summary>The collision lines that the change adds: the deck around the hole and the floor of the boss room.</summary>
	private static readonly (int AX, int AY, int BX, int BY, uint Paths)[] AddedLines =
	{
		(39516, 4266, 39620, 4302, 1), (39620, 4302, 39760, 4334, 1), (39760, 4334, 39872, 4346, 1),
		(39872, 4346, 39936, 4350, 1), (39936, 4350, 42240, 4352, 1), (41472, 4864, 43520, 4864, 1),
		(41472, 4992, 41472, 4864, 1), (41856, 4992, 41472, 4992, 1), (41856, 5120, 41856, 4992, 1),
		(41984, 4992, 41984, 5120, 1), (41984, 5120, 41856, 5120, 1), (42240, 4352, 42240, 4480, 1),
		(42240, 4480, 39040, 4480, 1), (42368, 4992, 41984, 4992, 1), (42368, 5120, 42368, 4992, 1),
		(42496, 4480, 42496, 3968, 1), (42496, 4992, 42496, 5120, 1), (42496, 5120, 42368, 5120, 1),
		(42880, 4992, 42496, 4992, 1), (42880, 5120, 42880, 4992, 1), (43008, 4992, 43008, 5120, 1),
		(43008, 5120, 42880, 5120, 1), (43200, 5120, 43392, 5120, 1), (43392, 4992, 43008, 4992, 1),
		(43392, 5120, 43392, 4992, 1), (43520, 4864, 43520, 4992, 1), (43968, 4480, 42496, 4480, 1),
	};

	/// <summary>What the rivet changed in each map, so that a new rivet can put it back.</summary>
	private static readonly ConditionalWeakTable<LevelMap, MapChange> Changes = new ConditionalWeakTable<LevelMap, MapChange>();

	private AnimationInstance _animation;

	private bool _playerInBall;

	private sealed class MapChange
	{
		public readonly List<(LevelLayer Layer, int Column, int Row, int Tile)> OldTiles = new List<(LevelLayer, int, int, int)>();

		public readonly List<CollisionVector> RemovedVectors = new List<CollisionVector>();

		public readonly List<CollisionVector> AddedVectors = new List<CollisionVector>();
	}

	public WFZRivetInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZRivetType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
		if (Changes.TryGetValue(Level.Map, out MapChange change))
		{
			Undo(change);
			Changes.Remove(Level.Map);
		}
	}

	protected override void OnUpdatePrepare()
	{
		// A character in a ball that lands stops being in a ball when it updates. The original also looks at the
		// animation of the player before it lands.
		_playerInBall = Level.Player.Protagonist?.IsSpinball == true;
	}

	protected override void OnUpdate()
	{
		ICharacter player = Level.Player.Protagonist;
		if (player == null || player.ObjectLink != this || !_playerInBall)
		{
			return;
		}
		// ObjC2_Bust
		player.LeaveGround();
		player.IsAirborne = true;
		CollisionVectors = new CollisionVector[0];
		Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(WFZRivetType.ExplosionResourceKey), Level.Map.Layers.IndexOf(Layer), Position));
		OpenDeck();
		FinishForever();
	}

	/// <summary>Changes the tiles and the collision of the map, as the original changes the level layout.</summary>
	private void OpenDeck()
	{
		LevelMap map = Level.Map;
		LevelLayer front = map.Layers.FirstOrDefault(l => l.Name == "front");
		LevelLayer high = map.Layers.FirstOrDefault(l => l.Name == "front_high");
		Vector2i origin = Level.TryGetMarker("origin_1", out LevelMarker marker) ? marker.Position : new Vector2i();
		int firstColumn = origin.X / BlockSize + FirstColumn;
		int firstRow = origin.Y / BlockSize + FirstRow;
		// Only a map from "s2hd-data import-level wfz" has the deck at this place
		if (front == null || high == null || Changes.TryGetValue(map, out _) ||
			firstColumn + Columns > front.Columns || firstRow + Tiles.Length > front.Rows || (front.Tiles[firstColumn + 1, firstRow] & 0x3FF) != 0xB2)
		{
			return;
		}
		var change = new MapChange();
		for (int row = 0; row < Tiles.Length; row++)
		{
			for (int column = 0; column < Columns; column++)
			{
				int x = firstColumn + column;
				int y = firstRow + row;
				int tile = Tiles[row][column];
				change.OldTiles.Add((front, x, y, front.Tiles[x, y]));
				change.OldTiles.Add((high, x, y, high.Tiles[x, y]));
				front.Tiles[x, y] = tile;
				high.Tiles[x, y] = PriorityBlocks.Contains(tile & 0x3FF) ? PriorityTile + tile : 0;
			}
		}
		foreach ((int ax, int ay, int bx, int by, uint paths) in RemovedLines)
		{
			Vector2i a = origin + new Vector2i(ax, ay);
			Vector2i b = origin + new Vector2i(bx, by);
			CollisionVector vector = map.CollisionVectors.FirstOrDefault(v => v.Owner == null && v.AbsoluteA == a && v.AbsoluteB == b && v.Paths == paths);
			if (vector != null)
			{
				change.RemovedVectors.Add(vector);
			}
		}
		foreach ((int ax, int ay, int bx, int by, uint paths) in AddedLines)
		{
			change.AddedVectors.Add(new CollisionVector(origin + new Vector2i(ax, ay), origin + new Vector2i(bx, by), paths));
		}
		ReplaceVectors(change.RemovedVectors, change.AddedVectors);
		Changes.Add(map, change);
	}

	/// <summary>Puts back the tiles and the collision lines that the rivet changed.</summary>
	private void Undo(MapChange change)
	{
		foreach ((LevelLayer layer, int column, int row, int tile) in change.OldTiles)
		{
			layer.Tiles[column, row] = tile;
		}
		ReplaceVectors(change.AddedVectors, change.RemovedVectors);
	}

	/// <summary>Takes landscape collision lines out of the map and puts others in, then connects the lines again.</summary>
	private void ReplaceVectors(IReadOnlyCollection<CollisionVector> oldVectors, IReadOnlyCollection<CollisionVector> newVectors)
	{
		LevelMap map = Level.Map;
		CollisionTable table = Level.CollisionTable;
		var neighbours = new HashSet<CollisionVector>(oldVectors.SelectMany(v => table.GetVectorConnections(v).Where(c => c != null)));
		foreach (CollisionVector vector in oldVectors)
		{
			map.CollisionVectors.Remove(vector);
			table.InternalTree.Remove(vector);
		}
		foreach (CollisionVector vector in newVectors)
		{
			vector.UpdateDerrivedFields();
			map.CollisionVectors.Add(vector);
			table.InternalTree.Add(vector);
		}
		neighbours.ExceptWith(oldVectors);
		table.UpdateConnectionsFast(newVectors.Concat(neighbours).ToArray());
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = 0;
		renderer.GetObjectRenderer().Render(_animation);
	}

	/// <summary>The same blocks for the 8 columns of a chunk, or two blocks one after the other.</summary>
	private static int[] Chunk(params int[] blocks) => Enumerable.Range(0, 8).Select(i => blocks[i % blocks.Length]).ToArray();

	/// <summary>The rows of chunk $71 at the hole: the deck on the left half, the inside of the ship on the right half.</summary>
	private static int[] HoleTop(params int[] blocks) => Chunk(blocks).Take(4).Concat(new[] { 0x1EF, 0x1F0, 0x41EF, 0x1E7 }).ToArray();

	/// <summary>A row of the hull. Chunk $71 has the inside of the ship in its right half, which ends with <paramref name="holeEnd"/>.</summary>
	private static int[] Row(int[] hull, int holeEnd) => Row(hull, hull.Take(4).Concat(new[] { 0x1EF, 0x1F0, 0x41EF, holeEnd }).ToArray(), hull, hull);

	private static int[] Hull(params int[] blocks) => blocks;

	private static int[] Row(params int[][] chunks) => chunks.SelectMany(c => c).ToArray();

	private static int[] Repeat(params int[] blocks) => Enumerable.Range(0, Columns).Select(i => blocks[i % blocks.Length]).ToArray();
}
