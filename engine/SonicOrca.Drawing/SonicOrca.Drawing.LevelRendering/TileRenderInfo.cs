using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Drawing.LevelRendering;

/// <summary>
/// Holds the data that is necessary to draw one tile of a level layer.
/// </summary>
internal struct TileRenderInfo
{
	public LevelLayer Layer { get; set; }

	public int TileIndex { get; set; }

	public Rectanglei Source { get; set; }

	public Rectanglei Destination { get; set; }

	/// <summary>True when the tile is a copy that the wrap of the layer makes, in the editor view of the layer.</summary>
	public bool Repeated { get; set; }
}
