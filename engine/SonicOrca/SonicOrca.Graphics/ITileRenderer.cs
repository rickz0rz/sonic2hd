using System;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface ITileRenderer : IRenderer, IDisposable
{
	Rectangle ClipRectangle { get; set; }

	Matrix4 ModelMatrix { get; set; }

	ITexture[] Textures { get; set; }

	int Filter { get; set; }

	double FilterAmount { get; set; }

	bool Rendering { get; }

	int NumTiles { get; }

	void BeginRender();

	void AddTile(Rectanglei source, Rectanglei destination, int textureIndex, bool flipX = false, bool flipY = false, float opacity = 1f, TileBlendMode blend = TileBlendMode.Alpha);

	void EndRender();
}
