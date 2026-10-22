using System;

namespace SonicOrca.Input;

public abstract class InputContext
{
	public InputState LastState { get; protected set; }

	public InputState CurrentState { get; protected set; }

	public InputState Pressed { get; private set; }

	public InputState Released { get; private set; }

	public OutputState OutputState { get; set; }

	public bool IsVibrationEnabled { get; set; }

	public string TextInput { get; protected set; }

	protected InputContext()
	{
		LastState = new InputState();
		CurrentState = new InputState();
		Pressed = new InputState();
		Released = new InputState();
		OutputState = new OutputState();
	}

	public virtual void UpdateCurrentState()
	{
	}

	public void Update()
	{
		CurrentState = new InputState();
		UpdateCurrentState();
		OutputState = new OutputState();
	}

	public void UpdatePressedReleased()
	{
		Pressed = InputState.GetPressed(LastState, CurrentState);
		Released = InputState.GetReleased(LastState, CurrentState);
		LastState = CurrentState;
	}

	public InputState GetInputState(InputStateEventType eventType)
	{
		return eventType switch
		{
			InputStateEventType.Current => CurrentState, 
			InputStateEventType.Pressed => Pressed, 
			InputStateEventType.Released => Released, 
			_ => throw new ArgumentException("Invalid event type", "eventType"), 
		};
	}

	public abstract char GetKeyCode(int scancode);
}
