using SonicOrca.Geometry;

namespace SonicOrca.Core.Collision;

/// <summary>
/// The result of a collision with a collision vector: the vector, the touch point, the shift and the angle.
/// </summary>
public class CollisionInfo
{
	private readonly CollisionVector _vector;

	private readonly Vector2 _touch;

	private readonly double _shift;

	private readonly double _angle;

	public CollisionVector Vector => _vector;

	public Vector2 Touch => _touch;

	public double Shift => _shift;

	public double Angle => _angle;

	public CollisionInfo(CollisionVector vector, Vector2 touch, double shift, double angle)
	{
		_vector = vector;
		_touch = touch;
		_shift = shift;
		_angle = angle;
	}
}
