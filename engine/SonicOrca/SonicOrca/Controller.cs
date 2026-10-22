using SonicOrca.Geometry;
using SonicOrca.Input;

namespace SonicOrca;

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
		GamePadInputState gamePadInputState = _gameContext.Input.GetInputState(_eventType).GamePad[_index];
		if (_index == 0)
		{
			KeyboardState keyboard = _gameContext.Input.GetInputState(_eventType).Keyboard;
			Vector2 vector = default;
			if (keyboard.Keys[80])
			{
				vector.X = -1.0;
			}
			else if (keyboard.Keys[79])
			{
				vector.X = 1.0;
			}
			if (keyboard.Keys[82])
			{
				vector.Y = -1.0;
			}
			else if (keyboard.Keys[81])
			{
				vector.Y = 1.0;
			}
			if (vector == default(Vector2))
			{
				vector = ((!(gamePadInputState.LeftAxis == default(Vector2))) ? gamePadInputState.LeftAxis : ((Vector2)gamePadInputState.POV));
			}
			DirectionLeft = vector;
			Action1 = gamePadInputState.West || gamePadInputState.North;
			Action2 = gamePadInputState.South;
			Action3 = gamePadInputState.East;
			Action1 = Action1 || keyboard.Keys[4];
			Action2 = Action2 || keyboard.Keys[22];
			Action3 = Action3 || keyboard.Keys[7];
			Start = gamePadInputState.Start;
			Start = Start || keyboard.Keys[40];
			LeftTrigger = gamePadInputState.LeftTrigger;
			RightTrigger = gamePadInputState.RightTrigger;
			DirectionRight = gamePadInputState.RightAxis;
		}
		else if (_index == 1)
		{
			KeyboardState keyboard2 = _gameContext.Input.GetInputState(_eventType).Keyboard;
			Vector2 vector2 = default;
			if (keyboard2.Keys[13])
			{
				vector2.X = -1.0;
			}
			else if (keyboard2.Keys[15])
			{
				vector2.X = 1.0;
			}
			if (keyboard2.Keys[12])
			{
				vector2.Y = -1.0;
			}
			else if (keyboard2.Keys[14])
			{
				vector2.Y = 1.0;
			}
			if (vector2 == default(Vector2))
			{
				vector2 = ((!(gamePadInputState.LeftAxis == default(Vector2))) ? gamePadInputState.LeftAxis : ((Vector2)gamePadInputState.POV));
			}
			DirectionLeft = vector2;
			Action1 = gamePadInputState.West || gamePadInputState.North;
			Action2 = gamePadInputState.South;
			Action3 = gamePadInputState.East;
			DirectionLeft = vector2;
			Action1 = gamePadInputState.West || gamePadInputState.North;
			Action2 = gamePadInputState.South;
			Action3 = gamePadInputState.East;
			Action1 = Action1 || keyboard2.Keys[20];
			Action2 = Action2 || keyboard2.Keys[26];
			Action3 = Action3 || keyboard2.Keys[8];
			Start = gamePadInputState.Start;
			Start = Start || keyboard2.Keys[40];
			LeftTrigger = gamePadInputState.LeftTrigger;
			RightTrigger = gamePadInputState.RightTrigger;
			DirectionRight = gamePadInputState.RightAxis;
		}
		if (!IsDebug)
		{
			Action1 = Action1 || Action2 || Action3;
		}
	}
}
