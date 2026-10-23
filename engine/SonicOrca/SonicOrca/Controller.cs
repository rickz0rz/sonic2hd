using SonicOrca.Geometry;
using SonicOrca.Input;

namespace SonicOrca;

/// <summary>
/// The state of one player's controls for one input event type (current, pressed or released).
/// </summary>
/// <remarks>
/// Player 1 also uses the arrow keys with A, S, D and Return. Player 2 also uses I, J, K, L with Q, W, E and Return.
/// </remarks>
public class Controller
{
	private readonly int _index;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly InputStateEventType _eventType;

	public static bool IsDebug { get; set; }

	public Vector2 DirectionLeft { get; private set; }

	public Vector2 DirectionRight { get; private set; }

	public bool Start { get; private set; }

	public bool Action1 { get; private set; }

	public bool Action2 { get; private set; }

	public bool Action3 { get; private set; }

	public double LeftTrigger { get; private set; }

	public double RightTrigger { get; private set; }

	public int Index => _index;

	public Controller(SonicOrcaGameContext gameContext, int index, InputStateEventType eventType)
	{
		_gameContext = gameContext;
		_index = index;
		_eventType = eventType;
	}

	public void Update()
	{
		GamePadInputState gamePad = _gameContext.Input.GetInputState(_eventType).GamePad[_index];
		if (_index == 0)
		{
			KeyboardState keyboard = _gameContext.Input.GetInputState(_eventType).Keyboard;
			UpdateFromGamePadAndKeyboard(gamePad, keyboard,
				KeyboardState.KEY_LEFT, KeyboardState.KEY_RIGHT, KeyboardState.KEY_UP, KeyboardState.KEY_DOWN,
				KeyboardState.KEY_A, KeyboardState.KEY_S, KeyboardState.KEY_D);
		}
		else if (_index == 1)
		{
			KeyboardState keyboard = _gameContext.Input.GetInputState(_eventType).Keyboard;
			UpdateFromGamePadAndKeyboard(gamePad, keyboard,
				KeyboardState.KEY_J, KeyboardState.KEY_L, KeyboardState.KEY_I, KeyboardState.KEY_K,
				KeyboardState.KEY_Q, KeyboardState.KEY_W, KeyboardState.KEY_E);
		}
		// Note: for players 3 and 4, the game pad state is read but not used.
		if (!IsDebug)
		{
			// Outside debug mode, all three action buttons do the same thing.
			Action1 = Action1 || Action2 || Action3;
		}
	}

	private void UpdateFromGamePadAndKeyboard(GamePadInputState gamePad, KeyboardState keyboard,
		int keyLeft, int keyRight, int keyUp, int keyDown, int keyAction1, int keyAction2, int keyAction3)
	{
		Vector2 direction = default;
		if (keyboard.Keys[keyLeft])
		{
			direction.X = -1.0;
		}
		else if (keyboard.Keys[keyRight])
		{
			direction.X = 1.0;
		}
		if (keyboard.Keys[keyUp])
		{
			direction.Y = -1.0;
		}
		else if (keyboard.Keys[keyDown])
		{
			direction.Y = 1.0;
		}
		// The keyboard has priority. Then the left stick, then the directional pad.
		if (direction == default(Vector2))
		{
			direction = gamePad.LeftAxis != default(Vector2) ? gamePad.LeftAxis : (Vector2)gamePad.POV;
		}
		DirectionLeft = direction;
		Action1 = gamePad.West || gamePad.North || keyboard.Keys[keyAction1];
		Action2 = gamePad.South || keyboard.Keys[keyAction2];
		Action3 = gamePad.East || keyboard.Keys[keyAction3];
		Start = gamePad.Start || keyboard.Keys[KeyboardState.KEY_RETURN];
		LeftTrigger = gamePad.LeftTrigger;
		RightTrigger = gamePad.RightTrigger;
		DirectionRight = gamePad.RightAxis;
	}
}
