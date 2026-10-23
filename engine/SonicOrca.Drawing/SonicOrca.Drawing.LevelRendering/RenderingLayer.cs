using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Drawing.LevelRendering;

internal class RenderingLayer
{
	private readonly Level _level;

	private readonly IVideoSettings _videoSettings;

	private LevelMarker[] _clippingMarkers = new LevelMarker[0];

	public int LayerIndex { get; }

	public LevelLayer Layer { get; }

	public int RenderTileIndex { get; private set; }

	public int RenderTileCount { get; private set; }

	public List<ActiveObject> LowObjects { get; private set; }

	public List<ActiveObject> HighObjects { get; private set; }

	public bool HasShadows { get; private set; }

	public RenderingLayer(Level level, int layerIndex, IVideoSettings videoSettings)
	{
		_level = level;
		_videoSettings = videoSettings;
		LayerIndex = layerIndex;
		Layer = _level.Map.Layers[layerIndex];
	}

	public void Clear()
	{
		HasShadows = Layer.Shadows.Count > 0 && _videoSettings.EnableShadows;
		LowObjects?.Clear();
		HighObjects?.Clear();
	}

	public void SetRenderTileRange(int index, int count)
	{
		RenderTileIndex = index;
		RenderTileCount = count;
	}

	public void AddLowObject(ActiveObject obj)
	{
		if (LowObjects == null)
		{
			LowObjects = new List<ActiveObject>();
		}
		LowObjects.Add(obj);
	}

	public void AddHighObject(ActiveObject obj)
	{
		if (HighObjects == null)
		{
			HighObjects = new List<ActiveObject>();
		}
		HighObjects.Add(obj);
	}

	public void UpdateClippingMarkers()
	{
		_clippingMarkers = (from m in _level.Map.Markers
							where string.Equals(m.Tag, "clipping", StringComparison.OrdinalIgnoreCase)
							where m.Layer == Layer
							select m).ToArray();
	}

	public bool IsAreaClipped(Viewport viewport, Rectanglei area)
	{
		if (_clippingMarkers.Length != 0)
		{
			LevelMarker[] clippingMarkers = _clippingMarkers;
			foreach (LevelMarker levelMarker in clippingMarkers)
			{
				if (((Rectanglei)new Rectangle((levelMarker.Bounds.X - viewport.Bounds.X) * viewport.Scale.X, (levelMarker.Bounds.Y - viewport.Bounds.Y) * viewport.Scale.Y, levelMarker.Bounds.Width * viewport.Scale.X, levelMarker.Bounds.Height * viewport.Scale.Y)).IntersectsWith(area))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}
}
