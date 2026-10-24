using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZFLOORSPIKE;

/// <summary>
/// A floor spike of Metropolis Zone, as in Sonic 2 (Obj6D). The spike comes up $20 pixels at 4 pixels each frame,
/// stays up for 3 frames, and goes back down at 4 pixels each frame. Then it waits until the level frame counter minus
/// the subtype is a multiple of $80, and comes up again. So the subtype gives the time offset of the spike. The spike
/// hurts a character that touches it (collision flags $84: 4 pixels to each side and $10 up and down).
/// </summary>
public class MTZFloorSpikeInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The height of the spike when it is up, in 1/256 pixels.</summary>
	private const int UpOffset = 0x2000;

	private const int Speed = 0x400;

	private const int UpDelay = 3;

	private const int Period = 0x80;

	private const int HalfWidth = 4;

	private const int HalfHeight = 0x10;

	private AnimationInstance _animation;

	private int _phase;

	private Vector2 _start;

	private int _offset;

	private bool _up;

	private bool _waiting;

	private int _delay;

	/// <summary>The time offset of the spike (the subtype in the original).</summary>
	[StateVariable]
	private int Phase
	{
		get => _phase;
		set => _phase = value;
	}

	public MTZFloorSpikeInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, (-HalfHeight - 0x20) * Scale, HalfWidth * 2 * Scale, (HalfHeight * 2 + 0x20) * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZFloorSpikeType.AnimationGroupResourceKey));
		_animation.Index = 0;
		_start = PositionPrecise;
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 1, -HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale)
		};
	}

	protected override void OnUpdate()
	{
		UpdateSpike();
		PositionPrecise = _start - new Vector2(0.0, (_offset >> 8) * Scale);
	}

	private void UpdateSpike()
	{
		if (_delay > 0)
		{
			_delay--;
			return;
		}
		if (_waiting)
		{
			if (((Level.Ticks - _phase) & (Period - 1)) != 0)
			{
				return;
			}
			_waiting = false;
		}
		if (_up)
		{
			// As in the original, the spike stops one frame after it reaches the bottom (the subtraction borrows)
			_offset -= Speed;
			if (_offset < 0)
			{
				_offset = 0;
				_up = false;
				_waiting = true;
			}
		}
		else
		{
			_offset += Speed;
			if (_offset >= UpOffset)
			{
				_offset = UpOffset;
				_up = true;
				_delay = UpDelay;
			}
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != 1 || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		character.Hurt(character.Position.X >= Position.X ? 1 : -1);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
