using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CLUCKER;

/// <summary>
/// Clucker, as in Sonic 2 (ObjAE). It hides in its base until the nearer player is within $80 pixels on the x axis.
/// Then it rises out of the base and pushes out its gun, and only then can a character touch it. It fires a shot at
/// $200 toward the way that it faces, and then fires again every $40 frames. The art faces left. A flipped Clucker
/// faces right.
/// </summary>
public class CluckerInstance : Badnik
{
	private const int Scale = 4;

	private const int TriggerDistance = 0x80;

	/// <summary>Touch size 6: 16 by 16 pixels from the centre.</summary>
	private const int TouchRadius = 0x10 * Scale;

	private const int HiddenFrame = 0x15;

	private const int ArmFrame = 8;

	private const int FireFrame = 0xB;

	private const int FireDelay = 0x40;

	private const int ShotSpeed = 0x200;

	private const int ShotOffsetX = 8;

	private const int ShotOffsetY = 0xB;

	private const int RiseScript = 0;

	private const int ArmScript = 1;

	private const int RecoilScript = 2;

	private readonly SpriteAnimator _animator = new SpriteAnimator(
		// Ani_objAE_a: rise out of the base
		new byte[] { 1, 0, 1, 2, 3, 4, 5, 6, 7, 0xFC },
		// Ani_objAE_b: push out the gun
		new byte[] { 1, 8, 9, 0xA, 0xB, 0xB, 0xB, 0xB, 0xFC },
		// Ani_objAE_c: the gun recoils after a shot
		new byte[] { 3, 0xA, 0xB, 0xFC });

	private AnimationInstance _animation;

	private bool _flipX;

	private State _state;

	private int _timer;

	private enum State
	{
		Wait,
		Rise,
		Arm,
		Pause,
		Recoil
	}

	/// <summary>The Clucker faces right and fires to the right.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public CluckerInstance() => DesignBounds = new Rectanglei(-0x20 * Scale, -0x14 * Scale, 0x40 * Scale, 0x38 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CluckerType.AnimationGroupResourceKey));
		_animator.Reset(HiddenFrame);
		// The Clucker cannot be touched until its gun is out (collision_flags 0)
		CollisionRectangles = new CollisionRectangle[0];
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Wait:
			{
				int deltaX = PlayerOrientation.Get(Level, Position).DeltaX;
				if (deltaX >= -TriggerDistance && deltaX < TriggerDistance)
				{
					_state = State.Rise;
					_animator.Animation = RiseScript;
				}
				break;
			}
			case State.Rise:
				if (_animator.Step() == SpriteAnimator.Result.NextRoutine)
				{
					_animator.Reset(ArmFrame);
					_animator.Animation = ArmScript;
					CollisionRectangles = new CollisionRectangle[]
					{
						new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
					};
					_state = State.Arm;
				}
				break;
			case State.Arm:
				if (_animator.Step() == SpriteAnimator.Result.NextRoutine)
				{
					_state = State.Pause;
					_timer = 0;
				}
				break;
			case State.Pause:
				if (_timer > 0)
				{
					_timer--;
					break;
				}
				_animator.Reset(FireFrame);
				_animator.Animation = RecoilScript;
				Fire();
				_state = State.Recoil;
				break;
			case State.Recoil:
				if (_animator.Step() == SpriteAnimator.Result.NextRoutine)
				{
					_state = State.Pause;
					_timer = FireDelay;
				}
				break;
		}
	}

	/// <summary>The shot starts 8 pixels in front of the centre and 11 pixels below it.</summary>
	private void Fire()
	{
		int direction = _flipX ? 1 : -1;
		var position = Position + new Vector2i(direction * ShotOffsetX * Scale, ShotOffsetY * Scale);
		Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(CluckerType.ShotResourceKey), Level.Map.Layers.IndexOf(Layer), position,
			new { VelocityX = direction * ShotSpeed }));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _animator.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
