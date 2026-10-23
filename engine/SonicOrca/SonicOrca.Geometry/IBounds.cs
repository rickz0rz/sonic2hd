namespace SonicOrca.Geometry;

/// <summary>
/// An object that has a rectangular area, for example an item in a <see cref="QuadTree{T}"/>.
/// </summary>
public interface IBounds
{
	Rectanglei Bounds { get; }
}
