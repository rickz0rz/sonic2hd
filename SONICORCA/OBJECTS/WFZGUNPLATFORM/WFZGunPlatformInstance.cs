using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZGUNPLATFORM;

/// <summary>
/// The lateral cannon, as in Sonic 2 (ObjBE): a platform that comes out of the wall and goes back. It waits until
/// bits 4-7 of the frame counter equal its time (subtype bits 4-7), so it opens once in each 256 frames. It opens in
/// 24 frames and stays open for $9F frames. While it is open, a character can stand on its top, 25 pixels above the
/// centre. Then the characters on it fall, and it closes in 24 frames.
/// </summary>
public class WFZGunPlatformInstance : ActiveObject
{
	private const int Scale = 4;

	private const int OpenTicks = 0xA0;

	private const int HalfWidth = 0x20 * Scale;

	/// <summary>PlatformObject with height $19.</summary>
	private const int TopHeight = 0x19 * Scale;

	private const int OpenScript = 0;

	private const int CloseScript = 1;

	private readonly SpriteAnimator _animator = new SpriteAnimator(
		new byte[] { 5, 0, 1, 2, 3, 0xFC },
		new byte[] { 5, 3, 2, 1, 0, 0xFC });

	private AnimationInstance _animation;

	private int _time;

	private State _state;

	private int _timer;

	private enum State
	{
		Wait,
		Open,
		Out,
		Close
	}

	/// <summary>The time to open (subtype bits 4-7, as a multiple of 16 frames).</summary>
	[StateVariable]
	private int Time
	{
		get => _time;
		set => _time = value;
	}

	public WFZGunPlatformInstance() => DesignBounds = new Rectanglei(-HalfWidth, -0x18 * Scale, HalfWidth * 2, 0x18 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZGunPlatformType.AnimationGroupResourceKey));
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Wait:
				if ((Level.Ticks & 0xF0) == _time)
				{
					_state = State.Open;
					_animator.Animation = OpenScript;
					_timer = OpenTicks;
				}
				break;
			case State.Open:
				if (_animator.Step() == SpriteAnimator.Result.NextRoutine)
				{
					_state = State.Out;
				}
				break;
			case State.Out:
				if (--_timer == 0)
				{
					_state = State.Close;
					_animator.Animation = CloseScript;
					SetSolid(false);
					break;
				}
				// Only the open frames (3 and 4) carry the characters
				SetSolid(_animator.Frame == 3 || _animator.Frame == 4);
				break;
			case State.Close:
				if (_animator.Step() == SpriteAnimator.Result.NextRoutine)
				{
					_state = State.Wait;
				}
				break;
		}
	}

	/// <summary>Makes the top solid, or drops the characters on it (loc_3B7BC) and makes it not solid.</summary>
	private void SetSolid(bool solid)
	{
		if (solid == CollisionVectors.Length > 0)
		{
			return;
		}
		if (solid)
		{
			CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
		}
		else
		{
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				if (character.ObjectLink == this)
				{
					character.LeaveGround();
					character.IsAirborne = true;
				}
			}
			CollisionVectors = new CollisionVector[0];
		}
		RegisterCollisionUpdate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _animator.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
