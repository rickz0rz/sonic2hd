using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

/// <summary>
/// A light source that moves the shadows of objects near it.
/// </summary>
public interface ILightSource
{
	Vector2i GetShadowOffset(Vector2i position, IShadowInfo shadowInfo);
}
