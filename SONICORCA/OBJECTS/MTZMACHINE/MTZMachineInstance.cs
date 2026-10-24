using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZMACHINE;

/// <summary>
/// A machine part of Metropolis Zone that moves in a circle, as in Sonic 2 (Obj6E). Subtype bits 4-5 select the kind:
/// 0 is a small block ($10 by $C), 1 a long bar ($28 by 8), 2 a large block ($60 by $18), all solid, and 3 the indent
/// of the large wheel, which only shows behind the other sprites. The position follows oscillators 8 and 9, which
/// make a circle with a radius of $38 pixels around the placement ($1C for the indent). Subtype bit 0 starts on the
/// other side of the circle, and bit 1 turns the circle by a quarter.
/// </summary>
public class MTZMachineInstance : Platform
{
	private const int Scale = 4;

	private const int IndentKind = 3;

	/// <summary>width_pixels and y_radius of each kind.</summary>
	private static readonly (int Width, int Height)[] Sizes = { (0x10, 0xC), (0x28, 8), (0x60, 0x18), (0xC, 0xC) };

	private AnimationInstance _animation;

	private int _subtype;

	private int _kind;

	private Vector2 _origin;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public MTZMachineInstance() => DesignBounds = new Rectanglei((-0x60 - 0x38) * Scale, (-0x18 - 0x38) * Scale, (0x60 + 0x38) * 2 * Scale, (0x18 + 0x38) * 2 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_kind = (_subtype >> 4) & 3;
		string key = _kind == IndentKind ? MTZMachineType.IndentAnimationGroupResourceKey : MTZMachineType.AnimationGroupResourceKey;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(key));
		_animation.Index = _kind;
		_origin = PositionPrecise;
		if (_kind == IndentKind)
		{
			CollisionVectors = new CollisionVector[0];
		}
		else
		{
			// The solid box: width_pixels + $B to each side, y_radius up and down
			(int width, int height) = Sizes[_kind];
			int halfWidth = (width + 0xB) * Scale;
			CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-halfWidth, -height * Scale, halfWidth * 2, height * 2 * Scale));
		}
		UpdateMovement();
	}

	protected override void UpdateMovement()
	{
		Oscillators oscillators = Oscillators.For(Level);
		int x;
		int y;
		if (_kind == IndentKind)
		{
			x = (sbyte)(byte)((oscillators.GetHighByte(Level, 8) >> 1) - 0x1C);
			y = (sbyte)(byte)((oscillators.GetHighByte(Level, 9) >> 1) - 0x1C);
		}
		else
		{
			x = (sbyte)(byte)(oscillators.GetHighByte(Level, 8) - 0x38);
			y = (sbyte)(byte)(oscillators.GetHighByte(Level, 9) - 0x38);
		}
		if ((_subtype & 1) != 0)
		{
			x = -x;
			y = -y;
		}
		if ((_subtype & 2) != 0)
		{
			(x, y) = (y, -x);
		}
		PositionPrecise = _origin + new Vector2(x * Scale, y * Scale);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
