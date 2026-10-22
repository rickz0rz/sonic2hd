using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Video;

public class FilmGroup : ILoadedResource, IDisposable, IReadOnlyList<Film>, IReadOnlyCollection<Film>, IEnumerable<Film>, IEnumerable
{
	private readonly ResourceTree _resourceTree;

	private readonly IReadOnlyList<string> _filmResourceKeys;

	private readonly IReadOnlyList<Film> _films;

	public Resource Resource { get; set; }

	public IReadOnlyList<IFilmBuffer> FilmBuffers { get; private set; }

	public IReadOnlyList<string> FilmResourceKeys { get; private set; }

	public Film this[int index] => _films[index];

	public int Count => _films.Count;

	public FilmGroup(ResourceTree resourceTree, IEnumerable<string> filmResourceKeys, IEnumerable<Film> films)
	{
		_resourceTree = resourceTree;
		_filmResourceKeys = filmResourceKeys.ToArray();
		_films = films.ToArray();
	}

	public void OnLoaded()
	{
		FilmResourceKeys = _filmResourceKeys;
		FilmBuffers = _filmResourceKeys.Select((string x) => _resourceTree.GetLoadedResource<IFilmBuffer>(x)).ToArray();
	}

	public void Dispose()
	{
	}

	public IEnumerator<Film> GetEnumerator()
	{
		return _films.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
