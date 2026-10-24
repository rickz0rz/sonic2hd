using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZPLATFORM;

/// <summary>
/// A large wooden crate, as in Sonic 2 (Obj6A in Mystic Cave Zone). The crate is solid and moves at one pixel each
/// frame through a list of moves: down $40, left $80, up $40 and right $80, so it goes around a rectangle. The
/// subtype is the first move. With subtype $18, the object makes three crates on the same rectangle: this crate
/// first goes right $40 to the corner, and two more crates start $40 down and $40 to each side. The flip of the
/// original mirrors the moves.
/// </summary>
public class MCZPlatformInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The crate is $20 pixels from its centre to each side.</summary>
	private const int HalfSize = 0x20 * Scale;

	/// <summary>The subtype that makes the group of three crates.</summary>
	private const int GroupSubtype = 0x18;

	/// <summary>The moves of the crate (byte_27CF4): x speed, y speed (1/256 pixels each frame) and frames.</summary>
	private static readonly int[,] Moves =
	{
		{ 0, 0x100, 0x40 },
		{ -0x100, 0, 0x80 },
		{ 0, -0x100, 0x40 },
		{ 0x100, 0, 0x80 },
		{ 0x100, 0, 0x40 },
	};

	/// <summary>After a move, the next move comes from the first four only.</summary>
	private const int LoopMoves = 4;

	private AnimationInstance _animation;

	private int _subtype = GroupSubtype;

	private bool _flipX;

	private int _move;

	private Vector2 _velocity;

	private int _moveTicks;

	private MCZPlatformInstance _right;

	private MCZPlatformInstance _left;

	/// <summary>The first move, as a byte offset in the list (6 bytes for each move).</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The moves are mirrored (the flip of the original).</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MCZPlatformInstance() => DesignBounds = new Rectanglei(-HalfSize - 0x80 * Scale, -HalfSize, (HalfSize + 0x80 * Scale) * 2, HalfSize * 2 + 0x80 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZPlatformType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfSize, -HalfSize, HalfSize * 2, HalfSize * 2));
		if (IsSubObject)
		{
			// The crate that makes this one calls Setup
			return;
		}
		if (_subtype == GroupSubtype)
		{
			// The crate $40 to the right goes left first, and the crate $40 to the left goes up first
			_right = AddCrate(0x40, _flipX ? 0xC : 6);
			_left = AddCrate(-0x40, _flipX ? 6 : 0xC);
		}
		Setup(_subtype, _flipX);
	}

	private MCZPlatformInstance AddCrate(int offsetX, int subtype)
	{
		MCZPlatformInstance crate = Level.ObjectManager.AddSubObject<MCZPlatformInstance>(this);
		crate.LockLifetime = true;
		crate.PositionPrecise = PositionPrecise + new Vector2(offsetX * Scale, 0x40 * Scale);
		crate.Setup(subtype, _flipX);
		return crate;
	}

	private void Setup(int subtype, bool flipX)
	{
		_flipX = flipX;
		_move = subtype / 6;
		NextMove();
	}

	/// <summary>Starts the move in the list, then points to the next one (loc_27CA2).</summary>
	private void NextMove()
	{
		int move = _move < Moves.GetLength(0) ? _move : 0;
		int speedX = _flipX ? -Moves[move, 0] : Moves[move, 0];
		_velocity = new Vector2(speedX * Scale / 256.0, Moves[move, 1] * Scale / 256.0);
		_moveTicks = Moves[move, 2];
		_move = move + 1;
		if (_move >= LoopMoves)
		{
			_move = 0;
		}
	}

	protected override void UpdateMovement()
	{
		PositionPrecise += _velocity;
		if (--_moveTicks == 0)
		{
			NextMove();
		}
	}

	protected override void OnStop()
	{
		_right?.Finish();
		_left?.Finish();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
