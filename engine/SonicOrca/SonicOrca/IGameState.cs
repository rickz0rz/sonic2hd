using System;
using System.Collections.Generic;

namespace SonicOrca;

/// <summary>
/// A screen or mode of the game that updates as a sequence of steps and draws itself.
/// </summary>
public interface IGameState : IDisposable
{
	IEnumerable<UpdateResult> Update();

	void Draw();
}
