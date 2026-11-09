using System;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SONIC;

/// <summary>
/// The stars of a super character (Obj7E). At a high speed, the stars show their frames at the character. When they
/// come again while the speed stays high, they stay where they start, so they make a trail. Super Sonic and Super
/// Knuckles have them, each with the art of his import.
/// </summary>
internal sealed class SuperStars
{
	/// <summary>The stars show their mapping frames 0 to 5, and then start again.</summary>
	private const int Frames = 6;

	/// <summary>The stars come when the ground speed is at least $800.</summary>
	private const double Speed = 32.0;

	private readonly AnimationInstance _animation;

	/// <summary>objoff_30 of Obj7E: the stars show their frames.</summary>
	private bool _active;

	/// <summary>objoff_31 of Obj7E: the stars came again, so they stay where they start.</summary>
	private bool _repeat;

	private bool _visible;

	private int _frame;

	private int _timer;

	private Vector2 _position;

	/// <param name="group">The animation group of the stars: animation N shows mapping frame N of Obj7E.</param>
	public SuperStars(AnimationGroup group)
	{
		_animation = new AnimationInstance(group);
	}

	/// <summary>Stops the stars, as when the character becomes super.</summary>
	public void Reset()
	{
		_active = false;
		_repeat = false;
		_timer = 0;
	}

	/// <summary>Runs the stars for one tick.</summary>
	public void Update(Character character)
	{
		_visible = false;
		if (_active)
		{
			if (--_timer < 0)
			{
				_timer = 1;
				if (++_frame >= Frames)
				{
					_frame = 0;
					_active = false;
					_repeat = true;
					return;
				}
			}
		}
		else if (!character.IsObjectControlled && !character.IsTransformingToSuper && Math.Abs(character.GroundVelocity) >= Speed)
		{
			_frame = 0;
			_active = true;
			_position = character.PositionPrecise;
			_visible = true;
			return;
		}
		else
		{
			_repeat = false;
			return;
		}
		if (!_repeat)
			_position = character.PositionPrecise;
		_visible = true;
	}

	/// <summary>Draws the stars, in front of the character (Obj7E has a higher priority).</summary>
	public void Draw(Renderer renderer, Character character)
	{
		if (!_visible)
			return;
		_animation.Index = _frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _position - character.PositionPrecise);
	}
}
