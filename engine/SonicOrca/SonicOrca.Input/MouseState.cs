using SonicOrca.Geometry;

namespace SonicOrca.Input;

public struct MouseState
{
	public int X { get; set; }

	public int Y { get; set; }

	public double Wheel { get; set; }

	public bool Left { get; set; }

	public bool Middle { get; set; }

	public bool Right { get; set; }

	public Vector2i Position => new Vector2i(X, Y);

	public static MouseState GetPressed(MouseState previousState, MouseState nextState)
	{
		MouseState result = nextState;
		result.Left = !previousState.Left && nextState.Left;
		result.Middle = !previousState.Middle && nextState.Middle;
		result.Right = !previousState.Right && nextState.Right;
		return result;
	}

	public static MouseState GetReleased(MouseState previousState, MouseState nextState)
	{
		MouseState result = nextState;
		result.Left = previousState.Left && !nextState.Left;
		result.Middle = previousState.Middle && !nextState.Middle;
		result.Right = previousState.Right && !nextState.Right;
		return result;
	}
}
