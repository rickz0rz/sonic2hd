using System;

namespace SonicOrca;

public class Disposable : IDisposable
{
	private readonly Action _action;

	private Disposable(Action action)
	{
		_action = action;
	}

	public void Dispose()
	{
		if (_action != null)
		{
			_action();
		}
	}

	public static IDisposable FromAction(Action action)
	{
		return new Disposable(action);
	}
}
