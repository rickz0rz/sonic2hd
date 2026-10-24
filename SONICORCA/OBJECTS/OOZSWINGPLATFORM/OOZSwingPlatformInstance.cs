using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZSWINGPLATFORM;

/// <summary>
/// A platform on a chain, as in Sonic 2 (Obj15 in Oil Ocean Zone). It swings through half a circle below its pivot
/// with oscillator 6. The platform of the fall-off kind (subtype bit 7) comes off the chain at the end of a swing
/// while a character stands on it: it flies to the side at $200 and falls with gravity $18 to the oil ($720 in the
/// original act). There it stops and bobs with oscillator 5. Unlike the platform of Aquatic Ruin, it does not float
/// to the side.
/// </summary>
public class OOZSwingPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int HalfWidth = 0x20 * Scale;

	/// <summary>The top of the platform: y_radius $10 plus 1.</summary>
	private const int TopHeight = 0x11 * Scale;

	private const int PlatformFrame = 0;

	private const int LinkFrame = 1;

	private const int PivotFrame = 2;

	private const double FallSpeed = 0x200 * Scale / 256.0;

	private const double Gravity = 0x18 * Scale / 256.0;

	private AnimationInstance _animation;

	private int _links = 8;

	private bool _fallOff;

	private bool _flipX;

	private int _floatDepth = 0x100 * Scale;

	private Vector2 _pivot;

	private int _cachedOscillator = -1;

	private int _chainAngle = 0x40;

	private State _state;

	private Vector2 _velocity;

	private double _floatY;

	private enum State
	{
		Swing,
		Fall,
		Float
	}

	/// <summary>The number of chain links (subtype bits 0-3).</summary>
	[StateVariable]
	private int Links
	{
		get => _links;
		set => _links = value;
	}

	/// <summary>The platform comes off the chain (subtype bit 7).</summary>
	[StateVariable]
	private bool FallOff
	{
		get => _fallOff;
		set => _fallOff = value;
	}

	/// <summary>The swing is mirrored, and the platform falls off to the left.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	/// <summary>
	/// The distance from the pivot down to the line where the fallen platform stops (y $720 in the original act),
	/// in level units.
	/// </summary>
	[StateVariable]
	private int FloatDepth
	{
		get => _floatDepth;
		set => _floatDepth = value;
	}

	public OOZSwingPlatformInstance() => DesignBounds = new Rectanglei(-1024, -256, 2048, 1280);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZSwingPlatformType.AnimationGroupResourceKey));
		_pivot = PositionPrecise;
		int radius = (_links * 2 + 1) * 8 * Scale;
		int bottom = System.Math.Max(radius, _floatDepth) + 128;
		DesignBounds = new Rectanglei(-radius - HalfWidth, -radius - 64, (radius + HalfWidth) * 2, radius + 64 + bottom);
		PositionPrecise = _pivot + new Vector2(0.0, (_links * 0x10 + 8) * Scale);
		CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
	}

	protected override void UpdateMovement()
	{
		int oscillator = Oscillators.For(Level).GetHighByte(Level, 6);
		if (oscillator != _cachedOscillator)
		{
			_cachedOscillator = oscillator;
			_chainAngle = _flipX ? 0x80 - oscillator : oscillator;
			if (_state == State.Swing)
			{
				PositionPrecise = PointOnChain((_links * 2 + 1) * 8);
			}
		}
		switch (_state)
		{
			case State.Swing:
				if (_fallOff && oscillator == 0 && Level.ObjectManager.Characters.Any(c => c.ObjectLink == this))
				{
					_state = State.Fall;
					_velocity = new Vector2(_flipX ? -FallSpeed : FallSpeed, 0.0);
				}
				break;
			case State.Fall:
			{
				PositionPrecise += _velocity;
				_velocity.Y += Gravity;
				double floatLine = _pivot.Y + _floatDepth;
				if (PositionPrecise.Y >= floatLine)
				{
					_state = State.Float;
					_floatY = floatLine;
					_velocity = new Vector2();
					PositionPrecise = new Vector2(PositionPrecise.X, floatLine);
				}
				break;
			}
			case State.Float:
				PositionPrecise = new Vector2(PositionPrecise.X, _floatY + (Oscillators.For(Level).GetHighByte(Level, 5) >> 1) * Scale);
				break;
		}
	}

	/// <summary>A point on the chain at a distance from the pivot, in original pixels.</summary>
	private Vector2 PointOnChain(int distance)
	{
		return _pivot + new Vector2(distance * OriginalMath.Cos(_chainAngle) / 256.0 * Scale, distance * OriginalMath.Sin(_chainAngle) / 256.0 * Scale);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The pivot and the links stay on the chain, also after the platform comes off
		_animation.Index = PivotFrame;
		objectRenderer.Render(_animation, _pivot - PositionPrecise);
		_animation.Index = LinkFrame;
		for (int i = 1; i <= _links; i++)
		{
			objectRenderer.Render(_animation, PointOnChain(i * 0x10) - PositionPrecise);
		}
		_animation.Index = PlatformFrame;
		objectRenderer.Render(_animation);
	}
}
