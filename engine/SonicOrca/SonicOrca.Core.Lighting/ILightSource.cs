using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

public interface ILightSource
{
	Vector2i GetShadowOffset(Vector2i position, IShadowInfo shadowInfo);
}
