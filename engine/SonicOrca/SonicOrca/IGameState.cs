using System;
using System.Collections.Generic;

namespace SonicOrca;

public interface IGameState : IDisposable
{
	IEnumerable<UpdateResult> Update();

	void Draw();
}
