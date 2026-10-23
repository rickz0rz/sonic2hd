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
}
