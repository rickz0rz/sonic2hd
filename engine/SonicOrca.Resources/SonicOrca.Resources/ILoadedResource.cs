using System;

namespace SonicOrca.Resources;

public interface ILoadedResource : IDisposable
{
	Resource Resource { get; set; }

	void OnLoaded();
}
