using System;

namespace SonicOrca;

/// <summary>
/// Runs an action when it is disposed.
/// </summary>
public class Disposable : IDisposable
{
	private readonly Action _action;

	private Disposable(Action action)
	{
		_action = action;
	}

	public void Dispose()
	{
		_action?.Invoke();
	}

	public static IDisposable FromAction(Action action)
	{
		return new Disposable(action);
	}
}
