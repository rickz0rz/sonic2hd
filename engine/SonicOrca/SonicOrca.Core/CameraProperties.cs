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

	/// <summary>
	/// The X position that the camera follows instead of the position of the object, or null for the position of the
	/// object. A character uses it for the camera lag after a spindash.
	/// </summary>
	public double? TrackX { get; set; }
}
