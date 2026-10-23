using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

/// <summary>
/// Keeps the light sources of a level and gives the shadow offset for a position.
/// </summary>
public interface ILightingManager
{
	void RegisterLightSource(ILightSource lightSource);

	void UnregisterLightSource(ILightSource lightSource);

	Vector2i GetShadowOffset(Vector2i forPosition, IShadowInfo shadowInfo);
}
