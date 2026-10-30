using System;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing.LevelRendering;

/// <summary>
/// Finds the tiles of a level layer that are in the viewport and draws them.
/// </summary>
internal class RenderingTiler
{
	private const int TileSize = 64;

	// The low bits of a tile value hold the tile index. The high bits hold flags.
	private const int TileIndexMask = 0xFFF;

	/// <summary>The dark overlay on the copies of a wrapped layer in the editor view.</summary>
	private static readonly Colour RepeatedOverlay = new Colour(0.55, 0.0, 0.0, 0.0);

	private readonly Level _level;

	private readonly TileSet _tileSet;

	private readonly RenderingTileList _renderingTileList;

	public RenderingTiler(Level level, RenderingTileList renderingTileList)
	{
		_level = level;
		_tileSet = level.TileSet;
		_renderingTileList = renderingTileList;
	}

	public void PrepareTiles(RenderingLayer renderingLayer, Viewport viewport, LayerViewOptions layerViewOptions)
	{
		int startIndex = _renderingTileList.Count;
		if (layerViewOptions.ShowLandscape)
		{
			PrepareTilesVertical(renderingLayer, viewport);
		}
		int addedCount = _renderingTileList.Count - startIndex;
		renderingLayer.SetRenderTileRange(startIndex, addedCount);
	}

	/// <summary>
	/// Goes down the viewport one strip at a time and adds the tiles of each strip. In the editor view of a layer, the
	/// layer is at its level positions, and the copies that the wrap makes are marked as repeated.
	/// </summary>
	private void PrepareTilesVertical(RenderingLayer renderingLayer, Viewport viewport)
	{
		LevelLayer layer = renderingLayer.Layer;
		if (layer.Rows == 0 || layer.Columns == 0)
		{
			return;
		}
		bool editing = layer.Editing;
		bool wrapY = layer.WrapY;
		int offsetY = ((!editing) ? layer.OffsetY : 0);
		double parallaxY = (editing ? 1.0 : layer.ParallaxY);
		int layerHeight = layer.Rows * TileSize;
		int startSourceY = (int)(viewport.Bounds.Y * parallaxY);
		if (!wrapY && startSourceY >= layerHeight)
		{
			return;
		}
		int sourceY = wrapY ? (startSourceY % layerHeight + layerHeight) % layerHeight : startSourceY % layerHeight;
		int destinationY = viewport.Destination.Y + offsetY;
		// The level position of the strip, for the editor view
		int levelY = startSourceY;
		while (destinationY < viewport.Destination.Bottom / viewport.Scale.Y)
		{
			int height = layerHeight - sourceY;
			bool repeatedY = editing && (levelY < 0 || levelY >= layerHeight);
			PrepareTilesHorizontal(renderingLayer, viewport, sourceY, destinationY, repeatedY, ref height);
			if (height <= 0)
			{
				break;
			}
			sourceY += height;
			destinationY += height;
			levelY += height;
			if (wrapY)
			{
				sourceY %= layerHeight;
			}
			else if (sourceY > layerHeight)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Adds the tiles of one horizontal strip. The strip height can become smaller to stop at a tile or row boundary.
	/// </summary>
	private void PrepareTilesHorizontal(RenderingLayer renderingLayer, Viewport viewport, int sourceY, int destinationY, bool repeatedY, ref int height)
	{
		LevelLayer layer = renderingLayer.Layer;
		bool editing = layer.Editing;
		int layerWidth = layer.Columns * TileSize;
		int layerHeight = layer.Rows * TileSize;
		bool wrapX = layer.WrapX;
		double parallax = (editing ? 1.0 : layer.ParallaxY);
		int rowOffset = 0;
		LayerRowDefinition rowDefinition = layer.GetRowDefinitionAt(sourceY, out int rowTop);
		if (rowDefinition != null && !editing)
		{
			parallax = rowDefinition.Parallax;
			rowOffset = (int)rowDefinition.CurrentOffset;
			if (rowDefinition.Width != 0)
			{
				int rowWidth = rowDefinition.Width;
				if (rowWidth < layerWidth)
				{
					layerWidth = rowWidth;
				}
			}
			height = Math.Min(height, rowTop + rowDefinition.Height - sourceY);
			height = Math.Min(height, TileSize - sourceY % TileSize);
			if (height <= 0)
			{
				height = 1;
				return;
			}
		}
		if (height > TileSize)
		{
			height = TileSize - sourceY % TileSize;
		}
		if (destinationY + height < viewport.Destination.Top)
		{
			return;
		}
		int startSourceX = (int)(viewport.Bounds.X * parallax) - rowOffset;
		// The level position of the tile, for the editor view
		int levelX = startSourceX;
		if (wrapX)
		{
			while (startSourceX < 0)
			{
				startSourceX += layerWidth;
			}
		}
		if (!wrapX && startSourceX > layerWidth)
		{
			return;
		}
		if (wrapX)
		{
			startSourceX %= layerWidth;
		}
		int sourceX = startSourceX;
		int destinationX = (int)(viewport.Destination.X / viewport.Scale.X);
		TileSet tileSet = _tileSet;
		int[,] tiles = layer.Tiles;
		while (destinationX < viewport.Destination.Right / viewport.Scale.X)
		{
			int tileValue = 0;
			if (sourceX >= 0 && sourceY >= 0 && sourceX < layerWidth && sourceY < layerHeight)
			{
				tileValue = tiles[sourceX / TileSize, sourceY / TileSize];
			}
			int offsetInTileX = -(sourceX % TileSize);
			int stepX = TileSize - sourceX % TileSize;
			if (stepX == 0)
			{
				break;
			}
			if (tileSet.TryGetValue(tileValue & TileIndexMask, out _))
			{
				Rectangle source = new Rectangle(0.0, sourceY % TileSize, TileSize, height);
				Rectanglei destination = new Rectangle((destinationX + offsetInTileX) * viewport.Scale.X, destinationY * viewport.Scale.Y, TileSize * viewport.Scale.X, height * viewport.Scale.Y);
				TileRenderInfo tileRenderInfo = new TileRenderInfo
				{
					Layer = layer,
					TileIndex = tileValue,
					Source = source,
					Destination = destination,
					Repeated = repeatedY || (editing && (levelX - sourceX % TileSize < 0 || levelX - sourceX % TileSize >= layerWidth))
				};
				if (!renderingLayer.IsAreaClipped(viewport, destination))
				{
					_renderingTileList.AddTileRenderInfo(tileRenderInfo);
				}
			}
			sourceX += stepX;
			destinationX += stepX;
			levelX += stepX;
			if (wrapX)
			{
				sourceX %= layerWidth;
			}
			else if (sourceX > layerWidth)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Draws a range of prepared tiles. Plain tiles are drawn in one batch for each blend mode, then the tile sequences.
	/// </summary>
	public void RenderTiles(I2dRenderer g, ITileRenderer tr, int startIndex, int count)
	{
		if (count == 0)
		{
			return;
		}
		int endIndex = startIndex + count - 1;
		for (TileBlendMode blendMode = TileBlendMode.Alpha; blendMode <= TileBlendMode.Additive; blendMode++)
		{
			tr.BeginRender();
			for (int i = startIndex; i <= endIndex; i++)
			{
				TileRenderInfo tileRenderInfo = _renderingTileList[i];
				if (_tileSet.TryGetValue(tileRenderInfo.TileIndex, out ITile tileDefinition) && tileDefinition is Tile tile && tile.Blend == blendMode)
				{
					tile.Draw(tr, tileRenderInfo.TileIndex, tileRenderInfo.Source, tileRenderInfo.Destination);
				}
			}
			tr.EndRender();
		}
		for (int i = startIndex; i <= endIndex; i++)
		{
			TileRenderInfo tileRenderInfo = _renderingTileList[i];
			if (_tileSet.TryGetValue(tileRenderInfo.TileIndex, out ITile tileDefinition) && tileDefinition is TileSequence tileSequence)
			{
				tileSequence.Draw(g, tileRenderInfo.TileIndex, tileRenderInfo.Source, tileRenderInfo.Destination);
			}
		}
		for (int i = startIndex; i <= endIndex; i++)
		{
			TileRenderInfo tileRenderInfo = _renderingTileList[i];
			if (tileRenderInfo.Repeated)
			{
				g.BlendMode = BlendMode.Alpha;
				g.RenderQuad(RepeatedOverlay, tileRenderInfo.Destination);
			}
		}
	}
}
