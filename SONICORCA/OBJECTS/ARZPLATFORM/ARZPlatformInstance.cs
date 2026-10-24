using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZPLATFORM;

/// <summary>
/// A platform of Aquatic Ruin Zone, as in Sonic 2 (Obj18). The subtype gives the kind (bit 7: solid on all sides,
/// bits 4-6: the frame, bits 0-3: the movement). The platform sags up to 4 pixels while a character stands on it.
/// It moves with oscillator 6 (to the side or up and down, $40 pixels), with oscillator 3, or falls 30 frames after
/// a character stands on it.
/// </summary>
public class ARZPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int HalfWidth = 0x20 * Scale;

	/// <summary>The solid kind is $28 pixels from the centre up and down in Aquatic Ruin.</summary>
	private const int SolidHalfHeight = 0x28 * Scale;

	private const int TopHeight = 8 * Scale;

	private AnimationInstance _animation;

	private int _subtype;

	private int _type;

	private Vector2 _origin;

	private double _yActual;

	private int _angle = 0x80;

	private int _sag;

	private int _delay;

	private int _velocityY;

	private bool _falling;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public ARZPlatformInstance() => DesignBounds = new Rectanglei(-HalfWidth, -0x30 * Scale, HalfWidth * 2, 0x60 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZPlatformType.AnimationGroupResourceKey));
		_animation.Index = ((_subtype >> 4) & 7) == 1 ? 1 : 0;
		_type = _subtype & 0xF;
		_origin = PositionPrecise;
		_yActual = _origin.Y;
		CollisionVectors = (_subtype & 0x80) != 0
			? CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -SolidHalfHeight, HalfWidth * 2, SolidHalfHeight * 2))
			: new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
	}

	protected override void UpdateMovement()
	{
		bool standing = IsCharacterInteractingWithPlatform();
		_sag = standing ? System.Math.Min(_sag + 4, 0x40) : System.Math.Max(_sag - 4, 0);
		double x = PositionPrecise.X;
		int angleFromCentre = (sbyte)(byte)(_angle - 0x40);
		switch (_type)
		{
			case 1:
				x = _origin.X + angleFromCentre * Scale;
				break;
			case 5:
				x = _origin.X - angleFromCentre * Scale;
				break;
			case 2:
				_yActual = _origin.Y + angleFromCentre * Scale;
				break;
			case 6:
				_yActual = _origin.Y - angleFromCentre * Scale;
				break;
			case 0xA:
				_yActual = _origin.Y + (angleFromCentre >> 1) * Scale;
				break;
			case 0xB:
				_yActual = _origin.Y + ((sbyte)(byte)(0x40 - _angle) >> 1) * Scale;
				break;
			case 0xC:
				_yActual = _origin.Y + (sbyte)(byte)(Oscillators.For(Level).GetHighByte(Level, 3) - 0x30) * Scale;
				break;
			case 0xD:
				_yActual = _origin.Y + (sbyte)(byte)(0x30 - Oscillators.For(Level).GetHighByte(Level, 3)) * Scale;
				break;
			case 3:
				if (_delay == 0)
				{
					if (standing)
					{
						_delay = 30;
					}
				}
				else if (--_delay == 0)
				{
					_delay = 0x20;
					_type = 4;
				}
				break;
			case 4:
				Fall();
				break;
		}
		// The original reads the oscillator after it moves, so the next frame uses this value
		if (_type != 3 && _type != 4)
		{
			_angle = Oscillators.For(Level).GetHighByte(Level, 6);
		}
		double sag = _falling ? 0.0 : OriginalMath.Sin(_sag) * 4.0 / 256.0 * Scale;
		PositionPrecise = new Vector2(x, _yActual + sag);
	}

	/// <summary>The platform stays 32 frames, then drops the characters on it and falls with gravity $38.</summary>
	private void Fall()
	{
		if (_delay != 0 && --_delay == 0)
		{
			_falling = true;
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				if (character.ObjectLink == this)
				{
					character.LeaveGround();
					character.IsAirborne = true;
				}
			}
			CollisionVectors = new CollisionVector[0];
		}
		_yActual += _velocityY * Scale / 256.0;
		_velocityY += 0x38;
		if (_yActual > Level.Bounds.Bottom + 0x120 * Scale)
		{
			Finish();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
