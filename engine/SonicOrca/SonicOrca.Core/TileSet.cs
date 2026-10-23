using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// A collection of tiles, indexed by tile ID, and the textures that the tiles use.
/// </summary>
public class TileSet : IReadOnlyDictionary<int, ITile>, IReadOnlyCollection<KeyValuePair<int, ITile>>, IEnumerable<KeyValuePair<int, ITile>>, IEnumerable, ILoadedResource, IDisposable
{
	public const int InitialTileCapacity = 4096;

	private ITile[] _tiles = new ITile[InitialTileCapacity];

	private readonly HashSet<int> _tileIds = new HashSet<int>();

	private readonly ResourceTree _resourceTree;

	private readonly IReadOnlyList<string> _textureKeys;

	public Resource Resource { get; set; }

	public IReadOnlyList<ITexture> Textures { get; private set; }

	public ITile this[int id]
	{
		get
		{
			if (id >= _tiles.Length)
			{
				return null;
			}
			return _tiles[id];
		}
		set
		{
			if (_tiles.Length <= id)
			{
				// Double the capacity until the ID fits.
				int newCapacity = _tiles.Length;
				do
				{
					newCapacity *= 2;
				}
				while (newCapacity <= id);
				Array.Resize(ref _tiles, newCapacity);
			}
			_tiles[id] = value;
			_tileIds.Add(id);
		}
	}

	public IEnumerable<int> Keys => _tileIds;

	public IEnumerable<ITile> Values => _tileIds.Select(i => _tiles[i]);

	public int Count => _tileIds.Count;

	public TileSet()
	{
	}

	public TileSet(ResourceTree resourceTree, IEnumerable<string> textureKeys)
	{
		_resourceTree = resourceTree;
		_textureKeys = textureKeys.ToArray();
	}

	public void Animate()
	{
		foreach (int tileId in _tileIds)
		{
			_tiles[tileId].Animate();
		}
	}

	public void DrawTile(Renderer renderer, int index, int x, int y)
	{
		DrawTile(renderer, index, new Rectanglei(x, y, Tile.Size, Tile.Size));
	}

	public void DrawTile(Renderer renderer, int index, Rectanglei destination)
	{
		DrawTile(renderer, index, new Rectanglei(0, 0, destination.Width, destination.Height), destination);
	}

	public void DrawTile(Renderer renderer, int index, Rectanglei source, Rectanglei destination)
	{
		// The index keeps its flag bits (flip and tile set), so it is also the flags argument.
		if (TryGetValue(index, out ITile tile))
		{
			tile.Draw(renderer, index, source, destination);
		}
	}

	public void OnLoaded()
	{
		Textures = _textureKeys.Select(x => _resourceTree.GetLoadedResource<ITexture>(x)).ToArray();
	}

	public void Dispose()
	{
	}

	public bool ContainsKey(int key)
	{
		return this[key] != null;
	}

	public bool TryGetValue(int tile, out ITile value)
	{
		value = this[tile & Tile.IndexMask];
		return value != null;
	}

	public IEnumerator<KeyValuePair<int, ITile>> GetEnumerator()
	{
		return _tileIds.Select(i => new KeyValuePair<int, ITile>(i, _tiles[i])).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
