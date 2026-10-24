using System.Runtime.CompilerServices;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.WFZTHRUST;

/// <summary>
/// The offsets of the background of Wing Fortress Zone (Camera_BG_X_offset and Camera_BG_Y_offset), where Eggman's
/// getaway ship is. The level events of the original (LevEvents_WFZ_Routine3) start to move the ship when the camera
/// reaches x $2BC0 and y $580. Then the x offset increases by 2 each frame up to $800. From x offset $600, the y
/// offset also increases, faster on each frame (WFZ_BG_Y_Speed). When the player holds on to the ship at the end of
/// the act, <see cref="Leave"/> starts LevEvents_WFZ_Routine4: the x offset decreases by 2 each frame down to -$2C0,
/// and the y offset continues to increase, first slower and then by 1 each frame. This class copies those events,
/// so that all the objects of a level that follow the ship read the same values. The area can set the x offset
/// instead with <see cref="Set"/>.
/// </summary>
public sealed class WFZShipOffset
{
	private const int Scale = 4;

	private const int StartCameraX = 0x2BC0;

	private const int StartCameraY = 0x580;

	/// <summary>Half the size of the screen of the original, from the centre of the camera to its top left.</summary>
	private const int HalfScreenWidth = 160;

	private const int HalfScreenHeight = 112;

	private const int MaximumOffset = 0x800;

	/// <summary>The y offset starts to increase when the x offset reaches this value.</summary>
	private const int RiseOffset = 0x600;

	/// <summary>WFZ_BG_Y_Speed increases by 4 each frame up to $840, in 1/256 pixels.</summary>
	private const int RiseAcceleration = 4;

	private const int MaximumRiseSpeed = 0x840;

	/// <summary>LevEvents_WFZ_Routine4 stops at this x offset, and stops the y offset at $1B81.</summary>
	private const int LeaveOffset = -0x2C0;

	private const int LeaveMaximumOffsetY = 0x1B81;

	private static readonly ConditionalWeakTable<Level, WFZShipOffset> All = new ConditionalWeakTable<Level, WFZShipOffset>();

	private int _ticks = -1;

	private bool _moving;

	private bool _leaving;

	private bool _external;

	private int _offset;

	private int _offsetY;

	private int _riseSpeed;

	/// <summary>Gives the x offset for the current frame, in original pixels.</summary>
	public static int Get(Level level) => For(level).Update(level)._offset;

	/// <summary>Gives the y offset for the current frame, in original pixels.</summary>
	public static int GetY(Level level) => For(level).Update(level)._offsetY;

	/// <summary>Sets the x offset, in original pixels, for example from the level events of the area. The copy of the events then stops.</summary>
	public static void Set(Level level, int offset)
	{
		WFZShipOffset shipOffset = For(level);
		shipOffset._external = true;
		shipOffset._offset = offset;
	}

	/// <summary>The ship starts to move now if it has not (LevEvents_WFZ_Routine2), for example when the end starts.</summary>
	public static void Start(Level level)
	{
		For(level).Update(level)._moving = true;
	}

	/// <summary>The ship flies away with the player (LevEvents_WFZ_Routine4).</summary>
	public static void Leave(Level level)
	{
		WFZShipOffset shipOffset = For(level).Update(level);
		shipOffset._moving = true;
		shipOffset._leaving = true;
	}

	private static WFZShipOffset For(Level level) => All.GetValue(level, l => new WFZShipOffset());

	private WFZShipOffset Update(Level level)
	{
		if (_external || level.Ticks == _ticks)
		{
			return this;
		}
		if (level.Ticks < _ticks)
		{
			// The level started again
			_moving = false;
			_leaving = false;
			_offset = 0;
			_offsetY = 0;
			_riseSpeed = 0;
		}
		// The offsets change once for each frame, also on the frames where no object reads them
		int frames = _ticks < 0 ? 1 : level.Ticks - _ticks;
		_ticks = level.Ticks;
		for (int i = 0; i < frames; i++)
		{
			UpdateFrame(level);
		}
		return this;
	}

	private void UpdateFrame(Level level)
	{
		if (_leaving)
		{
			UpdateLeave();
		}
		else if (_moving)
		{
			UpdateMove();
		}
		else
		{
			// The camera of SonicOrca is wider than the screen of the original, so compare the centres
			Vector2 centre = level.Camera.Bounds.Centre;
			int cameraX = (int)centre.X / Scale - HalfScreenWidth;
			int cameraY = (int)centre.Y / Scale - HalfScreenHeight;
			_moving = cameraX >= StartCameraX && cameraY >= StartCameraY;
		}
	}

	/// <summary>LevEvents_WFZ_Routine3.</summary>
	private void UpdateMove()
	{
		if (_offset != MaximumOffset)
		{
			_offset += 2;
		}
		if (_offset < RiseOffset)
		{
			return;
		}
		if (_riseSpeed < MaximumRiseSpeed)
		{
			_riseSpeed += RiseAcceleration;
		}
		_offsetY += _riseSpeed >> 8;
	}

	/// <summary>LevEvents_WFZ_Routine4.</summary>
	private void UpdateLeave()
	{
		if (_offset == LeaveOffset)
		{
			return;
		}
		_offset -= 2;
		if (_offsetY == LeaveMaximumOffsetY)
		{
			return;
		}
		int rise = 0;
		if (_riseSpeed != 0)
		{
			_riseSpeed -= RiseAcceleration;
			rise = _riseSpeed >> 8;
		}
		_offsetY += rise + 1;
	}
}
