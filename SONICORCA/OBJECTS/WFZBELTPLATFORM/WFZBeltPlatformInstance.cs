using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZBELTPLATFORM;

/// <summary>
/// The metal platforms on a belt, as in Sonic 2 (ObjBD). The placed object is not visible. It makes a platform at
/// its position at once and then every $40 frames. A platform unfolds in 12 frames, then moves up (subtype $7E) or
/// down (subtype $80) at $100 for $C7 frames, or $1C7 frames when the placed object is flipped. A character can stand
/// on its top while it moves. Then it folds in 6 frames, the characters on it fall, and it goes.
/// </summary>
public class WFZBeltPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int SpawnDelay = 0x40;

	private const int ShortTicks = 0xC7;

	private const int LongTicks = 0x1C7;

	private const double Speed = 0x100 * Scale / 256.0;

	private const int HalfWidth = 0x18 * Scale;

	/// <summary>PlatformObject with height 5: the top is 5 pixels above the centre.</summary>
	private const int TopHeight = 5 * Scale;

	private const int UnfoldScript = 0;

	private const int FoldScript = 1;

	private const int FoldedFrame = 2;

	private readonly AnimateSpriteScript _animator = new AnimateSpriteScript(
		new byte[] { 3, 2, 1, 0, 0xFA, 0 },
		new byte[] { 1, 0, 1, 2, 0xFA });

	private AnimationInstance _animation;

	private bool _spawned;

	private bool _down;

	private bool _long;

	private int _secondary;

	private int _timer = 1;

	private bool _done;

	/// <summary>The object is a platform, not the placed object that makes the platforms.</summary>
	[StateVariable]
	private bool Spawned
	{
		get => _spawned;
		set => _spawned = value;
	}

	/// <summary>The platforms move down (subtype $80) instead of up (subtype $7E).</summary>
	[StateVariable]
	private bool Down
	{
		get => _down;
		set => _down = value;
	}

	/// <summary>The platforms move for $1C7 frames instead of $C7 frames (the placed object is flipped).</summary>
	[StateVariable]
	private bool Long
	{
		get => _long;
		set => _long = value;
	}

	public WFZBeltPlatformInstance() => DesignBounds = new Rectanglei(-HalfWidth, -8 * Scale, HalfWidth * 2, 0x10 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZBeltPlatformType.AnimationGroupResourceKey));
		if (_spawned)
		{
			_animator.Frame = FoldedFrame;
			_timer = _long ? LongTicks : ShortTicks;
			_animator.Animation = UnfoldScript;
		}
	}

	protected override void UpdateMovement()
	{
		if (!_spawned)
		{
			return;
		}
		switch (_secondary)
		{
			case 0:
				if (_animator.Step() == AnimateSpriteScript.NextSecondaryRoutine)
				{
					_secondary = 1;
					CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
					RegisterCollisionUpdate();
				}
				break;
			case 1:
				if (--_timer < 0)
				{
					_secondary = 2;
					_animator.Animation = FoldScript;
					break;
				}
				PositionPrecise += new Vector2(0.0, _down ? Speed : -Speed);
				break;
			case 2:
				if (_animator.Step() == AnimateSpriteScript.NextSecondaryRoutine)
				{
					_secondary = 3;
				}
				break;
			case 3:
				_done = true;
				break;
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (!_spawned)
		{
			if (--_timer == 0)
			{
				_timer = SpawnDelay;
				Level.ObjectManager.AddObject(new ObjectPlacement(Type.ResourceKey, Level.Map.Layers.IndexOf(Layer), Position,
					new { Spawned = true, Down = _down, Long = _long }));
			}
			return;
		}
		if (_done)
		{
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				if (character.ObjectLink == this)
				{
					character.LeaveGround();
					character.IsAirborne = true;
				}
			}
			FinishForever();
		}
	}

	protected override void OnStop()
	{
		if (_spawned)
		{
			FinishForever();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!_spawned)
		{
			return;
		}
		_animation.Index = _animator.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
