using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZBRIDGE;

/// <summary>
/// A log bridge that opens, as in Sonic 2 (Obj77 in Mystic Cave Zone). The bridge is solid while it is closed. When
/// a vine switch sets the trigger of the bridge (the subtype), the bridge opens once: the logs swing down in the
/// middle over 20 frames, and the characters on it fall. As in the original, the bridge plays its close animation
/// once when it appears.
/// </summary>
public class MCZBridgeInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The solid box: $40 pixels to each side (SolidObject $4B less the $B of the character) and 8 pixels up and down.</summary>
	private const int HalfWidth = 0x40 * Scale;

	private const int HalfHeight = 8 * Scale;

	/// <summary>Each frame of the animations shows for 4 frames (Ani_obj77).</summary>
	private const int FrameTicks = 4;

	/// <summary>The last mapping frame: the bridge is fully open.</summary>
	private const int OpenFrame = 4;

	private AnimationInstance _animation;

	private int _trigger;

	private bool _opening;

	private int _frame = OpenFrame;

	private int _frameTicks;

	private bool _solid;

	/// <summary>The index of the trigger that opens the bridge (the subtype).</summary>
	[StateVariable]
	private int Trigger
	{
		get => _trigger;
		set => _trigger = value;
	}

	public MCZBridgeInstance() => DesignBounds = new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, 0x38 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZBridgeType.AnimationGroupResourceKey));
		_frame = OpenFrame;
		_frameTicks = FrameTicks;
		SetSolid(false);
	}

	protected override void OnUpdate()
	{
		if (!_opening && ButtonVineTriggers.For(Level).IsSet(_trigger))
		{
			_opening = true;
			_frameTicks = FrameTicks;
		}
		// The close animation shows frames 4 to 0 and the open animation frames 0 to 4, then each stays on its last frame
		if (--_frameTicks <= 0)
		{
			_frameTicks = FrameTicks;
			if (_opening && _frame < OpenFrame)
			{
				_frame++;
			}
			else if (!_opening && _frame > 0)
			{
				_frame--;
			}
		}
		bool solid = _frame == 0;
		if (solid != _solid)
		{
			if (!solid)
			{
				DropCharacters();
			}
			SetSolid(solid);
		}
	}

	private void DropCharacters()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
			{
				character.LeaveGround();
				character.IsAirborne = true;
			}
		}
	}

	private void SetSolid(bool solid)
	{
		_solid = solid;
		CollisionVectors = solid
			? CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2))
			: new CollisionVector[0];
		if (Level != null)
		{
			RegisterCollisionUpdate();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
