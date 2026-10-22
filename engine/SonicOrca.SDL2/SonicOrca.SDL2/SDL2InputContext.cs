#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;
using SonicOrca.Geometry;
using SonicOrca.Input;

namespace SonicOrca.SDL2;

public class SDL2InputContext : InputContext, IDisposable
{
	private readonly SDL2Platform _platform;

	private readonly IntPtr _windowHandle;

	private readonly List<IntPtr> _gameControllers = new List<IntPtr>();

	private readonly List<IntPtr> _haptic = new List<IntPtr>();

	private SDL.SDL_Event[] events = new SDL.SDL_Event[512];

	public SDL2InputContext(SDL2Platform platform)
	{
		_platform = platform;
		_windowHandle = ((SDL2WindowContext)_platform.Window).WindowHandle;
		Trace.WriteLine("Initialising SDL2 joystick");
		if (SDL.SDL_InitSubSystem(512u) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise SDL2 joystick subsystem.");
		}
		Trace.WriteLine("Initialising SDL2 game controller");
		if (SDL.SDL_InitSubSystem(8192u) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise SDL2 game controller subsystem.");
		}
		Trace.WriteLine("Initialising SDL2 haptic");
		if (SDL.SDL_InitSubSystem(4096u) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise SDL2 haptic subsystem.");
		}
		FindGameControllers();
	}

	public void Dispose()
	{
		Trace.WriteLine("Quitting SDL2 haptic");
		SDL.SDL_QuitSubSystem(4096u);
		Trace.WriteLine("Quitting SDL2 game controller");
		SDL.SDL_QuitSubSystem(8192u);
		Trace.WriteLine("Quitting SDL2 joystick");
		SDL.SDL_QuitSubSystem(512u);
	}

	private void FindGameControllers()
	{
		_gameControllers.Clear();
		for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
		{
			if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_FALSE)
			{
				continue;
			}
			IntPtr intPtr = SDL.SDL_GameControllerOpen(i);
			if (intPtr != IntPtr.Zero)
			{
				IntPtr zero = IntPtr.Zero;
				IntPtr joystick = SDL.SDL_GameControllerGetJoystick(intPtr);
				_gameControllers.Add(intPtr);
				zero = SDL.SDL_HapticOpenFromJoystick(joystick);
				if (zero != IntPtr.Zero && SDL.SDL_HapticRumbleInit(zero) != 0)
				{
					zero = IntPtr.Zero;
				}
				_haptic.Add(zero);
			}
		}
	}

	private void VibrateController(int index, double left, double right, int duration)
	{
		if (_haptic.Count > index)
		{
			IntPtr intPtr = _haptic[index];
			if (!(intPtr == IntPtr.Zero))
			{
				SDL.SDL_HapticRumblePlay(intPtr, (float)((left + right) / 2.0), (uint)duration);
			}
		}
	}

	public unsafe override void UpdateCurrentState()
	{
		int num = 0;
		TextInput = null;
		SDL.SDL_StartTextInput();
		int num2 = SDL.SDL_PeepEvents(events, events.Length, SDL.SDL_eventaction.SDL_PEEKEVENT, SDL.SDL_EventType.SDL_FIRSTEVENT, SDL.SDL_EventType.SDL_LASTEVENT);
		SDL.SDL_Event _event;
		for (int i = 0; i < num2; i++)
		{
			_event = events[i];
			switch (_event.type)
			{
			case SDL.SDL_EventType.SDL_MOUSEWHEEL:
				num = _event.wheel.y;
				break;
			case SDL.SDL_EventType.SDL_TEXTINPUT:
			{
				IntPtr ptr = (IntPtr)_event.text.text;
				byte[] array = new byte[32];
				int j;
				for (j = 0; j < 32 && (array[j] = Marshal.ReadByte(ptr, j)) != 0; j++)
				{
				}
				TextInput = Encoding.UTF8.GetString(array, 0, j);
				break;
			}
			}
		}
		while (SDL.SDL_PollEvent(out _event) != 0)
		{
		}
		IntPtr ptr2 = SDL.SDL_GetKeyboardState(out var numkeys);
		bool[] array2 = new bool[numkeys];
		for (int k = 0; k < numkeys; k++)
		{
			array2[k] = Marshal.ReadByte(ptr2, k) != 0;
		}
		uint num3 = SDL.SDL_GetMouseState(out var x, out var y);
		SDL.SDL_GetWindowSize(_windowHandle, out var _, out var _);
		MouseState mouseState = new MouseState
		{
			X = x,
			Y = y,
			Left = ((num3 & SDL.SDL_BUTTON_LMASK) != 0),
			Middle = ((num3 & SDL.SDL_BUTTON_MMASK) != 0),
			Right = ((num3 & SDL.SDL_BUTTON_RMASK) != 0),
			Wheel = num
		};
		KeyboardState keyboardState = new KeyboardState(array2);
		GamePadInputState[] array3 = Enumerable.Range(0, 4).Select((Func<int, GamePadInputState>)((int num6) => default)).ToArray();
		for (int num4 = 0; num4 < _gameControllers.Count; num4++)
		{
			IntPtr intPtr = _gameControllers[num4];
			SDL.SDL_GameControllerGetJoystick(intPtr);
			GamePadInputState gamePadInputState = array3[num4];
			double minTolerance = 0.2;
			double maxTolerance = 0.75;
			gamePadInputState.South = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A);
			gamePadInputState.East = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B);
			gamePadInputState.West = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X);
			gamePadInputState.North = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y);
			gamePadInputState.Start = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START);
			gamePadInputState.Select = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK);
			gamePadInputState.LeftBumper = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER);
			gamePadInputState.RightBumper = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER);
			gamePadInputState.LeftAxisButton = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK);
			gamePadInputState.RightAxisButton = IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK);
			Vector2i pOV = default;
			if (IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT))
			{
				pOV.X = -1;
			}
			if (IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT))
			{
				pOV.X = 1;
			}
			if (IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP))
			{
				pOV.Y = -1;
			}
			if (IsGameControllerButtonDown(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN))
			{
				pOV.Y = 1;
			}
			gamePadInputState.POV = pOV;
			gamePadInputState.LeftAxis = GetGameControllerAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, minTolerance, maxTolerance);
			gamePadInputState.RightAxis = GetGameControllerAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY, minTolerance, maxTolerance);
			gamePadInputState.LeftTrigger = GetGameControllerAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT, minTolerance, maxTolerance);
			gamePadInputState.RightTrigger = GetGameControllerAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT, minTolerance, maxTolerance);
			array3[num4] = gamePadInputState;
		}
		CurrentState = new InputState(mouseState, keyboardState, array3);
		GamePadOutputState[] gamePad = OutputState.GamePad;
		if (IsVibrationEnabled && OutputState.GamePad != null)
		{
			for (int num5 = 0; num5 < gamePad.Length; num5++)
			{
				VibrateController(num5, gamePad[num5].LeftVibration, gamePad[num5].RightVibration, 4000);
			}
		}
	}

	private bool IsGameControllerButtonDown(IntPtr controller, SDL.SDL_GameControllerButton button)
	{
		return SDL.SDL_GameControllerGetButton(controller, button) != 0;
	}

	private Vector2 GetGameControllerAxis(IntPtr controller, SDL.SDL_GameControllerAxis x, SDL.SDL_GameControllerAxis y, double minTolerance, double maxTolerance)
	{
		return new Vector2(GetGameControllerAxis(controller, x, minTolerance, maxTolerance), GetGameControllerAxis(controller, y, minTolerance, maxTolerance));
	}

	private double GetGameControllerAxis(IntPtr controller, SDL.SDL_GameControllerAxis axis, double minTolerance, double maxTolerance)
	{
		double value = (double)SDL.SDL_GameControllerGetAxis(controller, axis) / 32767.0;
		double num = Math.Abs(value);
		if (num < minTolerance)
		{
			return 0.0;
		}
		if (num > maxTolerance)
		{
			return 1.0 * (double)Math.Sign(value);
		}
		return (num - minTolerance) / (maxTolerance - minTolerance) * (double)Math.Sign(value);
	}

	public override char GetKeyCode(int scancode)
	{
		return (char)SDL.SDL_GetKeyFromScancode((SDL.SDL_Scancode)scancode);
	}
}
