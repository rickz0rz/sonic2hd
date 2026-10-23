using System.Collections.Generic;
using SonicOrca;

namespace S2HD;

/// <summary>
/// Runs a set of effect coroutines, one step on each call to <see cref="Update"/>.
/// The title screen uses it for its animated effects.
/// </summary>
internal class EffectEventManager
{
	private readonly List<Updater> _runningEvents = new List<Updater>();

	public void Clear() => _runningEvents.Clear();

	/// <summary>Runs the first step of the effect now, and keeps the effect if it has more steps.</summary>
	public void BeginEvent(IEnumerable<UpdateResult> eventMethod)
	{
		Updater updater = new Updater(eventMethod);
		if (!updater.Update())
			return;
		_runningEvents.Add(updater);
	}

	public void Update()
	{
		int index = 0;
		while (index < _runningEvents.Count)
		{
			if (_runningEvents[index].Update())
				++index;
			else
				_runningEvents.RemoveAt(index);
		}
	}
}
