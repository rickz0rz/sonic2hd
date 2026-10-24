using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZPULLEYPLATFORM;

/// <summary>
/// The small platforms of Metropolis Zone that go around two pulleys, as in Sonic 2 (Obj6C). The placement (subtype
/// $80 to $82) is the first of 8 platforms on a loop, and it adds the other 7. The loop goes $20 pixels to each side
/// of the placement and down $100, $180 or $200 pixels (subtype bits 0-6). Each platform goes from point to point of
/// the loop at 1 pixel each frame along the longer axis. The platforms go down the left side and up the right side,
/// or the other way when the placement is flipped. A character can stand on the top of a platform ($10 pixels to each side, 8 pixels above
/// the centre).
/// </summary>
public class MTZPulleyPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int HalfWidth = 0x10;

	private const int TopHeight = 8;

	private const int PlatformCount = 8;

	/// <summary>The points of each loop (off_28252), from the placement, in original pixels.</summary>
	private static readonly Vector2i[][] Paths =
	{
		Loop(0x100),
		Loop(0x180),
		Loop(0x200),
	};

	/// <summary>
	/// The position (from the placement) and the subtype of each of the 8 platforms of each loop (off_282D6). Subtype
	/// bits 4-7 select the loop, and bits 0-3 the first point that the platform goes to.
	/// </summary>
	private static readonly (int X, int Y, int Subtype)[][] Platforms =
	{
		new[] { (0, 0, 0x01), (-0x20, 0x3A, 0x03), (-0x20, 0x80, 0x03), (-0x20, 0xC6, 0x03), (0, 0x100, 0x06), (0x20, 0xC6, 0x08), (0x20, 0x80, 0x08), (0x20, 0x3A, 0x08) },
		new[] { (0, 0, 0x11), (-0x20, 0x5A, 0x13), (-0x20, 0xC0, 0x13), (-0x20, 0x126, 0x13), (0, 0x180, 0x16), (0x20, 0x126, 0x18), (0x20, 0xC0, 0x18), (0x20, 0x5A, 0x18) },
		new[] { (0, 0, 0x21), (-0x20, 0x7A, 0x23), (-0x20, 0x100, 0x23), (-0x20, 0x186, 0x23), (0, 0x200, 0x26), (0x20, 0x186, 0x28), (0x20, 0x100, 0x28), (0x20, 0x7A, 0x28) },
	};

	private AnimationInstance _animation;

	private int _subtype = 0x80;

	private bool _flipX;

	private Vector2 _centre;

	private Vector2i[] _path;

	private int _point;

	private Vector2i _from;

	private Vector2i _target;

	private int _frames;

	private int _frame;

	/// <summary>The subtype of the original: $80 to $82 for a loop of 8 platforms.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The platforms go down the right side.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MTZPulleyPlatformInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0x220 * Scale, 0x80 * Scale, 0x440 * Scale);

	/// <summary>The 10 points of a loop with the depth: around the top pulley, down the left side, around the bottom pulley and up the right side.</summary>
	private static Vector2i[] Loop(int depth)
	{
		return new[]
		{
			new Vector2i(0, 0), new Vector2i(-0x16, 0xA), new Vector2i(-0x20, 0x20), new Vector2i(-0x20, depth - 0x20),
			new Vector2i(-0x16, depth - 0xA), new Vector2i(0, depth), new Vector2i(0x16, depth - 0xA),
			new Vector2i(0x20, depth - 0x20), new Vector2i(0x20, 0x20), new Vector2i(0x16, 0xA)
		};
	}

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZPulleyPlatformType.AnimationGroupResourceKey));
		CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth * Scale, -TopHeight * Scale), new Vector2i(HalfWidth * Scale, -TopHeight * Scale)) };
		if (ParentObject != null)
		{
			// A platform that the first platform adds. The first platform sets it up.
			return;
		}
		Vector2 centre = PositionPrecise;
		if ((_subtype & 0x80) == 0)
		{
			// A single platform: the placement is the centre of the loop
			SetUp(centre, centre, _subtype, _flipX);
			return;
		}
		(int X, int Y, int Subtype)[] platforms = Platforms[Math.Min(_subtype & 0x7F, Platforms.Length - 1)];
		for (int i = 0; i < PlatformCount; i++)
		{
			(int x, int y, int subtype) = platforms[i];
			Vector2 position = centre + new Vector2(x * Scale, y * Scale);
			MTZPulleyPlatformInstance platform = i == 0 ? this : Level.ObjectManager.AddSubObject<MTZPulleyPlatformInstance>(this);
			if (i != 0)
			{
				platform.LockLifetime = true;
			}
			platform.SetUp(centre, position, subtype, _flipX);
		}
	}

	private void SetUp(Vector2 centre, Vector2 position, int subtype, bool flipX)
	{
		_centre = centre;
		_flipX = flipX;
		_path = Paths[Math.Min((subtype >> 4) & 0xF, Paths.Length - 1)];
		PositionPrecise = position;
		_point = subtype & 0xF;
		if (_flipX)
		{
			_point = Wrap(_point - 1);
		}
		StartMove();
	}

	private int Wrap(int point) => (point % _path.Length + _path.Length) % _path.Length;

	/// <summary>Starts the move to the current point. The longer axis moves 1 pixel each frame.</summary>
	private void StartMove()
	{
		_from = new Vector2i((int)Math.Round((PositionPrecise.X - _centre.X) / Scale), (int)Math.Round((PositionPrecise.Y - _centre.Y) / Scale));
		_target = _path[Wrap(_point)];
		Vector2i delta = _target - _from;
		_frames = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
		_frame = 0;
	}

	protected override void OnUpdate()
	{
		if (ParentObject != null && ParentObject.Finished)
		{
			Finish();
			return;
		}
		base.OnUpdate();
	}

	protected override void UpdateMovement()
	{
		if (_path == null)
		{
			return;
		}
		if (_frame >= _frames)
		{
			_point = Wrap(_point + (_flipX ? -1 : 1));
			StartMove();
		}
		if (_frames == 0)
		{
			return;
		}
		_frame++;
		Vector2i delta = _target - _from;
		double t = _frame / (double)_frames;
		PositionPrecise = _centre + new Vector2((_from.X + delta.X * t) * Scale, (_from.Y + delta.Y * t) * Scale);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
