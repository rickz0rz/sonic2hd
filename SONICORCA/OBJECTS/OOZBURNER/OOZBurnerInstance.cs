using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZBURNER;

/// <summary>
/// A burner platform, as in Sonic 2 (Obj33). The lid is solid. A green flame burns $10 pixels above the start of the
/// lid while the lid is $14 pixels or more above the flame, and the flame hurts. Subtype 0: each 121 frames the lid
/// pops up at -$96800 (16.16) with gravity $3800, and bounces at its start with a quarter of its speed until it is
/// slower than 1 pixel each frame. Other subtypes: when the characters on the lid are within $10 pixels of its
/// centre, the lid holds them and rises. At $7D pixels above its start it stops for ever and launches them up at
/// -$1000.
/// </summary>
public class OOZBurnerInstance : Platform
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	/// <summary>The speed of the lid when it pops, and its gravity, in 1/65536 pixels.</summary>
	private const long PopSpeed = -0x96800;

	private const long Gravity = 0x3800;

	private const int WaitTicks = 0x78;

	/// <summary>The lid is $18 pixels to each side and 8 pixels to the top and bottom.</summary>
	private const int HalfWidth = 0x18 * Scale;

	private const int HalfHeight = 8 * Scale;

	/// <summary>A character must stand within this distance of the centre on the x axis to start the lid.</summary>
	private const int TriggerRange = 0x10 * Scale;

	private const int TopOffset = -0x7D;

	private const int FlameOffset = -0x10;

	private const int FlameMinDistance = 0x14;

	/// <summary>Collision size $1B of the flame: 8 by 4 pixels to each side.</summary>
	private const int FlameHalfWidth = 8 * Scale;

	private const int FlameHalfHeight = 4 * Scale;

	private const int LaunchSpeed = -0x1000;

	private const int LaunchInertia = 0x800;

	private readonly AnimateSpriteScript _flameScript = new AnimateSpriteScript(new byte[] { 2, 2, 0, 2, 0, 2, 0, 1, 0xFF });

	private readonly List<ICharacter> _held = new List<ICharacter>();

	private AnimationInstance _animation;

	private AnimationInstance _flameAnimation;

	private bool _waitForPlayer;

	private Mode _mode;

	private int _timer = WaitTicks;

	private double _startY;

	/// <summary>The offset of the lid from its start, and its speed, in 1/65536 original pixels.</summary>
	private long _offset;

	private long _velocity;

	private bool _flameOn;

	private enum Mode
	{
		Wait,
		Pop,
		WaitForPlayer,
		Rise,
		Top
	}

	/// <summary>The lid waits for a character instead of popping by itself (subtype not 0).</summary>
	[StateVariable]
	private bool WaitForPlayer
	{
		get => _waitForPlayer;
		set => _waitForPlayer = value;
	}

	public OOZBurnerInstance() => DesignBounds = new Rectanglei(-128, -64, 256, 960);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZBurnerType.AnimationGroupResourceKey));
		_flameAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZBurnerType.FlameAnimationGroupResourceKey));
		_startY = PositionPrecise.Y;
		_mode = _waitForPlayer ? Mode.WaitForPlayer : Mode.Wait;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2));
	}

	protected override void UpdateMovement()
	{
		switch (_mode)
		{
			case Mode.Wait:
				if (--_timer < 0)
				{
					_timer = WaitTicks;
					_velocity = PopSpeed;
					_mode = Mode.Pop;
				}
				break;
			case Mode.Pop:
				_offset += _velocity;
				_velocity += Gravity;
				if (_offset >> 16 >= 0)
				{
					// Bounce: back to the wait when the lid is slower than 1 pixel each frame
					if (_velocity < 0x10000)
					{
						_mode = Mode.Wait;
					}
					_velocity = -(_velocity >> 2);
					_offset &= 0xFFFF;
				}
				break;
			case Mode.WaitForPlayer:
				CheckStart();
				break;
			case Mode.Rise:
				_offset += _velocity;
				_velocity += Gravity;
				if (_offset >> 16 == TopOffset)
				{
					_mode = Mode.Top;
					Launch();
				}
				break;
		}
		PositionPrecise = new Vector2(PositionPrecise.X, _startY + _offset / 65536.0 * Scale);
	}

	/// <summary>Starts the lid when each character on it is near its centre: one character, or both together.</summary>
	private void CheckStart()
	{
		ICharacter[] standing = Level.ObjectManager.Characters.Where(c => c.ObjectLink == this && !c.IsAirborne && !c.IsDebug).ToArray();
		if (standing.Length == 0 || standing.Any(c => c.Position.X < Position.X - TriggerRange || c.Position.X >= Position.X + TriggerRange))
		{
			return;
		}
		foreach (ICharacter character in standing)
		{
			character.IsObjectControlled = true;
			character.GroundVelocity = 0.0;
			character.Velocity = new Vector2();
			character.IsPushing = false;
			_held.Add(character);
		}
		_velocity = PopSpeed;
		_mode = Mode.Rise;
	}

	private void Launch()
	{
		bool launched = _held.Count > 0;
		foreach (ICharacter character in _held)
		{
			character.IsObjectControlled = false;
			character.Position = new Vector2i(Position.X, character.Position.Y);
			character.LeaveGround();
			character.IsAirborne = true;
			character.IsSpinball = true;
			character.Velocity = new Vector2(0.0, LaunchSpeed * Unit);
			character.GroundVelocity = LaunchInertia * Unit;
		}
		_held.Clear();
		if (launched)
		{
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(OOZBurnerType.SoundResourceKey));
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		// The lid holds the characters on its top while it rises
		for (int i = _held.Count - 1; i >= 0; i--)
		{
			ICharacter character = _held[i];
			if (character.IsDebug || character.IsDying || character.IsDead || character.IsHurt)
			{
				character.IsObjectControlled = false;
				_held.RemoveAt(i);
				continue;
			}
			character.Position = new Vector2i(character.Position.X, Position.Y - HalfHeight - character.CollisionRadius.Y);
			character.Velocity = new Vector2();
			character.GroundVelocity = 0.0;
		}
		UpdateFlame();
	}

	/// <summary>The flame burns while the lid is $14 pixels or more above it.</summary>
	private void UpdateFlame()
	{
		long lidY = _offset >> 16;
		_flameOn = FlameOffset - lidY >= FlameMinDistance;
		if (!_flameOn)
		{
			_flameScript.Rewind();
			CollisionRectangles = new CollisionRectangle[0];
			return;
		}
		_flameScript.Step();
		int flameY = (int)(FlameOffset * Scale - _offset / 65536.0 * Scale);
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 1, -FlameHalfWidth, flameY - FlameHalfHeight, FlameHalfWidth * 2, FlameHalfHeight * 2)
		};
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id == 1 && e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			var character = (ICharacter)e.ActiveObject;
			character.Hurt(System.Math.Sign(character.Position.X - Position.X));
		}
	}

	protected override void OnStop()
	{
		foreach (ICharacter character in _held)
		{
			character.IsObjectControlled = false;
		}
		_held.Clear();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (_flameOn)
		{
			_flameAnimation.Index = _flameScript.Frame;
			objectRenderer.Render(_flameAnimation, new Vector2(0.0, _startY + FlameOffset * Scale - PositionPrecise.Y));
		}
		_animation.Index = 0;
		objectRenderer.Render(_animation);
	}
}
