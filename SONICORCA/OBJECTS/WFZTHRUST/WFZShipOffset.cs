using System.Runtime.CompilerServices;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.WFZTHRUST;

/// <summary>
/// The x offset of the background of Wing Fortress Zone (Camera_BG_X_offset), where Eggman's getaway ship is. The
/// level events of the original (LevEvents_WFZ_Routine3) start to move the ship when the camera reaches x $2BC0 and y
/// $580. Then the offset increases by 2 each frame up to $800. This class copies those events, so that all the objects
/// of a level that follow the ship read the same value. The area can set the offset instead with <see cref="Set"/>.
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

	private static readonly ConditionalWeakTable<Level, WFZShipOffset> All = new ConditionalWeakTable<Level, WFZShipOffset>();

	private int _ticks = -1;

	private bool _moving;

	private bool _external;

	private int _offset;

	/// <summary>Gives the offset for the current frame, in original pixels.</summary>
	public static int Get(Level level) => All.GetValue(level, l => new WFZShipOffset()).Update(level);

	/// <summary>Sets the offset, in original pixels, for example from the level events of the area. The copy of the events then stops.</summary>
	public static void Set(Level level, int offset)
	{
		WFZShipOffset shipOffset = All.GetValue(level, l => new WFZShipOffset());
		shipOffset._external = true;
		shipOffset._offset = offset;
	}

	private int Update(Level level)
	{
		if (_external || level.Ticks == _ticks)
		{
			return _offset;
		}
		if (level.Ticks < _ticks)
		{
			// The level started again
			_moving = false;
			_offset = 0;
		}
		_ticks = level.Ticks;
		if (_moving)
		{
			if (_offset != MaximumOffset)
			{
				_offset += 2;
			}
		}
		else
		{
			// The camera of SonicOrca is wider than the screen of the original, so compare the centres
			Vector2 centre = level.Camera.Bounds.Centre;
			int cameraX = (int)centre.X / Scale - HalfScreenWidth;
			int cameraY = (int)centre.Y / Scale - HalfScreenHeight;
			_moving = cameraX >= StartCameraX && cameraY >= StartCameraY;
		}
		return _offset;
	}
}
