using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZDRAWBRIDGE;

/// <summary>
/// A drawbridge, as in Sonic 2 (Obj81 in Mystic Cave Zone). It starts as a column of 8 logs above its hinge (below
/// the hinge with the y flip of the original), and the column is a solid wall. When a vine switch sets the trigger of
/// the drawbridge (the subtype), the logs turn around the hinge by one angle step each frame until they are level:
/// to the right, or to the left with the x flip or the y flip (not both). Then the bridge is solid to walk on. As in
/// the original, the solid box stays where the column was while the logs turn.
/// </summary>
public class MCZDrawbridgeInstance : ActiveObject
{
	private const int Scale = 4;

	private const int Logs = 8;

	private const int LogDistance = 0x10;

	/// <summary>The centre of the column or of the bridge is $48 pixels from the hinge.</summary>
	private const int CentreDistance = 0x48;

	/// <summary>The solid box of the column: 8 by $40 pixels from the centre (SolidObject $13 less the $B of the character).</summary>
	private const int ColumnHalfWidth = 8 * Scale;

	private const int ColumnHalfHeight = 0x40 * Scale;

	/// <summary>The solid box of the bridge: $40 by 8 pixels from the centre (SolidObject $4B less the $B of the character).</summary>
	private const int BridgeHalfWidth = 0x40 * Scale;

	private const int BridgeHalfHeight = 8 * Scale;

	private const int LogFrame = 1;

	private AnimationInstance _animation;

	private int _trigger;

	private bool _flipX;

	private bool _flipY;

	private Vector2 _hinge;

	/// <summary>The angle of the logs from the hinge (256 units for each circle).</summary>
	private int _angle;

	private int _speed;

	private bool _turning;

	private bool _down;

	private bool? _columnBox;

	/// <summary>The index of the trigger that lowers the drawbridge (the subtype).</summary>
	[StateVariable]
	private int Trigger
	{
		get => _trigger;
		set => _trigger = value;
	}

	/// <summary>The bridge turns the other way (the x flip of the original).</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	/// <summary>The column hangs below the hinge (the y flip of the original).</summary>
	[StateVariable]
	private bool FlipY
	{
		get => _flipY;
		set => _flipY = value;
	}

	public MCZDrawbridgeInstance() => DesignBounds = new Rectanglei(-0x90 * Scale, -0x90 * Scale, 0x120 * Scale, 0x120 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZDrawbridgeType.AnimationGroupResourceKey));
		_hinge = PositionPrecise;
		_angle = _flipY ? 0x40 : 0xC0;
		_speed = _flipX ? -1 : 1;
		PositionPrecise = _hinge + new Vector2(0.0, (_flipY ? CentreDistance : -CentreDistance) * Scale);
		_columnBox = null;
		UpdateSolidBox();
	}

	protected override void OnUpdate()
	{
		if (_down)
		{
			return;
		}
		if (!_turning && ButtonVineTriggers.For(Level).IsSet(_trigger))
		{
			_turning = true;
			if (_flipX && !_flipY)
			{
				// The original moves the solid box to the left of the hinge here
				PositionPrecise = new Vector2(_hinge.X - CentreDistance * Scale, PositionPrecise.Y);
			}
		}
		if (_turning)
		{
			if (_angle == 0 || _angle == 0x80)
			{
				_turning = false;
				_down = true;
				PositionPrecise = _hinge + new Vector2((_angle == 0 ? CentreDistance : -CentreDistance) * Scale, 0.0);
			}
			else
			{
				_angle = (_angle + _speed) & 0xFF;
			}
		}
		UpdateSolidBox();
	}

	/// <summary>
	/// The column box is for the angles $40 and $C0 to $FF, and the bridge box for the other angles (loc_2A18A).
	/// </summary>
	private void UpdateSolidBox()
	{
		bool column = _angle != 0 && (_angle == 0x40 || _angle >= 0xC0);
		if (column == _columnBox)
		{
			return;
		}
		_columnBox = column;
		CollisionVectors = column
			? CollisionVector.FromRectangle(this, new Rectanglei(-ColumnHalfWidth, -ColumnHalfHeight, ColumnHalfWidth * 2, ColumnHalfHeight * 2))
			: CollisionVector.FromRectangle(this, new Rectanglei(-BridgeHalfWidth, -BridgeHalfHeight, BridgeHalfWidth * 2, BridgeHalfHeight * 2));
		if (Level != null)
		{
			RegisterCollisionUpdate();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = LogFrame;
		for (int i = 1; i <= Logs; i++)
		{
			int distance = i * LogDistance;
			var log = _hinge + new Vector2(distance * OriginalMath.Cos(_angle) / 256.0 * Scale, distance * OriginalMath.Sin(_angle) / 256.0 * Scale);
			objectRenderer.Render(_animation, log - PositionPrecise);
		}
	}
}
