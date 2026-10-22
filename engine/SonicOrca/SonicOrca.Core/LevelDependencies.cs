using System;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class LevelDependencies : ILoadedResource, IDisposable
{
	public Resource Resource { get; set; }

	public void OnLoaded()
	{
	}

	public void Dispose()
	{
	}
}
