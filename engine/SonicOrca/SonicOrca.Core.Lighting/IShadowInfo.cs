using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

/// <summary>
/// The shadow properties of an object that casts a shadow.
/// </summary>
public interface IShadowInfo
{
	Vector2i MaxShadowOffset { get; }

	Vector2i OcclusionSize { get; }
}
