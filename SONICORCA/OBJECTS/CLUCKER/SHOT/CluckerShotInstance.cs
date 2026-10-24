using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CLUCKER.SHOT;

/// <summary>
/// The shot of a Clucker, as in Sonic 2 (Obj98 with Obj98_CluckerShotMove). It flies straight at the speed that the
/// Clucker gives and turns through mapping frames $D to $14, 4 frames each. It hurts a character that it touches
/// (touch size $18: 4 by 4 pixels) and goes when it leaves the screen.
/// </summary>
public class CluckerShotInstance : Enemy
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int TouchRadius = 4 * Scale;

	private const int FirstFrame = 0xD;

	private const int FrameCount = 8;

	private const int FrameTicks = 4;

	private AnimationInstance _animation;

	private int _velocityX;

	private int _ticks;

	/// <summary>The speed on the x axis, in 1/256 original pixels for each frame.</summary>
	[StateVariable]
	private int VelocityX
	{
		get => _velocityX;
		set => _velocityX = value;
	}

	public CluckerShotInstance() => DesignBounds = new Rectanglei(-8 * Scale, -8 * Scale, 0x10 * Scale, 0x10 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CluckerShotType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
		};
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		if (!Level.Camera.Bounds.IntersectsWith(new Rectangle(Position.X - TouchRadius, Position.Y - TouchRadius, TouchRadius * 2, TouchRadius * 2)))
		{
			FinishForever();
			return;
		}
		PositionPrecise += new Vector2(_velocityX * Unit, 0.0);
		_ticks++;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = FirstFrame + (_ticks / FrameTicks) % FrameCount;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
