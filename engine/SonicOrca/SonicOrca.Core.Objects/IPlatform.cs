using SonicOrca.Geometry;

namespace SonicOrca.Core.Objects;

public interface IPlatform : IActiveObject
{
	Vector2 Velocity { get; }
}
