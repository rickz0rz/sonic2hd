using SonicOrca.Geometry;

namespace SonicOrca.Core;

public class CameraProperties
{
	public Rectangle Box { get; set; }

	public Vector2i Delay { get; set; }

	public Vector2 MaxVelocity { get; set; }

	public Vector2 Offset { get; set; }
}
