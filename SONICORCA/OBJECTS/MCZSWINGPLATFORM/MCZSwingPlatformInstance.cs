using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SONICORCA.OBJECTS.ARZSWINGPLATFORM;
using SonicOrca.Extensions;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZSWINGPLATFORM;

/// <summary>
/// A platform on a chain, as in Sonic 2 (Obj15 in Mystic Cave Zone). Subtype bits 4 to 6 select the kind:
/// <list type="bullet">
/// <item>Full ($00): the platform swings through half a circle with oscillator 6, as in Aquatic Ruin.</item>
/// <item>Left ($10): the platform hangs down while the oscillator is below $3F. Then it swings out to the left and
/// back.</item>
/// <item>Right ($30): the platform swings out to the right while the oscillator is below $41, and hangs down for the
/// other half of the cycle.</item>
/// <item>Trap ($40): a spiked log that hangs to the left. When the main player comes within $20 pixels of the pivot
/// on the x axis, it swings down, waits 60 frames, swings back, waits 60 frames, and so on. Its edge hurts.</item>
/// </list>
/// A mirrored platform swings to the other side.
/// </summary>
public class MCZSwingPlatformInstance : ARZSwingPlatformInstance
{
	private const int TrapCollisionId = 1;

	/// <summary>The trap waits this number of frames at each end of its swing.</summary>
	private const int TrapPauseTicks = 60;

	/// <summary>The change of the swing speed of the trap each frame, in 1/256 of an angle step.</summary>
	private const int TrapAcceleration = 8;

	/// <summary>The trap stops when its swing speed is this value.</summary>
	private const int TrapTopSpeed = 0x200;

	private AnimationInstance _trapAnimation;

	private SwingKind _kind;

	private bool _trapTriggered;

	private bool _trapReturning;

	/// <summary>The angle of the trap, with 8 bits of fraction ($8000 is straight to the left).</summary>
	private int _trapAngle = 0x8000;

	private int _trapSpeed;

	private int _trapPause;

	public enum SwingKind
	{
		Full,
		Left,
		Right,
		Trap
	}

	/// <summary>The kind of swing (subtype bits 4 to 6).</summary>
	[StateVariable]
	private SwingKind Kind
	{
		get => _kind;
		set => _kind = value;
	}

	/// <summary>The platform of Mystic Cave is $18 pixels from the centre to each side.</summary>
	protected override int HalfWidth => 0x18 * Scale;

	protected override void OnStart()
	{
		base.OnStart();
		if (_kind == SwingKind.Trap)
		{
			_trapAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZSwingPlatformType.TrapAnimationGroupResourceKey));
			// Touch size $27: $20 pixels to each side and 2 pixels up and down
			CollisionRectangles = new[] { new CollisionRectangle(this, TrapCollisionId, -0x20 * Scale, -2 * Scale, 0x40 * Scale, 4 * Scale) };
		}
	}

	protected override int GetSwing(int oscillator)
	{
		switch (_kind)
		{
			case SwingKind.Left:
				// The original plays the platform knock sound when the platform comes back down
				return oscillator > 0x3F ? oscillator : 0x40;
			case SwingKind.Right:
				return oscillator < 0x41 ? oscillator : 0x40;
			case SwingKind.Trap:
				UpdateTrap();
				return (_trapAngle >> 8) & 0xFF;
			default:
				return oscillator;
		}
	}

	/// <summary>Swings the trap down and back with a speed that grows by 8 each frame up to $200.</summary>
	private void UpdateTrap()
	{
		if (_trapPause > 0)
		{
			_trapPause--;
			return;
		}
		if (!_trapTriggered)
		{
			ICharacter player = Level.Player.Protagonist;
			if (player == null || (uint)((int)(player.Position.X - Pivot.X) / Scale + 0x20) >= 0x40)
			{
				return;
			}
			_trapTriggered = true;
		}
		if (_trapReturning)
		{
			_trapSpeed += TrapAcceleration;
			_trapAngle += _trapSpeed;
			if (_trapSpeed == TrapTopSpeed)
			{
				_trapSpeed = 0;
				_trapAngle = 0x8000;
				_trapReturning = false;
				_trapPause = TrapPauseTicks;
			}
		}
		else
		{
			_trapSpeed -= TrapAcceleration;
			_trapAngle += _trapSpeed;
			if (_trapSpeed == -TrapTopSpeed)
			{
				_trapSpeed = 0;
				_trapAngle = 0x4000;
				_trapReturning = true;
				_trapPause = TrapPauseTicks;
			}
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		base.OnCollision(e);
		if (_kind != SwingKind.Trap || e.Id != TrapCollisionId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		character.Hurt(Math.Sign(character.Position.X - Position.X));
	}

	protected override void RenderPlatform(IObjectRenderer objectRenderer, AnimationInstance animation)
	{
		if (_kind != SwingKind.Trap)
		{
			base.RenderPlatform(objectRenderer, animation);
			return;
		}
		_trapAnimation.Index = 0;
		objectRenderer.Render(_trapAnimation);
	}
}
