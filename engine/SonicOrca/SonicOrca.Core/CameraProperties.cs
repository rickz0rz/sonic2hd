using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// The settings that an object gives to the camera when the camera tracks it.
/// </summary>
public class CameraProperties
{
	public Rectangle Box { get; set; }

	public Vector2i Delay { get; set; }

	public Vector2 MaxVelocity { get; set; }

	public Vector2 Offset { get; set; }
}
