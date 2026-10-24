using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZFLASHINGBLOCK;

/// <summary>
/// A solid block that moves through 16 positions in a U shape and changes its size, as in Sonic 2 (ObjD2). It shows
/// each position for 16 frames. After the last one, it waits (subtype * 16 frames) at the first position, and the
/// characters on it fall.
/// </summary>
public class CNZFlashingBlockInstance : Platform
{
	private const int Scale = 4;

	private const int Positions = 16;

	private const int PositionTicks = 16;

	/// <summary>The offset from the start and the half size of the block at each position, in original pixels.</summary>
	private static readonly sbyte[,] Steps =
	{
		{ -0x28, 0x18, 8, 8 }, { -0x28, 0x10, 8, 0x10 }, { -0x28, 8, 8, 0x18 }, { -0x28, 0, 8, 0x20 },
		{ -0x20, 0, 0x10, 0x20 }, { -0x18, -8, 0x18, 0x18 }, { -0x10, -0x10, 0x20, 0x10 }, { -8, -0x18, 0x28, 8 },
		{ 8, -0x18, 0x28, 8 }, { 0x10, -0x10, 0x20, 0x10 }, { 0x18, -8, 0x18, 0x18 }, { 0x20, 0, 0x10, 0x20 },
		{ 0x28, 0, 8, 0x20 }, { 0x28, 8, 8, 0x18 }, { 0x28, 0x10, 8, 0x10 }, { 0x28, 0x18, 8, 8 },
	};

	private AnimationInstance _animation;

	private int _delay;

	private bool _flipX;

	private Vector2i _start;

	private int _step;

	private int _waitTicks;

	private int _stepTicks;

	/// <summary>The wait before each cycle, in units of 16 frames (the subtype).</summary>
	[StateVariable]
	private int Delay
	{
		get => _delay;
		set => _delay = value;
	}

	/// <summary>The block goes from right to left (the flip of the original).</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public CNZFlashingBlockInstance() => DesignBounds = new Rectanglei(-0x50 * Scale, -0x30 * Scale, 0xA0 * Scale, 0x60 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZFlashingBlockType.AnimationGroupResourceKey));
		_start = Position;
		_waitTicks = _delay * PositionTicks;
		SetStep(0);
	}

	protected override void UpdateMovement()
	{
		if (_waitTicks > 0)
		{
			_waitTicks--;
			return;
		}
		if (--_stepTicks >= 0)
		{
			return;
		}
		_stepTicks = PositionTicks - 1;
		int step = (_step + 1) % Positions;
		if (step == 0)
		{
			_waitTicks = _delay * PositionTicks;
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				if (character.ObjectLink == this)
				{
					character.LeaveGround();
					character.IsAirborne = true;
				}
			}
		}
		SetStep(step);
	}

	private void SetStep(int step)
	{
		_step = step;
		int x = Steps[step, 0] * Scale;
		PositionPrecise = (Vector2)(_start + new Vector2i(_flipX ? -x : x, Steps[step, 1] * Scale));
		int halfWidth = Steps[step, 2] * Scale;
		int halfHeight = Steps[step, 3] * Scale;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-halfWidth, -halfHeight, halfWidth * 2, halfHeight * 2));
		if (Level != null)
		{
			RegisterCollisionUpdate();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _step;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
