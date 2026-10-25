using System;
using System.Collections.Generic;
using System.Globalization;
using SonicOrca.Core.Debugging;
using SonicOrca.Geometry;
using SonicOrca.Input;

namespace SonicOrca.Core.Editor;

/// <summary>
/// The mouse and keyboard state of one editor frame. The mouse position is in the 1920 x 1080 screen of the game.
/// The state comes from the input of the game, or from the scripted input of the tests (see
/// <see cref="TestHooks.EditorInput"/>).
/// </summary>
public sealed class EditorInput
{
	private readonly HashSet<int> _keysDown = new HashSet<int>();

	private readonly HashSet<int> _lastKeysDown = new HashSet<int>();

	private readonly HashSet<int> _tappedKeys = new HashSet<int>();

	private bool _lastLeft;

	private bool _lastRight;

	private bool _lastMiddle;

	public Vector2 Mouse { get; private set; }

	public bool Left { get; private set; }

	public bool Right { get; private set; }

	public bool Middle { get; private set; }

	public bool LeftPressed => Left && !_lastLeft;

	public bool RightPressed => Right && !_lastRight;

	public bool MiddlePressed => Middle && !_lastMiddle;

	public bool AnyReleased => (_lastLeft && !Left) || (_lastRight && !Right) || (_lastMiddle && !Middle);

	public double Wheel { get; private set; }

	public string TextInput { get; private set; }

	public bool IsKeyDown(int scancode) => _keysDown.Contains(scancode);

	public bool IsKeyPressed(int scancode) => _keysDown.Contains(scancode) && !_lastKeysDown.Contains(scancode);

	/// <summary>
	/// Takes the keys and buttons that are down when the editor opens, so that they do not count as pressed. For
	/// example, the F2 key that opens the editor does not close it again.
	/// </summary>
	public void Begin(SonicOrcaGameContext gameContext)
	{
		if (TestHooks.EditorInput != null)
		{
			return;
		}
		InputState state = gameContext.Input.CurrentState;
		Left = state.Mouse.Left;
		Right = state.Mouse.Right;
		Middle = state.Mouse.Middle;
		_keysDown.Clear();
		for (int scancode = 0; scancode < state.Keyboard.Count; scancode++)
		{
			if (state.Keyboard[scancode])
			{
				_keysDown.Add(scancode);
			}
		}
	}

	/// <summary>Reads the state of this frame.</summary>
	public void Read(SonicOrcaGameContext gameContext)
	{
		_lastLeft = Left;
		_lastRight = Right;
		_lastMiddle = Middle;
		_lastKeysDown.Clear();
		_lastKeysDown.UnionWith(_keysDown);
		Wheel = 0.0;
		TextInput = null;
		int frame = ++TestHooks.EditorFrame;
		if (TestHooks.EditorInput != null)
		{
			ReadScript(frame);
			return;
		}
		InputState state = gameContext.Input.CurrentState;
		Vector2i clientSize = gameContext.Window.ClientSize;
		if (clientSize.X > 0 && clientSize.Y > 0)
		{
			Mouse = new Vector2(state.Mouse.X * 1920.0 / clientSize.X, state.Mouse.Y * 1080.0 / clientSize.Y);
		}
		Left = state.Mouse.Left;
		Right = state.Mouse.Right;
		Middle = state.Mouse.Middle;
		Wheel = state.Mouse.Wheel;
		TextInput = gameContext.Input.TextInput;
		_keysDown.Clear();
		for (int scancode = 0; scancode < state.Keyboard.Count; scancode++)
		{
			if (state.Keyboard[scancode])
			{
				_keysDown.Add(scancode);
			}
		}
	}

	private void ReadScript(int frame)
	{
		_keysDown.ExceptWith(_tappedKeys);
		_tappedKeys.Clear();
		if (!TestHooks.EditorInput.TryGetValue(frame, out string[] commands))
		{
			return;
		}
		foreach (string command in commands)
		{
			string[] parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			string argument = parts.Length > 1 ? parts[1] : "";
			switch (parts[0].ToLowerInvariant())
			{
				case "mouse":
					string[] xy = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					Mouse = new Vector2(double.Parse(xy[0], CultureInfo.InvariantCulture), double.Parse(xy[1], CultureInfo.InvariantCulture));
					break;
				case "down":
				case "up":
					SetButton(argument, parts[0] == "down");
					break;
				case "wheel":
					Wheel = double.Parse(argument, CultureInfo.InvariantCulture);
					break;
				case "keydown":
					_keysDown.Add(int.Parse(argument, CultureInfo.InvariantCulture));
					break;
				case "keyup":
					_keysDown.Remove(int.Parse(argument, CultureInfo.InvariantCulture));
					break;
				case "tap":
					int scancode = int.Parse(argument, CultureInfo.InvariantCulture);
					_keysDown.Add(scancode);
					_tappedKeys.Add(scancode);
					break;
				case "type":
					TextInput = argument;
					break;
				default:
					throw new FormatException("Unknown editor input command: " + command);
			}
		}
	}

	private void SetButton(string button, bool down)
	{
		switch (button)
		{
			case "left":
				Left = down;
				break;
			case "right":
				Right = down;
				break;
			case "middle":
				Middle = down;
				break;
		}
	}
}
