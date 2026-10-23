// Decompiled with JetBrains decompiler
// Type: S2HD.EffectEventManager
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using SonicOrca;

#nullable disable
namespace S2HD;

internal class EffectEventManager
{
	private List<Updater> _runningEvents = new List<Updater>();

	public void Clear() => _runningEvents.Clear();

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
