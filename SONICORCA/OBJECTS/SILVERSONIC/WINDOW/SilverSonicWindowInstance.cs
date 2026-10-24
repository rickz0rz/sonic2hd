using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SILVERSONIC.WINDOW;

/// <summary>
/// The window in the wall of the Silver Sonic arena, as in Sonic 2 (ObjAF, subtype $A4). It is shut until Silver
/// Sonic lands. Then the shutters open and Eggman watches: he laughs while the player is hurt, and he makes a face
/// while Silver Sonic flashes after a hit. When Silver Sonic is defeated, the shutters close.
/// </summary>
public class SilverSonicWindowInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>Ani_objAF_c.</summary>
	private readonly AnimateSpriteScript _animator = new AnimateSpriteScript(
		// 0 - the shutters open
		new byte[] { 3, 4, 3, 2, 1, 0, 0xFC, 0 },
		// 1 - the shutters close
		new byte[] { 3, 0, 1, 2, 3, 4, 0xFA, 0 },
		// 2 - Eggman watches
		new byte[] { 3, 5, 5, 0xFF },
		// 3 - Eggman laughs
		new byte[] { 3, 5, 6, 0xFF },
		// 4 - Eggman when Silver Sonic is hit
		new byte[] { 3, 7, 7, 0xFF });

	private AnimationInstance _animation;

	private State _state;

	private enum State
	{
		Shut,
		Opening,
		Watching,
		Closing
	}

	/// <summary>The Silver Sonic that added the window.</summary>
	[HideInEditor]
	public SilverSonicInstance Boss { get; set; }

	public SilverSonicWindowInstance() => DesignBounds = new Rectanglei(-16 * Scale, -12 * Scale, 32 * Scale, 24 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SilverSonicWindowType.AnimationGroupResourceKey));
		_animator.Frame = 4;
		LockLifetime = true;
		// Priority 6: behind the other objects
		Priority -= 64;
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		if (Boss == null)
		{
			return;
		}
		switch (_state)
		{
			case State.Shut:
				if (Boss.Landed)
				{
					_state = State.Opening;
				}
				break;
			case State.Opening:
				if (_animator.Step() == AnimateSpriteScript.NextRoutine)
				{
					_state = State.Watching;
				}
				break;
			case State.Watching:
				if (Boss.Defeated)
				{
					_state = State.Closing;
					_animator.Animation = 1;
					break;
				}
				ICharacter player = Level.Player.Protagonist;
				_animator.Animation = player != null && player.IsHurt ? 3 : Boss.Flashing ? 4 : 2;
				_animator.Step();
				break;
			case State.Closing:
				_animator.Step();
				// MarkObjGone: the window goes when it is far from the screen
				LockLifetime = false;
				break;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _animator.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
