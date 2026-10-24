using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZPILLAR;

/// <summary>
/// A solid pillar or platform, as in Sonic 2 (Obj82). Subtype bits 4-6 give the kind (0: a small platform, 1: a
/// pillar), bits 0-3 the movement (0: none, 1: it falls 30 frames after a character stands on it, with gravity 8,
/// until it lands). A pillar that can fall sags up to 4 pixels while a character stands on it.
/// </summary>
public class ARZPillarInstance : Platform
{
	private const int Scale = 4;

	private AnimationInstance _animation;

	private int _subtype;

	private int _type;

	private int _halfWidth;

	private int _halfHeight;

	private bool _sags;

	private int _sag;

	private int _delay;

	private double _y;

	private double _velocityY;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public ARZPillarInstance() => DesignBounds = new Rectanglei(-0x20 * Scale, -0x38 * Scale, 0x40 * Scale, 0x70 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZPillarType.AnimationGroupResourceKey));
		bool pillar = ((_subtype >> 4) & 7) == 1;
		_animation.Index = pillar ? 1 : 0;
		_halfWidth = (pillar ? 0x1C : 0x20) * Scale;
		_halfHeight = (pillar ? 0x30 : 8) * Scale;
		_type = _subtype & 0xF;
		_sags = _type != 0 && _type != 7;
		_y = PositionPrecise.Y;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-_halfWidth, -_halfHeight, _halfWidth * 2, _halfHeight * 2));
	}

	protected override void UpdateMovement()
	{
		bool standing = IsCharacterInteractingWithPlatform();
		switch (_type)
		{
			case 1:
				if (_delay == 0)
				{
					if (standing)
					{
						_delay = 0x1E;
					}
				}
				else if (--_delay == 0)
				{
					_type = 2;
					_sags = false;
				}
				break;
			case 2:
				_y += _velocityY;
				_velocityY += 8 * Scale / 256.0;
				int? floor = LandscapeFloor.GetDistance(Level, new Vector2i((int)PositionPrecise.X, (int)_y + _halfHeight));
				if (floor < 0)
				{
					_y += (floor.Value + 1) * Scale;
					_velocityY = 0.0;
					_type = 0;
				}
				break;
		}
		if (_sags)
		{
			_sag = standing ? System.Math.Min(_sag + 4, 0x40) : System.Math.Max(_sag - 4, 0);
		}
		PositionPrecise = new Vector2(PositionPrecise.X, _y + OriginalMath.Sin(_sag) * 4.0 / 256.0 * Scale);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
