using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

public class TileSequence : ITile
{
	private readonly TileSet _tileSet;

	private readonly int _id;

	private readonly int[] _tileIds;

	public int Id => _id;

	public bool Animated => false;

	public TileSequence(TileSet tileSet, int id, IEnumerable<int> tileIds)
	{
		_tileSet = tileSet;
		_id = id;
		_tileIds = tileIds.ToArray();
	}

	public void Animate()
	{
	}

	public void Draw(Renderer renderer, int flags, int x, int y)
	{
		int[] tileIds = _tileIds;
		foreach (int tile in tileIds)
		{
			if (_tileSet.TryGetValue(tile, out var value))
			{
				value.Draw(renderer, flags, x, y);
			}
		}
	}

	public void Draw(Renderer renderer, int flags, Rectanglei source, Rectanglei destination)
	{
		ITileRenderer tileRenderer = renderer.GetTileRenderer();
		if (tileRenderer.Rendering)
		{
			Draw(tileRenderer, flags, source, destination);
		}
		else
		{
			Draw(renderer.Get2dRenderer(), flags, source, destination);
		}
	}

	public void Draw(I2dRenderer g, int flags, Rectanglei source, Rectanglei destination)
	{
		int[] tileIds = _tileIds;
		foreach (int tile in tileIds)
		{
			if (_tileSet.TryGetValue(tile, out var value))
			{
				value.Draw(g, flags, source, destination);
			}
		}
	}

	public void Draw(ITileRenderer tileRenderer, int flags, Rectanglei source, Rectanglei destination)
	{
		int[] tileIds = _tileIds;
		foreach (int tile in tileIds)
		{
			if (_tileSet.TryGetValue(tile, out var value))
			{
				value.Draw(tileRenderer, flags, source, destination);
			}
		}
	}
}
