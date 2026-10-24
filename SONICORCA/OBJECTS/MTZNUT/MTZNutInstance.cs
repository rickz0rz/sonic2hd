using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZNUT;

/// <summary>
/// A nut on a screw, as in Sonic 2 (Obj69 in Metropolis Zone). The nut is solid. When a character that stands on it
/// comes to its centre, the nut holds the character at its centre and turns with the run of the character: each 8
/// pixels to the left move the nut 1 pixel down, and each 8 pixels to the right move it 1 pixel up. The nut stops
/// (subtype and $7F) * 8 pixels below its start. With subtype bit 7, the nut then falls to the floor.
/// </summary>
public class MTZNutInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The solid box: $2B pixels to each side, $C pixels up and down.</summary>
	private const int HalfWidth = 0x2B;

	private const int HalfHeight = 0xC;

	/// <summary>The nut falls with $38 gravity and checks the floor $B pixels below its centre.</summary>
	private const int Gravity = 0x38;

	private const int YRadius = 0xB;

	private readonly Dictionary<ICharacter, Rider> _riders = new Dictionary<ICharacter, Rider>();

	private AnimationInstance _animation;

	private int _subtype;

	private Vector2 _start;

	/// <summary>The bottom of the screw, in pixels below the start (objoff_36).</summary>
	private int _maxDepth;

	/// <summary>The run of the characters on the nut, in pixels (objoff_34). Negative is down.</summary>
	private int _turn;

	/// <summary>The distance below the start position, in pixels.</summary>
	private int _depth;

	private int _frame;

	private bool _falling;

	private bool _landed;

	private double _fallY;

	private int _fallVelocity;

	private enum Mode
	{
		None,
		Approach,
		Turn
	}

	private sealed class Rider
	{
		public Mode Mode;

		/// <summary>The character came from the right side.</summary>
		public bool FromRight;
	}

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public MTZNutInstance() => DesignBounds = new Rectanglei(-0x30 * Scale, -0x40 * Scale, 0x60 * Scale, 0x400 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZNutType.AnimationGroupResourceKey));
		_start = PositionPrecise;
		_maxDepth = (_subtype & 0x7F) << 3;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
	}

	protected override void UpdateMovement()
	{
		if (_landed)
		{
			return;
		}
		if (_falling)
		{
			Fall();
			return;
		}
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			UpdateCharacter(character);
			if (_falling)
			{
				break;
			}
		}
		PositionPrecise = _start + new Vector2(0.0, _depth * Scale);
	}

	/// <summary>Obj69_Action for one character.</summary>
	private void UpdateCharacter(ICharacter character)
	{
		if (!_riders.TryGetValue(character, out Rider rider))
		{
			rider = new Rider();
			_riders[character] = rider;
		}
		bool standing = character.ObjectLink == this && !character.IsDebug;
		if (!standing)
		{
			rider.Mode = Mode.None;
			return;
		}
		int dx = (int)Math.Floor((character.PositionPrecise.X - PositionPrecise.X) / Scale);
		if (rider.Mode == Mode.None)
		{
			rider.Mode = Mode.Approach;
			rider.FromRight = dx > 0;
		}
		if (rider.Mode == Mode.Approach)
		{
			// The character must come to the centre
			if ((uint)(dx + (rider.FromRight ? 0xF : 0)) < 0x10)
			{
				HoldAtCentre(character);
				rider.Mode = Mode.Turn;
			}
			return;
		}
		_turn += dx;
		HoldAtCentre(character);
		int rise = _turn >> 3;
		_frame = (rise >> 1) & 3;
		_depth = -rise;
		if (dx >= 0 || _depth < _maxDepth)
		{
			return;
		}
		// The bottom of the screw
		_depth = _maxDepth;
		_turn = -(_maxDepth << 3);
		_frame = 0;
		if ((_subtype & 0x80) != 0)
		{
			_falling = true;
			_fallY = _depth;
		}
		else
		{
			rider.Mode = Mode.None;
		}
	}

	private void HoldAtCentre(ICharacter character)
	{
		character.PositionPrecise = new Vector2(PositionPrecise.X, character.PositionPrecise.Y);
	}

	/// <summary>Falls to the floor (loc_279FC).</summary>
	private void Fall()
	{
		_fallY += _fallVelocity / 256.0;
		_fallVelocity += Gravity;
		PositionPrecise = _start + new Vector2(0.0, _fallY * Scale);
		int? floor = LandscapeFloor.GetDistance(Level, new Vector2i((int)PositionPrecise.X, (int)PositionPrecise.Y + YRadius * Scale));
		if (floor < 0)
		{
			_fallY += floor.Value;
			_fallVelocity = 0;
			_landed = true;
			PositionPrecise = _start + new Vector2(0.0, _fallY * Scale);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
