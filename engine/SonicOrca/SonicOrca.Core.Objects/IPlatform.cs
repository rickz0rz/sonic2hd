using SonicOrca.Geometry;

namespace SonicOrca.Core.Objects;

/// <summary>
/// An object that a character can stand on and that can move the character with it.
/// </summary>
public interface IPlatform : IActiveObject
{
	Vector2 Velocity { get; }
}
