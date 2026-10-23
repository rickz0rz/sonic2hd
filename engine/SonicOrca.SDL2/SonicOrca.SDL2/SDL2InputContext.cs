#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using SonicOrca.Geometry;
using SonicOrca.Input;

namespace SonicOrca.SDL2;

/// <summary>Keyboard, mouse, text input and up to four game controllers (with rumble), read from SDL2.</summary>
public class SDL2InputContext : InputContext, IDisposable
{
	private const int MaxGamePads = 4;

	// Stick and trigger dead zone: values below MinTolerance read as 0, values above MaxTolerance as full
	private const double AxisMinTolerance = 0.2;

	private const double AxisMaxTolerance = 0.75;

	private const int VibrationDurationMs = 4000;

	private readonly List<IntPtr> _gameControllers = new List<IntPtr>();

	/// <summary>The rumble device of each game controller, or IntPtr.Zero when it has none.</summary>
	private readonly List<IntPtr> _haptic = new List<IntPtr>();

	private readonly SDL.SDL_Event[] _events = new SDL.SDL_Event[512];

	public SDL2InputContext(SDL2Platform platform)
	{
		Trace.WriteLine("Initialising SDL2 joystick");
		if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_JOYSTICK) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise SDL2 joystick subsystem.");
		}
		Trace.WriteLine("Initialising SDL2 game controller");
		if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_GAMECONTROLLER) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise SDL2 game controller subsystem.");
		}
		Trace.WriteLine("Initialising SDL2 haptic");
		if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_HAPTIC) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise SDL2 haptic subsystem.");
		}
		FindGameControllers();
	}

	public void Dispose()
	{
		Trace.WriteLine("Quitting SDL2 haptic");
		SDL.SDL_QuitSubSystem(SDL.SDL_INIT_HAPTIC);
		Trace.WriteLine("Quitting SDL2 game controller");
		SDL.SDL_QuitSubSystem(SDL.SDL_INIT_GAMECONTROLLER);
		Trace.WriteLine("Quitting SDL2 joystick");
		SDL.SDL_QuitSubSystem(SDL.SDL_INIT_JOYSTICK);
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
			IntPtr controller = SDL.SDL_GameControllerOpen(i);
			if (controller == IntPtr.Zero)
			{
				continue;
			}
			_gameControllers.Add(controller);
			IntPtr haptic = SDL.SDL_HapticOpenFromJoystick(SDL.SDL_GameControllerGetJoystick(controller));
			if (haptic != IntPtr.Zero && SDL.SDL_HapticRumbleInit(haptic) != 0)
			{
				haptic = IntPtr.Zero;
			}
			_haptic.Add(haptic);
		}
	}

	private void VibrateController(int index, double left, double right, int duration)
	{
		// SDL2 rumble has one strength, so use the mean of the two motors
		if (index < _haptic.Count && _haptic[index] != IntPtr.Zero)
		{
			SDL.SDL_HapticRumblePlay(_haptic[index], (float)((left + right) / 2.0), (uint)duration);
		}
	}

	public unsafe override void UpdateCurrentState()
	{
		int wheel = 0;
		TextInput = null;
		SDL.SDL_StartTextInput();
		// SDL2WindowContext.Update pumped and peeked the events of this frame. Read them, then remove them all.
		int eventCount = SDL.SDL_PeepEvents(_events, _events.Length, SDL.SDL_eventaction.SDL_PEEKEVENT, SDL.SDL_EventType.SDL_FIRSTEVENT, SDL.SDL_EventType.SDL_LASTEVENT);
		for (int i = 0; i < eventCount; i++)
		{
			SDL.SDL_Event e = _events[i];
			switch (e.type)
			{
				case SDL.SDL_EventType.SDL_MOUSEWHEEL:
					wheel = e.wheel.y;
					break;
				case SDL.SDL_EventType.SDL_TEXTINPUT:
					// A null-terminated UTF-8 string in a 32-byte buffer
					TextInput = Marshal.PtrToStringUTF8((IntPtr)e.text.text);
					break;
			}
		}
		while (SDL.SDL_PollEvent(out _) != 0)
		{
		}

		IntPtr keyStates = SDL.SDL_GetKeyboardState(out int keyCount);
		bool[] keys = new bool[keyCount];
		for (int i = 0; i < keyCount; i++)
		{
			keys[i] = Marshal.ReadByte(keyStates, i) != 0;
		}
		uint buttons = SDL.SDL_GetMouseState(out int x, out int y);
		MouseState mouseState = new MouseState
		{
			X = x,
			Y = y,
			Left = (buttons & SDL.SDL_BUTTON_LMASK) != 0,
			Middle = (buttons & SDL.SDL_BUTTON_MMASK) != 0,
			Right = (buttons & SDL.SDL_BUTTON_RMASK) != 0,
			Wheel = wheel
		};

		GamePadInputState[] gamePads = new GamePadInputState[MaxGamePads];
		for (int i = 0; i < Math.Min(_gameControllers.Count, MaxGamePads); i++)
		{
			gamePads[i] = ReadGamePad(_gameControllers[i]);
		}
		CurrentState = new InputState(mouseState, new KeyboardState(keys), gamePads);

		GamePadOutputState[] outputs = OutputState.GamePad;
		if (IsVibrationEnabled && outputs != null)
		{
			for (int i = 0; i < outputs.Length; i++)
			{
				VibrateController(i, outputs[i].LeftVibration, outputs[i].RightVibration, VibrationDurationMs);
			}
		}
	}

	private GamePadInputState ReadGamePad(IntPtr controller)
	{
		GamePadInputState state = default;
		state.South = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A);
		state.East = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B);
		state.West = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X);
		state.North = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y);
		state.Start = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START);
		state.Select = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK);
		state.LeftBumper = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER);
		state.RightBumper = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER);
		state.LeftAxisButton = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK);
		state.RightAxisButton = IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK);
		Vector2i dpad = default;
		if (IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT))
		{
			dpad.X = -1;
		}
		if (IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT))
		{
			dpad.X = 1;
		}
		if (IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP))
		{
			dpad.Y = -1;
		}
		if (IsGameControllerButtonDown(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN))
		{
			dpad.Y = 1;
		}
		state.POV = dpad;
		state.LeftAxis = GetGameControllerAxis(controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);
		state.RightAxis = GetGameControllerAxis(controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);
		state.LeftTrigger = GetGameControllerAxis(controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
		state.RightTrigger = GetGameControllerAxis(controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);
		return state;
	}

	private bool IsGameControllerButtonDown(IntPtr controller, SDL.SDL_GameControllerButton button)
	{
		return SDL.SDL_GameControllerGetButton(controller, button) != 0;
	}

	private Vector2 GetGameControllerAxis(IntPtr controller, SDL.SDL_GameControllerAxis x, SDL.SDL_GameControllerAxis y)
	{
		return new Vector2(GetGameControllerAxis(controller, x), GetGameControllerAxis(controller, y));
	}

	/// <summary>The axis position from -1 to 1, with the dead zone removed and the range in between rescaled.</summary>
	private double GetGameControllerAxis(IntPtr controller, SDL.SDL_GameControllerAxis axis)
	{
		double value = SDL.SDL_GameControllerGetAxis(controller, axis) / 32767.0;
		double magnitude = Math.Abs(value);
		if (magnitude < AxisMinTolerance)
		{
			return 0.0;
		}
		if (magnitude > AxisMaxTolerance)
		{
			return Math.Sign(value);
		}
		return (magnitude - AxisMinTolerance) / (AxisMaxTolerance - AxisMinTolerance) * Math.Sign(value);
	}

	public override char GetKeyCode(int scancode)
	{
		return (char)SDL.SDL_GetKeyFromScancode((SDL.SDL_Scancode)scancode);
	}
}
