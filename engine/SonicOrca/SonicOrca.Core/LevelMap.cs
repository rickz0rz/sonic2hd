using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The map of a level: the layers of tiles, the collision vectors and the markers.
/// </summary>
public class LevelMap : ILoadedResource, IDisposable
{
	// The width and height of a tile in pixels.
	private const int TileSize = 64;

	private readonly ILevelLayerTreeNode _layerTree = new LevelLayerGroup(null);

	private readonly List<LevelLayer> _layers = new List<LevelLayer>();

	private readonly List<CollisionVector> _collisionVectors = new List<CollisionVector>();

	private readonly List<int> _collisionPathLayers = new List<int>();

	private readonly List<LevelMarker> _markers = new List<LevelMarker>();

	public Resource Resource { get; set; }

	public ILevelLayerTreeNode LayerTree => _layerTree;

	public IList<LevelLayer> Layers => _layers;

	public IList<CollisionVector> CollisionVectors => _collisionVectors;

	public IList<int> CollisionPathLayers => _collisionPathLayers;

	public IList<LevelMarker> Markers => _markers;

	public Level Level { get; set; }

	public int MaxColumns
	{
		get
		{
			if (Layers.Count == 0)
			{
				return 0;
			}
			return Layers.Max(x => x.Columns);
		}
	}

	public int MaxRows
	{
		get
		{
			if (Layers.Count == 0)
			{
				return 0;
			}
			return Layers.Max(x => x.Rows);
		}
	}

	public Rectanglei Bounds => new Rectanglei(0, 0, MaxColumns * TileSize, MaxRows * TileSize);

	public void Update()
	{
		foreach (LevelLayer layer in _layers)
		{
			layer.Update();
		}
	}

	public void Animate()
	{
		foreach (LevelLayer layer in _layers)
		{
			layer.Animate();
		}
	}

	public void OnLoaded()
	{
	}

	public void Dispose()
	{
	}

	public LevelLayer FindLayer(string name)
	{
		return _layers.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}
}
