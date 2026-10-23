using System.Collections.Generic;

namespace SonicOrca;

/// <summary>
/// Runs an update sequence one step for each call to <see cref="Update"/>, and skips calls while a wait is active.
/// </summary>
public class Updater
{
	private readonly IEnumerator<UpdateResult> _updateStateEnumerator;

	private int _updateStateWaitTicks;

	public Updater(IEnumerable<UpdateResult> updateMethod)
	{
		_updateStateEnumerator = updateMethod.GetEnumerator();
	}

	/// <returns><c>false</c> when the update sequence has no more steps.</returns>
	public bool Update()
	{
		if (_updateStateWaitTicks > 0)
		{
			_updateStateWaitTicks--;
			return true;
		}
		if (_updateStateEnumerator.MoveNext())
		{
			UpdateResult current = _updateStateEnumerator.Current;
			if (current.Type == UpdateResultType.Wait)
			{
				_updateStateWaitTicks = current.WaitTicks;
			}
			return true;
		}
		return false;
	}
}
