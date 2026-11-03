using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Shows a background layer so that the tiles tool can paint on it. A background layer scrolls with parallax, rows
/// and wrap, so its tiles are not at their level positions. The tiles tool paints at level positions. Thus the editor
/// draws the layer of the tiles tool without these effects, from the top left of the level, and hides the other
/// layers.
/// </summary>
public static class EditorLayerView
{
	/// <summary>True when the game draws the layer at other positions than its level positions.</summary>
	public static bool IsShifted(LevelLayer layer)
	{
		return layer.ParallaxY != 1.0 || layer.AutomaticYParallax || layer.OffsetY != 0 || layer.WrapX || layer.WrapY ||
			layer.LayerRowDefinitions.Any(row => row.Parallax != 1.0 || row.CurrentOffset != 0.0 || row.Width != 0);
	}

	/// <summary>
	/// Sets the view of the layers. When the layer at <paramref name="paintIndex"/> is shifted, the layer shows alone,
	/// at its level positions. Else, or with -1, all layers show as in the game, without the layers that the user hid
	/// with their eye in the editor.
	/// </summary>
	/// <param name="hidden">The layers that the user hid in the editor, or null.</param>
	/// <returns>True when the layer at <paramref name="paintIndex"/> shows alone.</returns>
	public static bool Apply(IList<LevelLayer> layers, int paintIndex, ICollection<LevelLayer> hidden = null)
	{
		bool alone = paintIndex >= 0 && paintIndex < layers.Count && IsShifted(layers[paintIndex]);
		for (int i = 0; i < layers.Count; i++)
		{
			layers[i].Editing = alone && i == paintIndex;
			layers[i].HiddenInEditor = alone ? i != paintIndex : hidden?.Contains(layers[i]) == true;
		}
		return alone;
	}

	/// <summary>
	/// Gives the point of a layer that the game draws at a level position, for a view. The game moves a layer by its
	/// Y parallax and its Y offset, and each row definition by its X parallax and its offset, as RenderingTiler does.
	/// Returns false when the game draws no part of the layer there.
	/// </summary>
	/// <param name="view">The level area that the camera shows.</param>
	/// <param name="world">The level position.</param>
	/// <param name="x">The X position in the layer, in pixels.</param>
	/// <param name="y">The Y position in the layer, in pixels.</param>
	public static bool TryGetLayerPoint(LevelLayer layer, Rectangle view, Vector2 world, out int x, out int y)
	{
		const int TileSize = 64;
		x = 0;
		y = (int)(view.Y * layer.ParallaxY) + (int)Math.Floor(world.Y - view.Y) - layer.OffsetY;
		int height = layer.Rows * TileSize;
		int width = layer.Columns * TileSize;
		if (height == 0 || width == 0)
		{
			return false;
		}
		if (layer.WrapY)
		{
			y = (y % height + height) % height;
		}
		else if (y < 0 || y >= height)
		{
			return false;
		}
		double parallax = layer.ParallaxY;
		int rowOffset = 0;
		LayerRowDefinition row = layer.GetRowDefinitionAt(y, out _);
		if (row != null)
		{
			parallax = row.Parallax;
			rowOffset = (int)row.CurrentOffset;
			if (row.Width != 0 && row.Width < width)
			{
				width = row.Width;
			}
		}
		x = (int)(view.X * parallax) - rowOffset + (int)Math.Floor(world.X - view.X);
		if (layer.WrapX)
		{
			x = (x % width + width) % width;
		}
		else if (x < 0 || x >= width)
		{
			return false;
		}
		return true;
	}
}
