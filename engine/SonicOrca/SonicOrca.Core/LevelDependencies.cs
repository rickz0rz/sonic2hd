using System;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// A resource that has no data. Its only function is to load the resources that a level needs.
/// </summary>
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
