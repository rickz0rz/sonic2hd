using System;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing.LevelRendering;

internal class RenderingTiler
{
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
		int count = _renderingTileList.Count;
		if (layerViewOptions.ShowLandscape)
		{
			PrepareTilesVertical(renderingLayer, viewport);
		}
		int count2 = _renderingTileList.Count - count;
		renderingLayer.SetRenderTileRange(count, count2);
	}

	private void PrepareTilesVertical(RenderingLayer renderingLayer, Viewport viewport)
	{
		LevelLayer layer = renderingLayer.Layer;
		if (layer.Rows == 0 || layer.Columns == 0)
		{
			return;
		}
		bool editing = layer.Editing;
		bool flag = !editing && layer.WrapY;
		int num = ((!editing) ? layer.OffsetY : 0);
		double num2 = (editing ? 1.0 : layer.ParallaxY);
		int num3 = layer.Rows * 64;
		int num4 = (int)((double)viewport.Bounds.Y * num2);
		if (!flag && num4 >= num3)
		{
			return;
		}
		int num5 = num4 % num3;
		int num6 = viewport.Destination.Y + num;
		while ((double)num6 < (double)viewport.Destination.Bottom / viewport.Scale.Y)
		{
			int height = num3 - num5;
			PrepareTilesHorizontal(renderingLayer, viewport, num5, num6, ref height);
			if (height > 0)
			{
				num5 += height;
				num6 += height;
				if (flag)
				{
					num5 %= num3;
				}
				else if (num5 > num3)
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	private void PrepareTilesHorizontal(RenderingLayer renderingLayer, Viewport viewport, int sourceY, int destinationY, ref int height)
	{
		LevelLayer layer = renderingLayer.Layer;
		bool editing = layer.Editing;
		int num = 64;
		int num2 = layer.Columns * num;
		int num3 = layer.Rows * num;
		bool flag = !editing && layer.WrapX;
		double num4 = (editing ? 1.0 : layer.ParallaxY);
		int num5 = 0;
		LayerRowDefinition rowDefinitionAt = layer.GetRowDefinitionAt(sourceY, out var top);
		if (rowDefinitionAt != null && !editing)
		{
			num4 = rowDefinitionAt.Parallax;
			num5 = (int)rowDefinitionAt.CurrentOffset;
			if (rowDefinitionAt.Width != 0)
			{
				int width = rowDefinitionAt.Width;
				if (width < num2)
				{
					num2 = width;
				}
			}
			height = Math.Min(height, top + rowDefinitionAt.Height - sourceY);
			height = Math.Min(height, 64 - sourceY % 64);
			if (height <= 0)
			{
				height = 1;
				return;
			}
		}
		if (height > num)
		{
			height = num - sourceY % num;
		}
		if (destinationY + height < viewport.Destination.Top)
		{
			return;
		}
		int i = (int)((double)viewport.Bounds.X * num4) - num5;
		if (flag)
		{
			for (; i < 0; i += num2)
			{
			}
		}
		if (!flag && i > num2)
		{
			return;
		}
		if (flag)
		{
			i %= num2;
		}
		int num6 = i;
		int num7 = (int)((double)viewport.Destination.X / viewport.Scale.X);
		if (!editing)
		{
			_ = layer.ParallaxY;
		}
		TileSet tileSet = _tileSet;
		int[,] tiles = layer.Tiles;
		while ((double)num7 < (double)viewport.Destination.Right / viewport.Scale.X)
		{
			int num8 = 0;
			if (num6 >= 0 && sourceY >= 0 && num6 < num2 && sourceY < num3)
			{
				num8 = tiles[num6 / num, sourceY / num];
			}
			int num9 = -(num6 % num);
			int num10 = num - num6 % num;
			if (num10 == 0)
			{
				break;
			}
			if (tileSet.TryGetValue(num8 & 0xFFF, out var _))
			{
				Rectangle rectangle = new Rectangle(0.0, sourceY % num, 64.0, height);
				Rectanglei rectanglei = new Rectangle((double)(num7 + num9) * viewport.Scale.X, (double)destinationY * viewport.Scale.Y, (double)num * viewport.Scale.X, (double)height * viewport.Scale.Y);
				TileRenderInfo tileRenderInfo = new TileRenderInfo
				{
					Layer = layer,
					TileIndex = num8,
					Source = rectangle,
					Destination = rectanglei
				};
				if (!renderingLayer.IsAreaClipped(viewport, rectanglei))
				{
					_renderingTileList.AddTileRenderInfo(tileRenderInfo);
				}
			}
			num6 += num10;
			num7 += num10;
			if (flag)
			{
				num6 %= num2;
			}
			else if (num6 > num2)
			{
				break;
			}
		}
	}

	public void RenderTiles(I2dRenderer g, ITileRenderer tr, int startIndex, int count)
	{
		if (count == 0)
		{
			return;
		}
		int num = startIndex + count - 1;
		for (int i = 1; i <= 2; i++)
		{
			TileBlendMode tileBlendMode = (TileBlendMode)i;
			tr.BeginRender();
			for (int j = startIndex; j <= num; j++)
			{
				TileRenderInfo tileRenderInfo = _renderingTileList[j];
				if (_tileSet.TryGetValue(tileRenderInfo.TileIndex, out var value) && value is Tile tile && tile.Blend == tileBlendMode)
				{
					tile.Draw(tr, tileRenderInfo.TileIndex, tileRenderInfo.Source, tileRenderInfo.Destination);
				}
			}
			tr.EndRender();
		}
		for (int k = startIndex; k <= num; k++)
		{
			TileRenderInfo tileRenderInfo = _renderingTileList[k];
			if (_tileSet.TryGetValue(tileRenderInfo.TileIndex, out var value2) && value2 is TileSequence tileSequence)
			{
				tileSequence.Draw(g, tileRenderInfo.TileIndex, tileRenderInfo.Source, tileRenderInfo.Destination);
			}
		}
	}
}
