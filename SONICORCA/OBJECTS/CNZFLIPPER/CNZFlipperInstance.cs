using System;
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

namespace SONICORCA.OBJECTS.CNZFLIPPER;

/// <summary>
/// A flipper, as in Sonic 2 (Obj86).
/// <para>
/// An upward flipper holds a character that lands on it in a ball, and lets it roll to the tip. A jump press flips
/// it: each character on it goes up at a speed from $800 at the pivot to $1000 at the tip.
/// </para>
/// <para>
/// A side flipper is a solid post. It kicks a character that pushes it away at speed $1000, rolling.
/// </para>
/// </summary>
public class CNZFlipperInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The half width of the upward flipper in original pixels ($23, SlopedSolid).</summary>
	private const int HalfWidth = 0x23;

	/// <summary>The surface height of the upward flipper for each 2 pixels, from the left: at rest, half way, and up.</summary>
	private static readonly sbyte[][] Heights =
	{
		new sbyte[] { 7, 7, 7, 7, 7, 7, 7, 8, 9, 10, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12, -13, -14 },
		new sbyte[] { 6, 6, 6, 6, 6, 6, 7, 8, 9, 9, 9, 9, 9, 9, 8, 8, 8, 8, 8, 8, 7, 7, 7, 7, 6, 6, 6, 6, 5, 5, 4, 4, 4, 4, 4, 4 },
		new sbyte[] { 5, 5, 5, 5, 5, 6, 7, 8, 9, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18, 17, 17, 16, 16, 16, 16, 16, 16, 16, 16 },
	};

	/// <summary>The flip animation of the original: frame 1, 2, 1, each for 4 frames, then frame 0.</summary>
	private static readonly int[] FlipFrames = { 1, 2, 1 };

	private const int FlipFrameTicks = 4;

	/// <summary>The kick animations of the side flipper: frames for a kick to the left and to the right, one frame each.</summary>
	private static readonly int[] KickLeftFrames = { 5, 4, 3, 3, 3, 3 };

	private static readonly int[] KickRightFrames = { 3, 4, 5, 5, 5, 5 };

	private const int SideRestFrame = 4;

	/// <summary>The radius of a character in a ball ($E) at four times the resolution.</summary>
	private const int BallRadius = 56;

	/// <summary>The side flipper: a solid post 8 pixels to each side and 24 pixels up and down.</summary>
	private const int PostHalfWidth = 8 * Scale;

	private const int PostHalfHeight = 24 * Scale;

	/// <summary>$1000 at four times the resolution.</summary>
	private const double KickSpeed = 64.0;

	private const int KickLockTicks = 15;

	/// <summary>The frames after a character leaves the flipper during which the flipper does not take it again.</summary>
	private const int ReleaseTicks = 16;

	private readonly HashSet<ICharacter> _held = new HashSet<ICharacter>();

	private readonly Dictionary<ICharacter, int> _released = new Dictionary<ICharacter, int>();

	private AnimationInstance _animation;

	private bool _sideways;

	private bool _flipX;

	private bool _flipRequested;

	private int _flipTicks = -1;

	private int[] _kickFrames;

	private int _kickTicks = -1;

	/// <summary>A side flipper (subtype not 0) instead of an upward flipper.</summary>
	[StateVariable]
	private bool Sideways
	{
		get => _sideways;
		set => _sideways = value;
	}

	/// <summary>The upward flipper has its pivot on the right and its tip on the left.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public CNZFlipperInstance() => DesignBounds = new Rectanglei(-160, -128, 320, 256);

	private int Frame => _sideways
		? (_kickTicks >= 0 ? _kickFrames[_kickTicks] : SideRestFrame)
		: (_flipTicks >= 0 ? FlipFrames[_flipTicks / FlipFrameTicks] : 0);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZFlipperType.AnimationGroupResourceKey));
		if (_sideways)
		{
			CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-PostHalfWidth, -PostHalfHeight, PostHalfWidth * 2, PostHalfHeight * 2));
			// Touch a little outside the post, so that a character that pushes it touches it
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -PostHalfWidth - 8, -PostHalfHeight, PostHalfWidth * 2 + 16, PostHalfHeight * 2)
			};
		}
		else
		{
			CollisionVectors = CreateSurface(0);
		}
	}

	/// <summary>The surface of the upward flipper for a frame, as collision lines that a character can land on.</summary>
	private CollisionVector[] CreateSurface(int frame)
	{
		var points = Enumerable.Range(0, Heights[frame].Length)
			.Select(i => new Vector2i(GetSurfaceX(i), -Heights[frame][i] * Scale))
			.OrderBy(p => p.X)
			.ToList();
		var vectors = new List<CollisionVector>();
		for (int i = 0; i + 1 < points.Count; i++)
		{
			vectors.Add(new CollisionVector(this, points[i], points[i + 1]));
		}
		return vectors.ToArray();
	}

	/// <summary>The x of a height entry, from the centre. A flipped flipper mirrors the table.</summary>
	private int GetSurfaceX(int index)
	{
		int x = (index * 2 - HalfWidth) * Scale;
		return _flipX ? -x : x;
	}

	/// <summary>The surface y under a position, from the centre.</summary>
	private int GetSurfaceY(int frame, int x)
	{
		int dx = (_flipX ? -x : x) / Scale;
		int index = Math.Clamp((dx + HalfWidth) / 2, 0, Heights[frame].Length - 1);
		return -Heights[frame][index] * Scale;
	}

	protected override void OnUpdatePrepare()
	{
		// The character clears its input when an object controls it, so read the jump press before it updates.
		// Only the main player can flip, as in the original, where Tails reads the second pad.
		ICharacter player = Level.Player.Protagonist;
		if (_held.Contains(player) && player is Character character && character.Input.ABC == CharacterInputButtonState.Pressed)
		{
			_flipRequested = true;
		}
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in _released.Keys.ToArray())
		{
			if (--_released[character] <= 0)
			{
				_released.Remove(character);
			}
		}
		if (_sideways)
		{
			if (_kickTicks >= 0 && ++_kickTicks >= _kickFrames.Length)
			{
				_kickTicks = -1;
			}
			return;
		}
		if (_flipTicks >= 0 && ++_flipTicks >= FlipFrames.Length * FlipFrameTicks)
		{
			_flipTicks = -1;
			CollisionVectors = CreateSurface(0);
			RegisterCollisionUpdate();
		}
		if (_flipRequested)
		{
			_flipRequested = false;
			Flip();
		}
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (_held.Contains(character))
			{
				Hold(character);
			}
			else if (character.ObjectLink == this && !_released.ContainsKey(character) && !character.IsDebug)
			{
				_held.Add(character);
				Hold(character);
			}
		}
	}

	/// <summary>Keeps a character on the flipper in a ball, and rolls it one pixel each frame toward the low end.</summary>
	private void Hold(ICharacter character)
	{
		if (character.IsDebug || character.IsDying)
		{
			Release(character);
			return;
		}
		int frame = Frame;
		// At rest the surface goes down to the tip, half way it is flat, and up it goes down to the pivot
		int slide = (1 - frame) * Scale * (_flipX ? -1 : 1);
		int x = character.Position.X - Position.X + slide;
		if (x < -HalfWidth * Scale || x >= HalfWidth * Scale)
		{
			Release(character);
			return;
		}
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.Facing = slide < 0 ? -1 : 1;
		character.Position = new Vector2i(Position.X + x, Position.Y + GetSurfaceY(frame, x) - BallRadius);
		character.Velocity = new Vector2(slide, 0.0);
	}

	private void Release(ICharacter character)
	{
		_held.Remove(character);
		_released[character] = ReleaseTicks;
		character.IsObjectControlled = false;
		character.LeaveGround();
		character.IsAirborne = true;
	}

	/// <summary>Sends each character on the flipper up, faster nearer the tip.</summary>
	private void Flip()
	{
		foreach (ICharacter character in _held.ToArray())
		{
			int along = (character.Position.X - Position.X) / Scale;
			if (_flipX)
			{
				along = -along;
			}
			along += HalfWidth;
			// The speed and the angle in the units of the original: $800 to $1000, and $40 (up) to $51
			double speed = (0x800 + Math.Min(along, 0x40) * 32) / 256.0 * Scale;
			double angle = ((along >> 2) + 0x40) * Math.PI / 128.0;
			double velocityX = Math.Cos(angle) * -speed;
			Release(character);
			character.Velocity = new Vector2(_flipX ? -velocityX : velocityX, Math.Sin(angle) * -speed);
		}
		_flipTicks = 0;
		CollisionVectors = CreateSurface(1);
		RegisterCollisionUpdate();
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZFlipperType.SoundResourceKey));
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (!_sideways || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		var character = (Character)e.ActiveObject;
		bool left = character.Position.X < Position.X;
		// Only a character on the ground that runs into the post or pushes it
		if (character.IsAirborne || !(character.IsPushing || (left ? character.GroundVelocity > 0.0 : character.GroundVelocity < 0.0)))
		{
			return;
		}
		double velocity = left ? -KickSpeed : KickSpeed;
		character.GroundVelocity = velocity;
		character.Velocity = new Vector2(velocity, character.Velocity.Y);
		character.Facing = left ? -1 : 1;
		character.SlopeLockTicks = Math.Max(KickLockTicks, character.SlopeLockTicks);
		character.IsSpinball = true;
		character.IsPushing = false;
		_kickFrames = left ? KickLeftFrames : KickRightFrames;
		_kickTicks = 0;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZFlipperType.SoundResourceKey));
	}

	protected override void OnStop()
	{
		foreach (ICharacter character in _held.ToArray())
		{
			Release(character);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = Frame;
		renderer.GetObjectRenderer().Render(_animation, _flipX && !_sideways);
	}
}
