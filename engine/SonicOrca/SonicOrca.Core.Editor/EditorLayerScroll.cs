using System.Linq;

namespace SonicOrca.Core.Editor;

/// <summary>The settings of one row definition of a layer, for the undo history.</summary>
public readonly record struct EditorRowScroll(int Width, int Height, int InitialOffset, double Parallax, double Velocity);

/// <summary>
/// The scroll settings of a layer, for the undo history: the Y offset, the Y parallax, the wrap and the row
/// definitions.
/// </summary>
public sealed record EditorLayerScroll(int OffsetY, double ParallaxY, bool AutomaticYParallax, bool WrapX, bool WrapY, EditorRowScroll[] Rows)
{
	public static EditorLayerScroll Of(LevelLayer layer)
	{
		return new EditorLayerScroll(layer.OffsetY, layer.ParallaxY, layer.AutomaticYParallax, layer.WrapX, layer.WrapY,
			layer.LayerRowDefinitions.Select(row => new EditorRowScroll(row.Width, row.Height, row.InitialOffset, row.Parallax, row.Velocity)).ToArray());
	}

	public bool SameAs(EditorLayerScroll other)
	{
		return OffsetY == other.OffsetY && ParallaxY == other.ParallaxY && AutomaticYParallax == other.AutomaticYParallax &&
			WrapX == other.WrapX && WrapY == other.WrapY && Rows.SequenceEqual(other.Rows);
	}

	/// <summary>Puts the settings on a layer. A row keeps its current offset when the layer has a row at its index.</summary>
	public void ApplyTo(LevelLayer layer)
	{
		layer.OffsetY = OffsetY;
		layer.ParallaxY = ParallaxY;
		layer.AutomaticYParallax = AutomaticYParallax;
		layer.WrapX = WrapX;
		layer.WrapY = WrapY;
		double[] offsets = layer.LayerRowDefinitions.Select(row => row.CurrentOffset).ToArray();
		layer.LayerRowDefinitions.Clear();
		for (int i = 0; i < Rows.Length; i++)
		{
			layer.LayerRowDefinitions.Add(new LayerRowDefinition
			{
				Width = Rows[i].Width,
				Height = Rows[i].Height,
				InitialOffset = Rows[i].InitialOffset,
				Parallax = Rows[i].Parallax,
				Velocity = Rows[i].Velocity,
				CurrentOffset = i < offsets.Length ? offsets[i] : 0.0
			});
		}
	}
}
