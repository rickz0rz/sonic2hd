using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// Switches on the layout changes that "s2hd-data import-level" writes into a map, for example the walls that close a
/// boss arena. The original writes other chunks into the level layout during play. The import writes the new tiles
/// into hidden layers ("change_N_name" and "change_N_name_high"), a marker "change_N_name" at each changed chunk, and
/// the collision lines of the change in path bits that the engine does not use.
/// </summary>
/// <remarks>
/// The map is a shared resource, so a change stays when the level starts again. <see cref="Reset"/> puts back all
/// the changes of a map. The area calls it before it loads the map.
/// </remarks>
public static class LayoutChanges
{
	private const int ChunkBlocks = 8;

	private const int BlockSize = 64;

	private const uint PathMask = 3;

	/// <summary>What each change changed in a map, so that <see cref="Reset"/> can put it back.</summary>
	private static readonly ConditionalWeakTable<LevelMap, List<AppliedChange>> Applied = new ConditionalWeakTable<LevelMap, List<AppliedChange>>();

	private sealed class AppliedChange
	{
		public string Name;

		public readonly List<(LevelLayer Layer, int Column, int Row, int Tile)> OldTiles = new List<(LevelLayer, int, int, int)>();

		public readonly List<(CollisionVector Vector, uint Paths)> OldPaths = new List<(CollisionVector, uint)>();
	}

	/// <summary>True when the map has the change.</summary>
	public static bool Exists(Level level, string name) => FindChange(level.Map, name).Index >= 0;

	/// <summary>True when the change is on.</summary>
	public static bool IsApplied(Level level, string name) =>
		Applied.TryGetValue(level.Map, out List<AppliedChange> changes) && changes.Any(c => c.Name == name);

	/// <summary>Switches on a change: its tiles go into the front layers, and its collision lines take the place of the old ones.</summary>
	public static bool Apply(Level level, string name)
	{
		LevelMap map = level.Map;
		(int index, LevelLayer front, LevelLayer high) = FindChange(map, name);
		if (index < 0 || IsApplied(level, name))
		{
			return false;
		}
		LevelLayer mapFront = map.Layers[map.CollisionPathLayers.Count > 0 ? map.CollisionPathLayers[0] : 1];
		LevelLayer mapHigh = map.Layers.FirstOrDefault(l => l.Name == "front_high");
		var applied = new AppliedChange { Name = name };
		string markerName = front.Name.ToLowerInvariant();
		foreach (LevelMarker marker in map.Markers.Where(m => m.Name?.ToLowerInvariant() == markerName))
		{
			int left = marker.Position.X / BlockSize;
			int top = marker.Position.Y / BlockSize;
			for (int row = top; row < top + ChunkBlocks && row < mapFront.Rows; row++)
			{
				for (int column = left; column < left + ChunkBlocks && column < mapFront.Columns; column++)
				{
					applied.OldTiles.Add((mapFront, column, row, mapFront.Tiles[column, row]));
					mapFront.Tiles[column, row] = front.Tiles[column, row];
					if (mapHigh != null)
					{
						applied.OldTiles.Add((mapHigh, column, row, mapHigh.Tiles[column, row]));
						mapHigh.Tiles[column, row] = high.Tiles[column, row];
					}
				}
			}
		}
		(int shift, uint removedBit) = GetBits(index);
		foreach (CollisionVector vector in map.CollisionVectors.Where(v => v.Owner == null))
		{
			uint paths = vector.Paths;
			if ((paths & removedBit) != 0 && (paths & PathMask) != 0)
			{
				applied.OldPaths.Add((vector, paths));
				vector.Paths = paths & ~PathMask;
			}
			else if ((paths & PathMask) == 0 && ((paths >> shift) & PathMask) != 0)
			{
				applied.OldPaths.Add((vector, paths));
				vector.Paths = paths | ((paths >> shift) & PathMask);
			}
		}
		UpdateConnections(level, applied.OldPaths.Select(p => p.Vector));
		Applied.GetOrCreateValue(map).Add(applied);
		return true;
	}

	/// <summary>Switches off a change: its old tiles and collision lines come back.</summary>
	public static bool Undo(Level level, string name)
	{
		if (!Applied.TryGetValue(level.Map, out List<AppliedChange> changes))
		{
			return false;
		}
		AppliedChange applied = changes.FirstOrDefault(c => c.Name == name);
		if (applied == null)
		{
			return false;
		}
		Restore(applied);
		changes.Remove(applied);
		UpdateConnections(level, applied.OldPaths.Select(p => p.Vector));
		return true;
	}

	/// <summary>
	/// Puts back the tiles and the collision lines of all the changes of a map. Call it before the level loads the
	/// map, because the collision table connects the lines when it loads the map.
	/// </summary>
	public static void Reset(LevelMap map)
	{
		if (map == null || !Applied.TryGetValue(map, out List<AppliedChange> changes))
		{
			return;
		}
		for (int i = changes.Count - 1; i >= 0; i--)
		{
			Restore(changes[i]);
		}
		changes.Clear();
	}

	private static void Restore(AppliedChange applied)
	{
		foreach ((LevelLayer layer, int column, int row, int tile) in applied.OldTiles)
		{
			layer.Tiles[column, row] = tile;
		}
		foreach ((CollisionVector vector, uint paths) in applied.OldPaths)
		{
			vector.Paths = paths;
		}
	}

	/// <summary>The bits of change N, as LevelImport.LayoutChangeBits makes them.</summary>
	private static (int Shift, uint RemovedBit) GetBits(int index) => (2 + index * 3, 1u << (4 + index * 3));

	private static (int Index, LevelLayer Front, LevelLayer High) FindChange(LevelMap map, string name)
	{
		foreach (LevelLayer layer in map.Layers)
		{
			string[] parts = layer.Name?.Split('_', 3);
			if (parts != null && parts.Length == 3 && parts[0] == "change" && parts[2] == name && int.TryParse(parts[1], out int index))
			{
				LevelLayer high = map.Layers.FirstOrDefault(l => l.Name == layer.Name + "_high");
				return high != null ? (index, layer, high) : (-1, null, null);
			}
		}
		return (-1, null, null);
	}

	/// <summary>Connects the changed lines and their neighbours again on each path.</summary>
	private static void UpdateConnections(Level level, IEnumerable<CollisionVector> vectors)
	{
		CollisionVector[] changed = vectors.ToArray();
		foreach (CollisionVector vector in changed)
		{
			vector.UpdateDerrivedFields();
		}
		level.CollisionTable.UpdateConnectionsFast(changed);
	}
}
