using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Drawing.LevelRendering;

/// <summary>
/// Holds the per-frame rendering state of one level layer: its tile range, its objects and its clipping markers.
/// </summary>
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
		LowObjects ??= new List<ActiveObject>();
		LowObjects.Add(obj);
	}

	public void AddHighObject(ActiveObject obj)
	{
		HighObjects ??= new List<ActiveObject>();
		HighObjects.Add(obj);
	}

	public void UpdateClippingMarkers()
	{
		_clippingMarkers = (from m in _level.Map.Markers
							where string.Equals(m.Tag, "clipping", StringComparison.OrdinalIgnoreCase)
							where m.Layer == Layer
							select m).ToArray();
	}

	/// <summary>
	/// Returns true when the layer has clipping markers and no marker touches the area.
	/// The clipping markers mark the only regions where the layer is visible.
	/// </summary>
	public bool IsAreaClipped(Viewport viewport, Rectanglei area)
	{
		if (_clippingMarkers.Length == 0)
		{
			return false;
		}
		foreach (LevelMarker marker in _clippingMarkers)
		{
			Rectanglei markerArea = (Rectanglei)new Rectangle((marker.Bounds.X - viewport.Bounds.X) * viewport.Scale.X, (marker.Bounds.Y - viewport.Bounds.Y) * viewport.Scale.Y, marker.Bounds.Width * viewport.Scale.X, marker.Bounds.Height * viewport.Scale.Y);
			if (markerArea.IntersectsWith(area))
			{
				return false;
			}
		}
		return true;
	}
}
