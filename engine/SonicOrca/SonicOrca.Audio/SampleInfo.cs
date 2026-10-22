using System;
using SonicOrca.Resources;

namespace SonicOrca.Audio;

public class SampleInfo : ILoadedResource, IDisposable
{
	private readonly ResourceTree _resourceTree;

	private readonly string _sampleResourceKeyPath;

	private readonly int? _loopSampleIndex;

	public Resource Resource { get; set; }

	public Sample Sample { get; private set; }

	public bool HasLoopPoint => _loopSampleIndex.HasValue;

	public int LoopSampleIndex => _loopSampleIndex.Value;

	public SampleInfo(Sample sample, int? loopSampleIndex = null)
	{
		Sample = sample;
		_loopSampleIndex = loopSampleIndex;
	}

	public SampleInfo(ResourceTree resourceTree, string sampleResourceKeyPath, int? loopSampleIndex = null)
	{
		_resourceTree = resourceTree;
		_sampleResourceKeyPath = sampleResourceKeyPath;
		_loopSampleIndex = loopSampleIndex;
	}

	public void OnLoaded()
	{
		if (_sampleResourceKeyPath != null)
		{
			Sample = _resourceTree.GetLoadedResource<Sample>(_sampleResourceKeyPath);
		}
	}

	public void Dispose()
	{
	}
}
