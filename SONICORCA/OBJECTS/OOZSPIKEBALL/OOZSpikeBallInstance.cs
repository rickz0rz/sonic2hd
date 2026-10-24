using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZSPIKEBALL;

/// <summary>
/// Sliding spike balls in Oil Ocean Zone, as in Sonic 2 (Obj43). Each ball slides 1 pixel each frame between two
/// limits and turns back at each limit. Subtype 0 is one ball that slides $68 pixels to each side of its start.
/// Subtypes 6 and $C are two balls: the first starts to the left and slides left, the second slides right. The
/// limits are $E8 (subtype 6) or $A8 (subtype $C) pixels to each side of the start. Both balls turn back when they
/// meet. A ball hurts a character that touches it.
/// </summary>
public class OOZSpikeBallInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The touch box of a ball (collision size $25): $18 pixels to each side, $28 pixels up and down.</summary>
	private const int TouchHalfWidth = 0x18;

	private const int TouchHalfHeight = 0x28;

	/// <summary>The two balls turn back when the distance between them is $30 pixels.</summary>
	private const int MeetDistance = 0x30;

	/// <summary>
	/// The balls of each subtype (Obj43_Properties): the count, the range to each side of the start (a byte that the
	/// original reads as unsigned), and the start of each ball from the placement.
	/// </summary>
	private static readonly (int Count, int Range, int FirstOffset, int SecondOffset)[] Properties =
	{
		(1, 0x68, 0, 0),
		(2, 0xE8, -0x18, 0x18),
		(2, 0xA8, -0x58, -0x28),
	};

	private AnimationInstance _animation;

	private int _subtype;

	private int _count;

	private int _min;

	private int _max;

	/// <summary>The position of each ball from the placement, in original pixels.</summary>
	private readonly int[] _x = new int[2];

	private readonly bool[] _movingRight = new bool[2];

	/// <summary>The subtype of the original: 0, 6 or $C.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public OOZSpikeBallInstance() => DesignBounds = new Rectanglei(-0x100 * Scale, -TouchHalfHeight * Scale, 0x200 * Scale, TouchHalfHeight * 2 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZSpikeBallType.AnimationGroupResourceKey));
		(int count, int range, int firstOffset, int secondOffset) = Properties[Math.Clamp(_subtype / 6, 0, Properties.Length - 1)];
		_count = count;
		_min = -range;
		_max = range;
		_x[0] = firstOffset;
		_x[1] = secondOffset;
		_movingRight[0] = false;
		_movingRight[1] = true;
		int reach = range + TouchHalfWidth;
		DesignBounds = new Rectanglei(-reach * Scale, -TouchHalfHeight * Scale, reach * 2 * Scale, TouchHalfHeight * 2 * Scale);
		UpdateTouchBoxes();
	}

	protected override void OnUpdate()
	{
		for (int i = 0; i < _count; i++)
		{
			Slide(i);
		}
		if (_count == 2 && _x[1] - _x[0] == MeetDistance)
		{
			_movingRight[0] = !_movingRight[0];
			_movingRight[1] = !_movingRight[1];
		}
		UpdateTouchBoxes();
	}

	/// <summary>Moves a ball 1 pixel, and turns it back when it reaches a limit.</summary>
	private void Slide(int i)
	{
		if (_movingRight[i])
		{
			if (++_x[i] == _max)
			{
				_movingRight[i] = false;
			}
		}
		else if (--_x[i] == _min)
		{
			_movingRight[i] = true;
		}
	}

	private void UpdateTouchBoxes()
	{
		var rectangles = new CollisionRectangle[_count];
		for (int i = 0; i < _count; i++)
		{
			rectangles[i] = new CollisionRectangle(this, i + 1, (_x[i] - TouchHalfWidth) * Scale, -TouchHalfHeight * Scale,
				TouchHalfWidth * 2 * Scale, TouchHalfHeight * 2 * Scale);
		}
		CollisionRectangles = rectangles;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id > 0 && e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			var character = (ICharacter)e.ActiveObject;
			character.Hurt(Math.Sign(character.Position.X - (Position.X + _x[e.Id - 1] * Scale)));
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = 0;
		for (int i = 0; i < _count; i++)
		{
			objectRenderer.Render(_animation, new Vector2(_x[i] * Scale, 0.0));
		}
	}
}
