using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZSWINGPLATFORM;

/// <summary>
/// A platform on a chain, as in Sonic 2 (Obj15 in Aquatic Ruin Zone). It swings through half a circle below its
/// pivot with oscillator 6. The platform of the fall-off kind comes off the chain at the end of a swing while a
/// character stands on it: it flies at $200, falls with gravity $18 to the water, then floats to the right until it
/// meets a wall. The Mystic Cave version derives from this class.
/// </summary>
public class ARZSwingPlatformInstance : Platform
{
	protected const int Scale = 4;

	private const int TopHeight = 9 * Scale;

	private const int PlatformFrame = 0;

	private const int LinkFrame = 1;

	private const int PivotFrame = 2;

	private AnimationInstance _animation;

	private int _links = 6;

	private bool _fallOff;

	private bool _flipX;

	private Vector2 _pivot;

	private int _cachedSwing;

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
	protected int Links
	{
		get => _links;
		set => _links = value;
	}

	/// <summary>The platform comes off the chain (subtype bit 7).</summary>
	[StateVariable]
	protected bool FallOff
	{
		get => _fallOff;
		set => _fallOff = value;
	}

	/// <summary>The swing is mirrored.</summary>
	[StateVariable]
	protected bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	/// <summary>Half the width of the platform top (width_pixels).</summary>
	protected virtual int HalfWidth => 0x20 * Scale;

	/// <summary>The pivot of the chain.</summary>
	protected Vector2 Pivot => _pivot;

	public ARZSwingPlatformInstance() => DesignBounds = new Rectanglei(-1024, -256, 2048, 1280);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZSwingPlatformType.AnimationGroupResourceKey));
		_pivot = PositionPrecise;
		int radius = (_links * 2 + 1) * 8 * Scale;
		DesignBounds = new Rectanglei(-radius - HalfWidth, -radius - 64, (radius + HalfWidth) * 2, radius * 2 + 128);
		PositionPrecise = _pivot + new Vector2(0.0, (_links * 0x10 + 8) * Scale);
		CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
	}

	protected override void UpdateMovement()
	{
		int oscillator = Oscillators.For(Level).GetHighByte(Level, 6);
		int swing = GetSwing(oscillator);
		if (swing != _cachedSwing)
		{
			_cachedSwing = swing;
			_chainAngle = _flipX ? 0x80 - swing : swing;
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
					_velocity = new Vector2((_flipX ? -0x200 : 0x200) * Scale / 256.0, 0.0);
				}
				break;
			case State.Fall:
			{
				PositionPrecise += _velocity;
				_velocity.Y += 0x18 * Scale / 256.0;
				double water = Level.WaterManager.WaterAreas.Count > 0 ? Level.WaterManager.WaterAreas[0].Top : double.MaxValue;
				if (PositionPrecise.Y >= water)
				{
					// The original always floats to the right here, even when the swing is mirrored
					_state = State.Float;
					_floatY = water;
					_velocity = new Vector2(0x100 * Scale / 256.0, 0.0);
				}
				break;
			}
			case State.Float:
			{
				double x = PositionPrecise.X + _velocity.X;
				if (_velocity.X != 0.0)
				{
					var front = new Vector2i((int)x + HalfWidth, (int)PositionPrecise.Y);
					if (OriginalMath.HitsLandscape(Level, front - new Vector2i((int)_velocity.X + 1, 0), front))
					{
						_velocity = new Vector2();
						x = PositionPrecise.X;
					}
				}
				PositionPrecise = new Vector2(x, _floatY + (Oscillators.For(Level).GetHighByte(Level, 5) >> 1) * Scale);
				break;
			}
		}
	}

	/// <summary>
	/// Gives the angle of the chain before the mirror, from the high byte of oscillator 6. The platform moves only
	/// when this value changes. Angle $40 is straight down.
	/// </summary>
	protected virtual int GetSwing(int oscillator) => oscillator;

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
		RenderPlatform(objectRenderer, _animation);
	}

	/// <summary>Draws the platform at the object position. The animation has the platform, link and pivot frames.</summary>
	protected virtual void RenderPlatform(IObjectRenderer objectRenderer, AnimationInstance animation)
	{
		animation.Index = PlatformFrame;
		objectRenderer.Render(animation);
	}
}
