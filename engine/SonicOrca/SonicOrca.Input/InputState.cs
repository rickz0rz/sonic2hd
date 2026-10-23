using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Input;

/// <summary>
/// The mouse, keyboard and game pad states for one frame.
/// </summary>
public class InputState
{
	private const int GamePadCount = 4;

	private readonly MouseState _mouseState;

	private readonly KeyboardState _keyboardState;

	private readonly GamePadInputState[] _gamePadInputState;

	public MouseState Mouse => _mouseState;

	public KeyboardState Keyboard => _keyboardState;

	public IReadOnlyList<GamePadInputState> GamePad => _gamePadInputState;

	public InputState()
	{
		_mouseState = default;
		_keyboardState = new KeyboardState();
		_gamePadInputState = new GamePadInputState[GamePadCount];
	}

	public InputState(MouseState mouseState, KeyboardState keyboardState, GamePadInputState[] gamepadInputState)
	{
		_mouseState = mouseState;
		_keyboardState = keyboardState;
		_gamePadInputState = gamepadInputState;
	}

	public static InputState GetPressed(InputState previousState, InputState nextState)
	{
		return new InputState(
			MouseState.GetPressed(previousState.Mouse, nextState.Mouse),
			KeyboardState.GetPressed(previousState.Keyboard, nextState.Keyboard),
			Enumerable.Range(0, GamePadCount).Select(x => GamePadInputState.GetPressed(previousState.GamePad[x], nextState.GamePad[x])).ToArray());
	}

	public static InputState GetReleased(InputState previousState, InputState nextState)
	{
		return new InputState(
			MouseState.GetReleased(previousState.Mouse, nextState.Mouse),
			KeyboardState.GetReleased(previousState.Keyboard, nextState.Keyboard),
			Enumerable.Range(0, GamePadCount).Select(x => GamePadInputState.GetReleased(previousState.GamePad[x], nextState.GamePad[x])).ToArray());
	}
}
