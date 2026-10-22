using System.Collections.Generic;

namespace SonicOrca;

public class Updater
{
	private readonly IEnumerator<UpdateResult> _updateStateEnumerator;

	private int _updateStateWaitTicks;

	public Updater(IEnumerable<UpdateResult> updateMethod)
	{
		_updateStateEnumerator = updateMethod.GetEnumerator();
	}

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
			UpdateResultType type = current.Type;
			if (type == UpdateResultType.Next || type != UpdateResultType.Wait)
			{
				return true;
			}
			_updateStateWaitTicks = current.WaitTicks;
			return true;
		}
		return false;
	}
}
