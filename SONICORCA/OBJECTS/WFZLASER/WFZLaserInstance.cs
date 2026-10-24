using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZLASER;

/// <summary>
/// The large horizontal laser that shoots past the Tornado at the start of Wing Fortress Zone, as in Sonic 2 (ObjB9).
/// When it comes on screen, it flies to the left at $1000 (16 pixels each frame). It goes for ever when it is more
/// than $40 pixels to the left of the screen. It does not hurt (collision_flags 0). The original plays the large laser
/// sound when it starts, which SonicOrca does not have.
/// </summary>
public class WFZLaserInstance : ActiveObject
{
	private const int Scale = 4;

	private const double Speed = -0x1000 * Scale / 256.0;

	/// <summary>width_pixels $60, and the sprite is 16 pixels high.</summary>
	private const int HalfWidth = 0x60 * Scale;

	private const int HalfHeight = 8 * Scale;

	private const int DeleteMargin = 0x40 * Scale;

	private AnimationInstance _animation;

	private bool _moving;

	public WFZLaserInstance() => DesignBounds = new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZLaserType.AnimationGroupResourceKey));
	}

	protected override void OnUpdate()
	{
		Rectangle camera = Level.Camera.Bounds;
		if (_moving)
		{
			PositionPrecise += new Vector2(Speed, 0.0);
		}
		else if (camera.IntersectsWith(new Rectangle(Position.X - HalfWidth, Position.Y - HalfHeight, HalfWidth * 2, HalfHeight * 2)))
		{
			_moving = true;
			// The laser must not stop when it leaves the area of the objects, only when it is off the screen
			LockLifetime = true;
		}
		if (Position.X < camera.X - DeleteMargin)
		{
			FinishForever();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = 0;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
