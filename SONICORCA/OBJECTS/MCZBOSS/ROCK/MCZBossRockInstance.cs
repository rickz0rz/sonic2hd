using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZBOSS.ROCK;

/// <summary>
/// A stone or a spike that falls from the ceiling of the Mystic Cave Zone boss arena (Obj57, subtype 4). It falls
/// from rest with a gravity of $10 ($38 from ObjectMoveAndFall less $28) and goes when it is below y $6F0. A stone
/// (mapping frame $D) is harmless. A spike (mapping frame $14, collision $B1) hurts a character.
/// </summary>
public class MCZBossRockInstance : Enemy
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int Gravity = 0x38 - 0x28;

	/// <summary>The rock goes when it is below this y (original level pixels).</summary>
	private const int BottomY = 0x6F0;

	private const int StoneFrame = 0xD;

	private const int SpikeFrame = 0x14;

	/// <summary>Touch size $31: 2 by 8 pixels from the centre.</summary>
	private const int SpikeHalfWidth = 2 * Scale;

	private const int SpikeHalfHeight = 8 * Scale;

	private AnimationInstance _animation;

	private double _velocityY;

	/// <summary>The rock is a spike that hurts, not a stone.</summary>
	[StateVariable]
	public bool Spike { get; set; }

	/// <summary>The level y of the original level origin, in HD units. The boss gives it.</summary>
	[StateVariable]
	public int OriginY { get; set; }

	public MCZBossRockInstance() => DesignBounds = new Rectanglei(-32, -64, 64, 128);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZBossRockType.AnimationGroupResourceKey));
		_animation.Index = Spike ? SpikeFrame : StoneFrame;
		LockLifetime = true;
		if (Spike)
		{
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -SpikeHalfWidth, -SpikeHalfHeight, SpikeHalfWidth * 2, SpikeHalfHeight * 2)
			};
		}
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		PositionPrecise += new Vector2(0.0, _velocityY * Unit);
		_velocityY += Gravity;
		if ((PositionPrecise.Y - OriginY) / Scale > BottomY)
		{
			FinishForever();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
