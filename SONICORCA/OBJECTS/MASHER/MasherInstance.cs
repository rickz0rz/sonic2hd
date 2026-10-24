using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MASHER;

/// <summary>
/// Masher, a piranha badnik that jumps up from its start position and falls back again and again.
/// </summary>
public class MasherInstance : Badnik
{
	/// <summary>
	/// The speed of the first jump. The original sets y_vel to -$400. The old value of -1.5625 was 400 / 256, which read
	/// the hexadecimal value as decimal.
	/// </summary>
	private const double InitialVelocity = -16.0;
	private const double LaunchVelocity = -20.0;
	private const double Acceleration = 0.375;
	private const int AnimationNormal = 0;
	private const int AnimationMunching = 1;
	private AnimationInstance _animation;
	private double _initialY;
	private double _velocityY;

	public MasherInstance() => DesignBounds = new Rectanglei(-51, -76, 102, 152);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MasherType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -48, -64, 96, 128)
		};
		_initialY = Position.Y;
		_velocityY = InitialVelocity;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		PositionPrecise = PositionPrecise + new Vector2(0.0, _velocityY);
		_velocityY += Acceleration;
		if (Position.Y <= _initialY)
			return;

		// Masher fell below its start position: put it back and jump again.
		PositionPrecise = new Vector2(Position.X, _initialY);
		_velocityY = LaunchVelocity;
	}

	protected override void OnAnimate()
	{
		_animation.Index = _velocityY >= 0.0 ? AnimationNormal : AnimationMunching;
		_animation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
