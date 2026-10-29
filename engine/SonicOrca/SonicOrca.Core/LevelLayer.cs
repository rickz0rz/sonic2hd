using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// One layer of tiles in a level map. It can scroll with parallax, wrap, and have rows that scroll at their own speed.
/// </summary>
public class LevelLayer : ILevelLayerTreeNode
{
	// The width and height of a tile in pixels.
	private const int TileSize = 64;

	private readonly LevelMap _map;

	private readonly List<LayerRowDefinition> _layerRowDefinitions = new List<LayerRowDefinition>();

	private readonly List<LevelLayerShadow> _shadows = new List<LevelLayerShadow>();

	public Level Level => _map.Level;

	public LevelMap Map => _map;

	public int Index { get; set; }

	public string Name { get; set; }

	/// <summary>True when the layer draws at its level positions, without parallax, offset and wrap (for the level editor).</summary>
	public bool Editing { get; set; }

	public bool Visible { get; set; }

	/// <summary>True when the level editor hides the layer for a time. The map file does not keep this.</summary>
	public bool HiddenInEditor { get; set; }

	public IList<ILevelLayerTreeNode> Children => null;

	public IList<LayerRowDefinition> LayerRowDefinitions => _layerRowDefinitions;

	public int Columns { get; private set; }

	public int Rows { get; private set; }

	public int[,] Tiles { get; private set; }

	public int OffsetY { get; set; }

	public double ParallaxY { get; set; }

	public bool AutomaticYParallax { get; set; }

	public bool WrapX { get; set; }

	public bool WrapY { get; set; }

	public IList<LevelLayerShadow> Shadows => _shadows;

	public LevelLayerLighting Lighting { get; set; }

	public IEnumerable<Rectanglei> WaterfallEffects { get; set; }

	public Colour MiniMapColour { get; set; }

	public LevelLayer(LevelMap map)
	{
		_map = map;
		Visible = true;
		Columns = 0;
		Rows = 0;
		Tiles = new int[0, 0];
		Name = string.Empty;
		ParallaxY = 1.0;
		Lighting = new LevelLayerLighting();
	}

	public override string ToString()
	{
		if (!string.IsNullOrEmpty(Name))
		{
			return Name;
		}
		return base.ToString();
	}

	public void Resize(int columns, int rows)
	{
		int rowsToCopy = Math.Min(rows, Rows);
		int columnsToCopy = Math.Min(columns, Columns);
		int[,] newTiles = new int[columns, rows];
		for (int y = 0; y < rowsToCopy; y++)
		{
			for (int x = 0; x < columnsToCopy; x++)
			{
				newTiles[x, y] = Tiles[x, y];
			}
		}
		Columns = columns;
		Rows = rows;
		Tiles = newTiles;
	}

	public void Update()
	{
	}

	public void Animate()
	{
		foreach (LayerRowDefinition layerRowDefinition in _layerRowDefinitions)
		{
			layerRowDefinition.Animate();
		}
	}

	public LayerRowDefinition GetRowDefinitionAt(int y, out int top)
	{
		int rowTop = 0;
		foreach (LayerRowDefinition layerRowDefinition in _layerRowDefinitions)
		{
			if (y >= rowTop && y < rowTop + layerRowDefinition.Height)
			{
				top = rowTop;
				return layerRowDefinition;
			}
			rowTop += layerRowDefinition.Height;
		}
		top = 0;
		return null;
	}

	public void Merge(LevelLayer layer)
	{
		// The tiles of the other layer replace the tiles of this layer, except where the other tile is empty (0).
		int columns = Math.Max(Columns, layer.Columns);
		int rows = Math.Max(Rows, layer.Rows);
		int[,] mergedTiles = new int[columns, rows];
		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < columns; x++)
			{
				if (Columns > x && Rows > y)
				{
					mergedTiles[x, y] = Tiles[x, y];
				}
				if (layer.Columns > x && layer.Rows > y && layer.Tiles[x, y] != 0)
				{
					mergedTiles[x, y] = layer.Tiles[x, y];
				}
			}
		}
		Columns = columns;
		Rows = rows;
		Tiles = mergedTiles;
	}
}
