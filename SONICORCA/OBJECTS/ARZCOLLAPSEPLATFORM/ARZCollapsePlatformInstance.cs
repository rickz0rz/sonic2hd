using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZCOLLAPSEPLATFORM;

/// <summary>
/// A platform that collapses, as in Sonic 2 (Obj1F in Aquatic Ruin Zone). 9 frames after a character lands on it, it
/// breaks into 8 pieces. Each piece waits its own delay, then falls with gravity $38. The platform stays solid until
/// the first piece falls.
/// </summary>
public class ARZCollapsePlatformInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0x20 * Scale;

	private const int TopHeight = 0x10 * Scale;

	private const int Gravity = 0x38;

	/// <summary>The delay of each piece (Obj1F_ARZ_DelayData).</summary>
	private static readonly int[] Delays = { 0x16, 0x1A, 0x18, 0x12, 0x06, 0x0E, 0x0A, 0x02 };

	/// <summary>Frame 0 is the platform. The pieces of frame 1 follow the 2 frames.</summary>
	private const int FirstPiece = 2;

	private const int LifeTicks = 240;

	private AnimationInstance _animation;

	private bool _stoodOn;

	private int _counter = 7;

	private bool _collapsed;

	private int _ticks;

	private readonly double[] _pieceY = new double[Delays.Length];

	private readonly double[] _pieceVelocity = new double[Delays.Length];

	private readonly int[] _pieceDelay = new int[Delays.Length];

	public ARZCollapsePlatformInstance() => DesignBounds = new Rectanglei(-HalfWidth, -TopHeight, HalfWidth * 2, TopHeight * 2);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZCollapsePlatformType.AnimationGroupResourceKey));
		CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
	}

	protected override void OnUpdate()
	{
		if (_collapsed)
		{
			UpdatePieces();
			return;
		}
		if (_stoodOn)
		{
			if (_counter == 0)
			{
				Collapse();
				return;
			}
			_counter--;
		}
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			_stoodOn |= character.ObjectLink == this;
		}
	}

	private void Collapse()
	{
		_collapsed = true;
		Delays.CopyTo(_pieceDelay, 0);
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(ARZCollapsePlatformType.SoundResourceKey));
	}

	private void UpdatePieces()
	{
		for (int i = 0; i < Delays.Length; i++)
		{
			if (_pieceDelay[i] > 0)
			{
				// The first piece holds the platform up until it falls
				if (--_pieceDelay[i] == 0 && i == 0)
				{
					CollisionVectors = new CollisionVector[0];
				}
				continue;
			}
			_pieceY[i] += _pieceVelocity[i];
			_pieceVelocity[i] += Gravity * Scale / 256.0;
		}
		if (++_ticks > LifeTicks)
		{
			// Without respawn tracking, the original loads the platform again when it comes back in range
			Finish();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (!_collapsed)
		{
			_animation.Index = 0;
			objectRenderer.Render(_animation);
			return;
		}
		for (int i = 0; i < Delays.Length; i++)
		{
			_animation.Index = FirstPiece + i;
			objectRenderer.Render(_animation, new Vector2(0.0, _pieceY[i]));
		}
	}
}
