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

namespace SONICORCA.OBJECTS.MTZPLATFORM;

/// <summary>
/// A platform that moves one step when a character walks off it, as in Sonic 2 (Obj6A in Metropolis Zone). The
/// platform is solid. When a character that stood on it leaves it, the platform makes the next move of a list and
/// stops: down $40 pixels, then right $80 and up $40, then down $40, then left $80 and up $40. The subtype is the
/// first move, as a byte offset in the list (6 bytes for each move).
/// </summary>
public class MTZPlatformInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The solid box: width_pixels ($20) + $B to each side, $C up and down.</summary>
	private const int HalfWidth = 0x20 + 0xB;

	private const int HalfHeight = 0xC;

	/// <summary>The art of the platform (frame 1 of the long platform mappings).</summary>
	private const int Frame = 1;

	/// <summary>The moves of the platform (byte_27CDC): x speed, y speed (1/256 pixels each frame) and frames.</summary>
	private static readonly int[,] Moves =
	{
		{ 0, 0x400, 0x10 },
		{ 0x400, -0x200, 0x20 },
		{ 0, 0x400, 0x10 },
		{ -0x400, -0x200, 0x20 },
	};

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private int _nextMove;

	private Vector2 _velocity;

	private int _moveTicks;

	private bool _moving;

	private HashSet<ICharacter> _standing = new HashSet<ICharacter>();

	/// <summary>The first move, as a byte offset in the list (6 bytes for each move).</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The flip of the original. It flips only the art.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MTZPlatformInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0x60 * Scale, 0x100 * Scale, 0xC0 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZPlatformType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		_nextMove = (_subtype / 6) % Moves.GetLength(0);
		LoadMove();
	}

	/// <summary>Loads the next move and stops (loc_27CA2).</summary>
	private void LoadMove()
	{
		_velocity = new Vector2(Moves[_nextMove, 0] * Scale / 256.0, Moves[_nextMove, 1] * Scale / 256.0);
		_moveTicks = Moves[_nextMove, 2];
		_moving = false;
		_nextMove = (_nextMove + 1) % Moves.GetLength(0);
	}

	protected override void UpdateMovement()
	{
		if (_moving)
		{
			PositionPrecise += _velocity;
			if (--_moveTicks == 0)
			{
				LoadMove();
			}
			return;
		}
		// The platform moves when a character that stood on it is no longer on it (loc_27BDE)
		HashSet<ICharacter> standing = Level.ObjectManager.Characters.Where(c => c.ObjectLink == this).ToHashSet();
		if (_standing.Any(c => !standing.Contains(c)))
		{
			_moving = true;
			_standing.Clear();
			return;
		}
		if (standing.Count > 0)
		{
			_standing = standing;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
