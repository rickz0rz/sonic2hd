// Decompiled with JetBrains decompiler
// Type: SonicOrca.FiniteStateMachine
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;

#nullable disable
namespace SonicOrca;

internal class FiniteStateMachine
{
	private FiniteStateMachine.State _currentState;
	private bool _reInitialised;

	public FiniteStateMachine.IState Begin()
	{
		_reInitialised = true;
		return _currentState = new FiniteStateMachine.State(this);
	}

	public void Finish()
	{
		_reInitialised = true;
		_currentState = null;
	}

	public bool Update()
	{
		if (_currentState == null)
			return false;
		_reInitialised = false;
		FiniteStateMachine.State state = _currentState.Update();
		if (!_reInitialised)
			_currentState = state;
		return true;
	}

	public interface IState
	{
		FiniteStateMachine.IState Do(Action action);

		FiniteStateMachine.IState While(Func<bool> condition);

		FiniteStateMachine.IState While(Func<bool> condition, Action action);

		FiniteStateMachine.IState Wait(int ticks);
	}

	private class State : FiniteStateMachine.IState
	{
		private readonly FiniteStateMachine _finiteStateMachine;
		private readonly Action _action;
		private FiniteStateMachine.State _next;
		private bool _finished;

		public State(FiniteStateMachine finiteStateMachine)
		{
			_finiteStateMachine = finiteStateMachine;
		}

		private State(FiniteStateMachine finiteStateMachine, Action action)
		{
			_finiteStateMachine = finiteStateMachine;
			_action = action;
		}

		public FiniteStateMachine.State Update()
		{
			if (_finiteStateMachine._reInitialised)
				return null;
			if (_action == null)
				return _next == null ? null : _next.Update();
			_action();
			if (!_finished)
				return this;
			return _next != null ? _next.Update() : null;
		}

		public FiniteStateMachine.IState Do(Action action)
		{
			FiniteStateMachine.State state1 = new FiniteStateMachine.State(_finiteStateMachine, action);
			state1._finished = true;
			FiniteStateMachine.State state2 = state1;
			_next = state1;
			return state2;
		}

		public FiniteStateMachine.IState While(Func<bool> condition)
		{
			return _next = new FiniteStateMachine.State(_finiteStateMachine, () =>
			{
				if (condition())
					return;
				_next._finished = true;
			});
		}

		public FiniteStateMachine.IState While(Func<bool> condition, Action action)
		{
			return _next = new FiniteStateMachine.State(_finiteStateMachine, () =>
			{
				if (condition())
					action();
				else
					_next._finished = true;
			});
		}

		public FiniteStateMachine.IState Wait(int ticks)
		{
			return _next = new FiniteStateMachine.State(_finiteStateMachine, () =>
			{
				if (ticks-- > 0)
					return;
				_next._finished = true;
			});
		}
	}
}
