using System;

namespace SonicOrca.Core.Objects.Base;

public class CharacterInputState
{
	private int _verticalDirection;

	private double _throttle { get; set; }

	public double Throttle
	{
		get
		{
			return _throttle;
		}
		set
		{
			_throttle = MathX.Clamp(-1.0, value, 1.0);
		}
	}

	public int HorizontalDirection
	{
		get
		{
			return Math.Sign(Throttle);
		}
		set
		{
			Throttle = Math.Sign(value);
		}
	}

	public int VerticalDirection
	{
		get
		{
			return _verticalDirection;
		}
		set
		{
			_verticalDirection = Math.Sign(value);
		}
	}

	public CharacterInputButtonState A { get; set; }

	public CharacterInputButtonState B { get; set; }

	public CharacterInputButtonState C { get; set; }

	public CharacterInputButtonState ABC
	{
		get
		{
			if (A == CharacterInputButtonState.Pressed || B == CharacterInputButtonState.Pressed || C == CharacterInputButtonState.Pressed)
			{
				return CharacterInputButtonState.Pressed;
			}
			return (A | B | C) & CharacterInputButtonState.Down;
		}
	}

	public CharacterInputState()
	{
	}

	public CharacterInputState(CharacterInputState state)
	{
		_throttle = state._throttle;
		_verticalDirection = state._verticalDirection;
		A = state.A;
		B = state.B;
		C = state.C;
	}

	public void Clear()
	{
		_throttle = 0.0;
		_verticalDirection = 0;
		A = CharacterInputButtonState.Up;
		B = CharacterInputButtonState.Up;
		C = CharacterInputButtonState.Up;
	}
}
