using System;
using System.Collections.Generic;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The object placements of a level.
/// </summary>
public class LevelBinding : ILoadedResource, IDisposable
{
	private readonly List<ObjectPlacement> _objectPlacements = new List<ObjectPlacement>();

	public Resource Resource { get; set; }

	public IList<ObjectPlacement> ObjectPlacements => _objectPlacements;

	public Level Level { get; set; }

	public void OnLoaded()
	{
	}

	public void Dispose()
	{
	}
}
