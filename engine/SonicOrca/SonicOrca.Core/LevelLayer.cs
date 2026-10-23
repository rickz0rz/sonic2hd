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

	public bool Editing { get; set; }

	public bool Visible { get; set; }

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

	public void Draw(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions, LevelLayerShadow shadow)
	{
		if (!Visible)
		{
			return;
		}
		if (Editing)
		{
			DrawNonLayerTiles(renderer, viewport, viewOptions);
		}
		using (viewport.ApplyRendererState(renderer))
		{
			if (viewOptions.ShowObjects)
			{
				Level.ObjectManager.Draw(renderer, viewport, this, viewOptions, priority: false);
			}
			if (viewOptions.ShowLandscape)
			{
				Matrix4 modelMatrix = Matrix4.Identity;
				if (viewOptions.Shadows)
				{
					modelMatrix = Matrix4.CreateTranslation((Vector2i)new Vector2(shadow.Displacement.X * viewport.Scale.X, shadow.Displacement.Y * viewport.Scale.Y));
				}
				ITileRenderer tileRenderer = renderer.GetTileRenderer();
				tileRenderer.ClipRectangle = viewport.Destination;
				tileRenderer.ModelMatrix = modelMatrix;
				tileRenderer.Textures = _map.Level.TileSet.Textures.ToArray();
				tileRenderer.Filter = viewOptions.Filter;
				tileRenderer.FilterAmount = viewOptions.FilterAmount;
				tileRenderer.BeginRender();
				DrawTilesVertical(renderer, viewport, viewOptions);
				tileRenderer.EndRender();
			}
			if (viewOptions.ShowObjects)
			{
				Level.ObjectManager.Draw(renderer, viewport, this, viewOptions, priority: true);
			}
			renderer.DeativateRenderer();
		}
	}

	public void DrawNonLayerTiles(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions)
	{
		// Mark the screen areas to the right of and below the layer, in screen pixels.
		int layerRight = Math.Max(0, Columns * TileSize - viewport.Bounds.X);
		int layerBottom = Math.Max(0, Rows * TileSize - viewport.Bounds.Y);
		layerRight = (int)(layerRight * viewport.Scale.X);
		layerBottom = (int)(layerBottom * viewport.Scale.Y);
		List<Rectanglei> areas = new List<Rectanglei>(2);
		if (layerRight < viewport.Destination.Width)
		{
			Rectanglei rightArea = new Rectanglei(layerRight, 0, viewport.Destination.Width - layerRight, viewport.Destination.Height);
			areas.Add(rightArea);
		}
		if (layerBottom < viewport.Destination.Height && layerRight > 0)
		{
			Rectanglei bottomArea = new Rectanglei(0, layerBottom, Math.Min(viewport.Destination.Width, layerRight), viewport.Destination.Height - layerBottom);
			areas.Add(bottomArea);
		}
		if (areas.Count > 0)
		{
			renderer.GetNonLayerRenderer().Render(areas);
		}
	}

	private void DrawTilesVertical(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions)
	{
		if (Rows == 0 || Columns == 0)
		{
			return;
		}
		// Draw the layer in horizontal strips from the top of the screen down. DrawTilesHorizontal sets the strip height.
		bool wrap = !Editing && WrapY;
		int offsetY = (!Editing) ? OffsetY : 0;
		double parallax = Editing ? 1.0 : ParallaxY;
		int layerHeight = (int)(Rows * TileSize * viewport.Scale.Y);
		int scrollY = (int)(viewport.Bounds.Y * viewport.Scale.Y * parallax);
		if (!wrap && scrollY >= layerHeight)
		{
			return;
		}
		int sourceY = scrollY % layerHeight;
		int destinationY = viewport.Destination.Y + offsetY;
		while (destinationY < viewport.Destination.Bottom)
		{
			int heightLeftInLayer = layerHeight - sourceY;
			int heightLeftOnScreen = viewport.Destination.Bottom - destinationY;
			int height = Math.Min(heightLeftInLayer, heightLeftOnScreen);
			DrawTilesHorizontal(renderer, viewport, viewOptions, sourceY, destinationY, ref height);
			if (height > 0)
			{
				sourceY += height;
				destinationY += height;
				if (wrap)
				{
					sourceY %= layerHeight;
				}
				else if (sourceY > layerHeight)
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	private void DrawTilesHorizontal(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions, int sourceY, int destinationY, ref int height)
	{
		// Note: the vertical layer size uses the horizontal scale. This is correct only when both scales are equal.
		int scaledTileSize = (int)(64.0 * viewport.Scale.X);
		int layerWidth = Columns * scaledTileSize;
		int layerHeight = Rows * scaledTileSize;
		bool wrap = !Editing && WrapX;
		// Note: the horizontal parallax starts at ParallaxY. A row definition can replace it.
		double parallax = Editing ? 1.0 : ParallaxY;
		int rowOffset = 0;
		LayerRowDefinition rowDefinition = GetRowDefinitionAt((int)(sourceY / viewport.Scale.Y), out int top);
		if (rowDefinition != null && !Editing)
		{
			parallax = rowDefinition.Parallax;
			rowOffset = (int)rowDefinition.CurrentOffset;
			if (rowDefinition.Width != 0)
			{
				layerWidth = (int)(rowDefinition.Width * viewport.Scale.X);
			}
			// Do not let the strip go past the end of the row definition or of the tile.
			top = (int)(top * viewport.Scale.Y);
			height = Math.Min(height, top + (int)(rowDefinition.Height * viewport.Scale.Y) - sourceY);
			height = Math.Min(height, 64 - sourceY % 64);
			if (height <= 0)
			{
				height = 1;
				return;
			}
		}
		if (height > scaledTileSize)
		{
			height = scaledTileSize - sourceY % scaledTileSize;
		}
		if (destinationY + height < viewport.Destination.Top)
		{
			return;
		}
		int scrollX = (int)(viewport.Bounds.X * viewport.Scale.X * parallax) - rowOffset;
		if (wrap)
		{
			while (scrollX < 0)
			{
				scrollX += layerWidth;
			}
		}
		if (!wrap && scrollX > layerWidth)
		{
			return;
		}
		if (wrap)
		{
			scrollX %= layerWidth;
		}
		int sourceX = scrollX;
		int destinationX = viewport.Destination.X;
		while (destinationX < viewport.Destination.Right)
		{
			int tileIndex = 0;
			if (sourceX >= 0 && sourceY >= 0 && sourceX < layerWidth && sourceY < layerHeight)
			{
				tileIndex = Tiles[sourceX / scaledTileSize, sourceY / scaledTileSize];
			}
			int tileOffsetX = -(sourceX % scaledTileSize);
			int widthLeftInTile = scaledTileSize - sourceX % scaledTileSize;
			if (widthLeftInTile != 0)
			{
				Rectanglei source = new Rectanglei(0, (int)(sourceY % scaledTileSize / viewport.Scale.Y), TileSize, (int)(height / viewport.Scale.Y));
				Rectanglei destination = new Rectanglei(destinationX + tileOffsetX, destinationY, scaledTileSize, height);
				Level.TileSet.DrawTile(renderer, tileIndex, source, destination);
				sourceX += widthLeftInTile;
				destinationX += widthLeftInTile;
				if (wrap)
				{
					sourceX %= layerWidth;
				}
				else if (sourceX > layerWidth)
				{
					break;
				}
				continue;
			}
			break;
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
