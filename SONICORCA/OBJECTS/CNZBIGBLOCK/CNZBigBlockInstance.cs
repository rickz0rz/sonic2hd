using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZBIGBLOCK;

/// <summary>
/// A big solid block, as in Sonic 2 (ObjD4). It starts $60 pixels to one side of its centre and swings through the
/// centre like a spring: each frame its speed changes by 4/256 of a pixel toward the centre.
/// </summary>
public class CNZBigBlockInstance : Platform
{
	private const int Scale = 4;

	private const int Swing = 0x60;

	/// <summary>The change of speed each frame, in 1/256 of an original pixel.</summary>
	private const int Acceleration = 4;

	/// <summary>The block is 32 pixels from the centre to each side.</summary>
	private const int HalfSize = 32 * Scale;

	private AnimationInstance _animation;

	private bool _vertical;

	private bool _startOnOtherSide;

	private Vector2 _centre;

	/// <summary>The offset from the centre and the speed, in original pixels and 1/256 pixels.</summary>
	private double _offset;

	private int _speed;

	/// <summary>The block moves up and down instead of to the left and the right (subtype 2).</summary>
	[StateVariable]
	private bool Vertical
	{
		get => _vertical;
		set => _vertical = value;
	}

	/// <summary>The block starts on the right (or at the bottom) instead (the flip of the original).</summary>
	[StateVariable]
	private bool StartOnOtherSide
	{
		get => _startOnOtherSide;
		set => _startOnOtherSide = value;
	}

	public CNZBigBlockInstance() => DesignBounds = new Rectanglei(-HalfSize - Swing * Scale, -HalfSize - Swing * Scale, (HalfSize + Swing * Scale) * 2, (HalfSize + Swing * Scale) * 2);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZBigBlockType.AnimationGroupResourceKey));
		_centre = (Vector2)Position;
		// The original also starts half way into a pixel
		_offset = (_startOnOtherSide ? Swing : -Swing) + 0.5;
		PositionPrecise = GetPosition();
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfSize, -HalfSize, HalfSize * 2, HalfSize * 2));
	}

	protected override void UpdateMovement()
	{
		_speed += _offset < 0.0 ? Acceleration : -Acceleration;
		_offset += _speed / 256.0;
		PositionPrecise = GetPosition();
	}

	private Vector2 GetPosition() => _centre + (_vertical ? new Vector2(0.0, _offset * Scale) : new Vector2(_offset * Scale, 0.0));

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
