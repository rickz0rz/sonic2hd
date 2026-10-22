using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

public interface IShadowInfo
{
	Vector2i MaxShadowOffset { get; }

	Vector2i OcclusionSize { get; }
}
