using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Resources;

namespace SonicOrca.Graphics;

public class AnimationGroup : ILoadedResource, IDisposable, IReadOnlyList<Animation>, IReadOnlyCollection<Animation>, IEnumerable<Animation>, IEnumerable
{
	private readonly ResourceTree _resourceTree;

	private readonly IReadOnlyList<string> _textureResourceKeys;

	private readonly IReadOnlyList<Animation> _animations;

	public Resource Resource { get; set; }

	public IReadOnlyList<ITexture> Textures { get; private set; }

	public Animation this[int index] => _animations[index];

	public int Count => _animations.Count;

	public AnimationGroup(IEnumerable<ITexture> textures, IEnumerable<Animation> animations)
	{
		Textures = textures.ToArray();
		_animations = animations.ToArray();
	}

	public AnimationGroup(ResourceTree resourceTree, IEnumerable<string> textureResourceKeys, IEnumerable<Animation> animations)
	{
		_resourceTree = resourceTree;
		_textureResourceKeys = textureResourceKeys.ToArray();
		_animations = animations.ToArray();
	}

	public void OnLoaded()
	{
		Textures = _textureResourceKeys.Select((string x) => _resourceTree.GetLoadedResource<ITexture>(x)).ToArray();
	}

	public void Dispose()
	{
	}

	public IEnumerator<Animation> GetEnumerator()
	{
		return _animations.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
