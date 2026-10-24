using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZCOG;

/// <summary>
/// The giant cog of Metropolis Zone, as in Sonic 2 (Obj70). The cog has 8 solid teeth on a circle with a radius of
/// $48 pixels. The placed object is the first tooth, and it adds the other 7 teeth. Each 16 frames, all teeth turn
/// one step: 4 steps take a tooth to the place of the next tooth. The cog turns clockwise, or anticlockwise when the
/// placement is flipped. Each tooth is $10 pixels to each side, and $10 pixels up and down. The teeth that point
/// to the sides (frames 7 to 9 of each 16) are less high.
/// </summary>
public class MTZCogInstance : Platform
{
	private const int Scale = 4;

	private const int TeethCount = 8;

	private const int StepCount = 4;

	private const int StepFrames = 0x10;

	private const int HalfWidth = 0x10;

	/// <summary>The x and y offset and the mapping frame of each tooth, for each of the 4 steps (Obj70_Positions).</summary>
	private static readonly sbyte[,] Positions =
	{
		{ 0x00, -0x48, 0x00 }, { 0x32, -0x32, 0x04 }, { 0x48, 0x00, 0x08 }, { 0x32, 0x32, 0x0C },
		{ 0x00, 0x48, 0x10 }, { -0x32, 0x32, 0x14 }, { -0x48, 0x00, 0x18 }, { -0x32, -0x32, 0x1C },

		{ 0x0D, -0x48, 0x01 }, { 0x3F, -0x26, 0x05 }, { 0x48, 0x0C, 0x09 }, { 0x27, 0x3C, 0x0D },
		{ -0x0D, 0x48, 0x11 }, { -0x3F, 0x26, 0x15 }, { -0x48, -0x0C, 0x19 }, { -0x27, -0x3C, 0x1D },

		{ 0x19, -0x44, 0x02 }, { 0x46, -0x17, 0x06 }, { 0x46, 0x17, 0x0A }, { 0x19, 0x44, 0x0E },
		{ -0x19, 0x44, 0x12 }, { -0x46, 0x17, 0x16 }, { -0x46, -0x17, 0x1A }, { -0x19, -0x44, 0x1E },

		{ 0x27, -0x3C, 0x03 }, { 0x48, -0x0C, 0x07 }, { 0x3F, 0x26, 0x0B }, { 0x0D, 0x48, 0x0F },
		{ -0x27, 0x3C, 0x13 }, { -0x48, 0x0C, 0x17 }, { -0x3F, -0x26, 0x1B }, { -0x0D, -0x48, 0x1F },
	};

	/// <summary>The half height of the solid box for each frame, modulo 16 (byte_28706).</summary>
	private static readonly int[] HalfHeights = { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0xC, 8, 0xC, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10 };

	private AnimationInstance _animation;

	private bool _flipX;

	private Vector2 _centre;

	private int _tooth;

	private int _step;

	private int _frame;

	/// <summary>The cog turns anticlockwise.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MTZCogInstance() => DesignBounds = new Rectanglei(-0x60 * Scale, -0x60 * Scale, 0xC0 * Scale, 0xC0 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZCogType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, GetSolidBox());
		if (ParentObject != null)
		{
			// A tooth that the first tooth adds. The first tooth sets it up.
			return;
		}
		SetTooth(PositionPrecise, 0, _flipX);
		for (int i = 1; i < TeethCount; i++)
		{
			MTZCogInstance tooth = Level.ObjectManager.AddSubObject<MTZCogInstance>(this);
			tooth.LockLifetime = true;
			tooth.SetTooth(_centre, i, _flipX);
		}
	}

	private void SetTooth(Vector2 centre, int tooth, bool flipX)
	{
		_centre = centre;
		_tooth = tooth;
		_step = 0;
		_flipX = flipX;
		Place();
	}

	private Rectanglei GetSolidBox()
	{
		int halfHeight = HalfHeights[_frame & 0xF];
		return new Rectanglei(-HalfWidth * Scale, -halfHeight * Scale, HalfWidth * 2 * Scale, halfHeight * 2 * Scale);
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
		if ((Level.Ticks & (StepFrames - 1)) != 0)
		{
			return;
		}
		if (_flipX)
		{
			if (--_step < 0)
			{
				_step = StepCount - 1;
				_tooth = (_tooth + TeethCount - 1) % TeethCount;
			}
		}
		else if (++_step == StepCount)
		{
			_step = 0;
			_tooth = (_tooth + 1) % TeethCount;
		}
		Place();
	}

	/// <summary>Moves the tooth to its place for the step, and sets its frame and its solid box.</summary>
	private void Place()
	{
		int i = _step * TeethCount + _tooth;
		PositionPrecise = _centre + new Vector2(Positions[i, 0] * Scale, Positions[i, 1] * Scale);
		_frame = Positions[i, 2];
		if (CollisionVectors != null && CollisionVectors.Length == 4)
		{
			CollisionVector.UpdateRectangle(CollisionVectors, GetSolidBox());
			RegisterCollisionUpdate();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
