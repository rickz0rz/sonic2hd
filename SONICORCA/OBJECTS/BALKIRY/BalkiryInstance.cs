using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.BALKIRY;

/// <summary>
/// Balkiry, as in Sonic 2 (ObjAC). It flies straight to the left at $300 with the Tornado velocity added, or at $500
/// when it is flipped vertically in the level. The flame of its jet (Obj9C) flickers behind it. It goes when it
/// leaves the screen on the left.
/// </summary>
public class BalkiryInstance : Badnik
{
	private const int Scale = 4;

	/// <summary>Speeds in 1/256 original pixels for each frame, at four times the resolution.</summary>
	private const double Unit = Scale / 256.0;

	private const int Speed = -0x300;

	private const int FastSpeed = -0x500;

	/// <summary>The touch size: $18 x $C.</summary>
	private const int TouchHalfWidth = 0x18 * Scale;

	private const int TouchHalfHeight = 0xC * Scale;

	private const int Frame = 1;

	/// <summary>The jet of obj9C: frames 8 and 9, 2 frames each.</summary>
	private const int JetFrame = 8;

	private const int FrameTicks = 2;

	private AnimationInstance _animation;

	private AnimationInstance _jetAnimation;

	private SkyChaseScroll _scroll;

	private bool _fast;

	private int _ticks;

	/// <summary>The faster Balkiry (the vertical flip of the original).</summary>
	[StateVariable]
	private bool Fast
	{
		get => _fast;
		set => _fast = value;
	}

	public BalkiryInstance() => DesignBounds = new Rectanglei(-0x24 * Scale, -0x14 * Scale, 0x48 * Scale, 0x28 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(BalkiryType.AnimationGroupResourceKey));
		_jetAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(BalkiryType.JetAnimationGroupResourceKey));
		_scroll = SkyChaseScroll.For(Level);
		CollisionRectangles = new[]
		{
			new CollisionRectangle(this, 0, -TouchHalfWidth, -TouchHalfHeight, TouchHalfWidth * 2, TouchHalfHeight * 2)
		};
	}

	protected override void OnUpdate()
	{
		_ticks++;
		PositionPrecise += new Vector2((_fast ? FastSpeed : Speed) * Unit, 0.0) + _scroll.Velocity;
		if (Position.X + DesignBounds.Right < Level.Camera.Bounds.Left)
		{
			Finish();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_jetAnimation.Index = JetFrame + (_ticks / FrameTicks) % 2;
		objectRenderer.RenderOriginal(_jetAnimation);
		_animation.Index = Frame;
		objectRenderer.RenderOriginal(_animation);
	}
}
