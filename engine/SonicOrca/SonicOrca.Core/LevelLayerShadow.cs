using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// The settings of the shadow that a level layer casts.
/// </summary>
public class LevelLayerShadow
{
	public static readonly Colour DefaultShadowColour = new Colour(128, 0, 0, 0);

	public bool Tiles { get; set; }

	public bool Objects { get; set; }

	public int LayerIndexOffset { get; set; }

	public Vector2i Displacement { get; set; }

	public int Softness { get; set; }

	public Colour Colour { get; set; }

	public LevelLayerShadow()
	{
		Tiles = true;
		Objects = true;
		Colour = DefaultShadowColour;
	}

	public override string ToString()
	{
		return $"[{(Tiles ? 'T' : ' ')}{(Objects ? 'O' : ' ')}] {LayerIndexOffset:+#;-#;0} {Displacement.X:+#;-#;0},{Displacement.Y:+#;-#;0} {Colour.ToHexString()} @{Softness}";
	}
}
