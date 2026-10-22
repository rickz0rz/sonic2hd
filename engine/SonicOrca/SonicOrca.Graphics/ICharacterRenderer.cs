using System;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface ICharacterRenderer : IRenderer, IDisposable
{
	Rectangle ClipRectangle { get; set; }

	Matrix4 ModelMatrix { get; set; }

	int Filter { get; set; }

	double FilterAmount { get; set; }

	float Brightness { get; set; }

	void RenderTexture(ITexture skinTexture, ITexture bodyTexture, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false);

	void RenderTexture(ITexture skinTexture, ITexture bodyTexture, double hueShift, double satuationShift, double luminosityShift, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false);

	void RenderTextureGhost(ITexture skinTexture, ITexture bodyTexture, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false);
}
