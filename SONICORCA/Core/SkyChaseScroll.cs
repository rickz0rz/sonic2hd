using System.Runtime.CompilerServices;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// The automatic scrolling of Sky Chase Zone (SwScrl_SCZ). The camera moves by the Tornado velocity
/// (Tornado_Velocity_X and Tornado_Velocity_Y) each frame. The Tornado and the badniks of the zone add the same
/// velocity to their own movement, so they keep their place on the screen. The area of the zone sets the velocity
/// and moves the camera. In other zones the scrolling is not active, and the velocity is zero.
/// </summary>
public sealed class SkyChaseScroll
{
	private const int Scale = 4;

	private static readonly ConditionalWeakTable<Level, SkyChaseScroll> All = new ConditionalWeakTable<Level, SkyChaseScroll>();

	public static SkyChaseScroll For(Level level) => All.GetValue(level, l => new SkyChaseScroll());

	/// <summary>True while the area moves the camera.</summary>
	public bool Active { get; private set; }

	/// <summary>The position of the act origin in the level, in level units.</summary>
	public Vector2i Origin { get; private set; }

	/// <summary>Tornado_Velocity_X: the camera movement each frame, in original pixels.</summary>
	public int VelocityX { get; set; }

	/// <summary>Tornado_Velocity_Y: the camera movement each frame, in original pixels.</summary>
	public int VelocityY { get; set; }

	/// <summary>The original camera X: the left edge of the 320 pixel screen, in original pixels from the origin.</summary>
	public int CameraX { get; set; }

	/// <summary>The original camera Y: the top edge of the 224 pixel screen, in original pixels from the origin.</summary>
	public int CameraY { get; set; }

	/// <summary>The Tornado sets this when the player has reached the end of the act.</summary>
	public bool Finished { get; set; }

	/// <summary>The Tornado velocity in level units each tick, or zero when the scrolling is not active.</summary>
	public Vector2 Velocity => Active ? new Vector2(VelocityX * Scale, VelocityY * Scale) : default;

	public void Start(Vector2i origin, int cameraX, int cameraY)
	{
		Active = true;
		Origin = origin;
		CameraX = cameraX;
		CameraY = cameraY;
		VelocityX = 0;
		VelocityY = 0;
		Finished = false;
	}

	public void Stop()
	{
		Active = false;
		VelocityX = 0;
		VelocityY = 0;
		Finished = false;
	}

	/// <summary>The level X of an original X from the act origin.</summary>
	public int ToLevelX(int x) => Origin.X + x * Scale;

	/// <summary>The level Y of an original Y from the act origin.</summary>
	public int ToLevelY(int y) => Origin.Y + y * Scale;

	/// <summary>The original X from the act origin of a level X.</summary>
	public int ToOriginalX(double x) => (int)System.Math.Floor((x - Origin.X) / Scale);

	/// <summary>The original Y from the act origin of a level Y.</summary>
	public int ToOriginalY(double y) => (int)System.Math.Floor((y - Origin.Y) / Scale);
}
