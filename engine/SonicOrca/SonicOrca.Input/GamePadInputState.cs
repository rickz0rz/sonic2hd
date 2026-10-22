using SonicOrca.Geometry;

namespace SonicOrca.Input;

public struct GamePadInputState
{
	public Vector2i POV { get; set; }

	public Vector2 LeftAxis { get; set; }

	public Vector2 RightAxis { get; set; }

	public bool LeftAxisButton { get; set; }

	public bool RightAxisButton { get; set; }

	public bool North { get; set; }

	public bool East { get; set; }

	public bool South { get; set; }

	public bool West { get; set; }

	public bool Start { get; set; }

	public bool Select { get; set; }

	public bool LeftBumper { get; set; }

	public bool RightBumper { get; set; }

	public double LeftTrigger { get; set; }

	public double RightTrigger { get; set; }

	public static GamePadInputState GetPressed(GamePadInputState prev, GamePadInputState next)
	{
		GamePadInputState result = next;
		result.POV = GetAxisStateChanged(prev.POV, next.POV);
		result.LeftAxis = GetAxisStateChanged(prev.LeftAxis, next.LeftAxis);
		result.RightAxis = GetAxisStateChanged(prev.RightAxis, next.RightAxis);
		result.LeftAxisButton = !prev.LeftAxisButton && next.LeftAxisButton;
		result.RightAxisButton = !prev.RightAxisButton && next.RightAxisButton;
		result.North = !prev.North && next.North;
		result.East = !prev.East && next.East;
		result.South = !prev.South && next.South;
		result.West = !prev.West && next.West;
		result.Start = !prev.Start && next.Start;
		result.Select = !prev.Select && next.Select;
		result.LeftBumper = !prev.LeftBumper && next.LeftBumper;
		result.RightBumper = !prev.RightBumper && next.RightBumper;
		return result;
	}

	public static GamePadInputState GetReleased(GamePadInputState prev, GamePadInputState next)
	{
		GamePadInputState result = next;
		result.POV = GetAxisStateChanged(prev.POV, next.POV);
		result.LeftAxis = GetAxisStateChanged(prev.LeftAxis, next.LeftAxis);
		result.RightAxis = GetAxisStateChanged(prev.RightAxis, next.RightAxis);
		result.LeftAxisButton = prev.LeftAxisButton && !next.LeftAxisButton;
		result.RightAxisButton = prev.RightAxisButton && !next.RightAxisButton;
		result.North = prev.North && !next.North;
		result.East = prev.East && !next.East;
		result.West = prev.West && !next.West;
		result.Start = prev.Start && !next.Start;
		result.Select = prev.Select && !next.Select;
		result.LeftBumper = prev.LeftBumper && !next.LeftBumper;
		result.RightBumper = prev.RightBumper && !next.RightBumper;
		return result;
	}

	private static Vector2i GetAxisStateChanged(Vector2i prev, Vector2i next)
	{
		Vector2i result = next;
		if (prev.X == next.X)
		{
			result.X = 0;
		}
		if (prev.Y == next.Y)
		{
			result.Y = 0;
		}
		return result;
	}

	private static Vector2 GetAxisStateChanged(Vector2 prev, Vector2 next)
	{
		int direction = GetDirection(prev.X);
		int direction2 = GetDirection(prev.Y);
		int direction3 = GetDirection(next.X);
		int direction4 = GetDirection(next.Y);
		Vector2 result = next;
		if (direction == direction3)
		{
			result.X = 0.0;
		}
		if (direction2 == direction4)
		{
			result.Y = 0.0;
		}
		return result;
	}

	private static int GetDirection(double value)
	{
		if (value <= -0.2)
		{
			return -1;
		}
		if (value >= 0.2)
		{
			return 1;
		}
		return 0;
	}
}
