using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Animation;

public class CompositionGroup : ILoadedResource, IDisposable, IReadOnlyList<Composition>, IReadOnlyCollection<Composition>, IEnumerable<Composition>, IEnumerable
{
	private readonly ResourceTree _resourceTree;

	private readonly IReadOnlyList<string> _textureResourceKeys;

	private readonly IReadOnlyList<CompositionAsset> _assets;

	private readonly IReadOnlyList<Composition> _compositions;

	public Resource Resource { get; set; }

	public IReadOnlyList<ITexture> Textures { get; private set; }

	public IReadOnlyList<string> TextureResourceKeys { get; private set; }

	public IReadOnlyList<CompositionAsset> Assets { get; private set; }

	public Composition this[int index] => _compositions[index];

	public int Count => _compositions.Count;

	public CompositionGroup(ResourceTree resourceTree, IEnumerable<string> textureResourceKeys, IEnumerable<CompositionAsset> assets, IEnumerable<Composition> compositions)
	{
		_resourceTree = resourceTree;
		_textureResourceKeys = textureResourceKeys.ToArray();
		_assets = assets.ToArray();
		_compositions = compositions.ToArray();
	}

	public void OnLoaded()
	{
		TextureResourceKeys = _textureResourceKeys;
		Textures = _textureResourceKeys.Select((string x) => _resourceTree.GetLoadedResource<ITexture>(x)).ToArray();
		Assets = _assets;
	}

	public void Dispose()
	{
	}

	public IEnumerator<Composition> GetEnumerator()
	{
		return _compositions.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
