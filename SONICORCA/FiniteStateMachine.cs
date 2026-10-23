using System;

#nullable disable
namespace SonicOrca;

/// <summary>
/// A small state machine that runs a chain of steps, one step for each call to <see cref="Update"/>.
/// <see cref="Begin"/> starts a new chain, and the <see cref="IState"/> methods add steps to it.
/// Calling <see cref="Begin"/> or <see cref="Finish"/> from a step stops the current chain at once.
/// </summary>
internal class FiniteStateMachine
{
	private State _currentState;
	/// <summary>True when <see cref="Begin"/> or <see cref="Finish"/> was called during the current update.</summary>
	private bool _reInitialised;

	public IState Begin()
	{
		_reInitialised = true;
		return _currentState = new State(this);
	}

	public void Finish()
	{
		_reInitialised = true;
		_currentState = null;
	}

	/// <summary>Runs the current step. Returns false when the chain is finished.</summary>
	public bool Update()
	{
		if (_currentState == null)
			return false;
		_reInitialised = false;
		State state = _currentState.Update();
		if (!_reInitialised)
			_currentState = state;
		return true;
	}

	/// <summary>A step of the chain. Each method adds a step after this one and returns it.</summary>
	public interface IState
	{
		/// <summary>Adds a step that runs the action once.</summary>
		IState Do(Action action);

		/// <summary>Adds a step that waits until the condition is false.</summary>
		IState While(Func<bool> condition);

		/// <summary>Adds a step that runs the action on each update while the condition is true.</summary>
		IState While(Func<bool> condition, Action action);

		/// <summary>Adds a step that waits for about the given number of updates.</summary>
		IState Wait(int ticks);
	}

	/// <summary>
	/// One step of a chain. The first step of a chain has no action. A step is finished when
	/// _finished is true after its action ran.
	/// </summary>
	private class State : IState
	{
		private readonly FiniteStateMachine _finiteStateMachine;
		private readonly Action _action;
		private State _next;
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

		/// <summary>Runs this step, and returns the step to run on the next update (null when the chain ends).</summary>
		public State Update()
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

		public IState Do(Action action)
		{
			State state = new State(_finiteStateMachine, action);
			state._finished = true;
			_next = state;
			return state;
		}

		public IState While(Func<bool> condition)
		{
			return _next = new State(_finiteStateMachine, () =>
			{
				if (condition())
					return;
				_next._finished = true;
			});
		}

		public IState While(Func<bool> condition, Action action)
		{
			return _next = new State(_finiteStateMachine, () =>
			{
				if (condition())
					action();
				else
					_next._finished = true;
			});
		}

		public IState Wait(int ticks)
		{
			return _next = new State(_finiteStateMachine, () =>
			{
				if (ticks-- > 0)
					return;
				_next._finished = true;
			});
		}
	}
}
