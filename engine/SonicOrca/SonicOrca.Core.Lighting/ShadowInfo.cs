using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

/// <summary>
/// The shadow properties of an object that casts a shadow.
/// </summary>
public class ShadowInfo : IShadowInfo
{
	public Vector2i MaxShadowOffset { get; set; }

	public Vector2i OcclusionSize { get; set; }
}
