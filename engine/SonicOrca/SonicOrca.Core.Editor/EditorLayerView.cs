using System.Collections.Generic;
using System.Linq;

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
	/// at its level positions. Else, or with -1, all layers show as in the game.
	/// </summary>
	/// <returns>True when the layer at <paramref name="paintIndex"/> shows alone.</returns>
	public static bool Apply(IList<LevelLayer> layers, int paintIndex)
	{
		bool alone = paintIndex >= 0 && paintIndex < layers.Count && IsShifted(layers[paintIndex]);
		for (int i = 0; i < layers.Count; i++)
		{
			layers[i].Editing = alone && i == paintIndex;
			layers[i].HiddenInEditor = alone && i != paintIndex;
		}
		return alone;
	}
}
