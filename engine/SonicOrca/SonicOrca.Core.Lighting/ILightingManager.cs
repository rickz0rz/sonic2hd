using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

public interface ILightingManager
{
	void RegisterLightSource(ILightSource lightSource);

	void UnregisterLightSource(ILightSource lightSource);

	Vector2i GetShadowOffset(Vector2i forPosition, IShadowInfo shadowInfo);
}
