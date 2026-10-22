using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

public class ShadowInfo : IShadowInfo
{
	public Vector2i MaxShadowOffset { get; set; }

	public Vector2i OcclusionSize { get; set; }
}
