using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZBARRIER;

/// <summary>
/// A one-way barrier, as in Sonic 2 (Obj2D in Wing Fortress Zone). It is a solid block 16 pixels wide and $40
/// pixels high. While a character is in the $200 pixels on the approach side, up to the barrier and $20 pixels above
/// or below its centre, the barrier rises 8 pixels each frame, up to $40 pixels. Then it goes down again. While the
/// barrier is open, the area also includes $18 pixels on the other side, so a character that passes through keeps it
/// open. The approach side is the left, or the right for a flipped barrier. The subtype is the mapping frame.
/// </summary>
public class WFZBarrierInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 8;

	private const int HalfHeight = 0x20;

	private const int ApproachDistance = 0x200;

	private const int PassDistance = 0x18;

	private const int OpenHeight = 0x40;

	private const int Speed = 8;

	private AnimationInstance _animation;

	private int _frame;

	private bool _flipX;

	private Vector2i _start;

	/// <summary>How far the barrier has risen, in original pixels.</summary>
	private int _offset;

	/// <summary>A character was in the area in the last frame (routine_secondary).</summary>
	private bool _opening;

	/// <summary>The mapping frame (the subtype of the original).</summary>
	[StateVariable]
	private int Frame
	{
		get => _frame;
		set => _frame = value;
	}

	/// <summary>The barrier opens for a character on the right.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public WFZBarrierInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZBarrierType.AnimationGroupResourceKey));
		_start = Position;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
	}

	protected override void OnUpdate()
	{
		_opening = Level.ObjectManager.Characters.Any(IsInArea);
		if (_opening)
		{
			if (_offset != OpenHeight)
			{
				_offset += Speed;
			}
		}
		else if (_offset != 0)
		{
			_offset -= Speed;
		}
		Position = _start - new Vector2i(0, _offset * Scale);
	}

	/// <summary>Obj2D_CheckCharacter: the area on the approach side, in original pixels from the start of the barrier.</summary>
	private bool IsInArea(ICharacter character)
	{
		if (character.IsObjectControlled || character.IsDebug || character.IsDead)
		{
			return false;
		}
		int left;
		int right;
		if (!_flipX)
		{
			left = -ApproachDistance;
			right = _opening ? PassDistance : 0;
		}
		else
		{
			left = _opening ? -PassDistance : 0;
			right = ApproachDistance;
		}
		double x = (character.PositionPrecise.X - _start.X) / Scale;
		double y = (character.PositionPrecise.Y - _start.Y) / Scale;
		return x >= left && x < right && y >= -HalfHeight && y < HalfHeight;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
