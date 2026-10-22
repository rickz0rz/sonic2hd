using System;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface IHeatRenderer : IRenderer, IDisposable
{
	ITexture DistortionTexture { get; set; }

	double DistortionAmount { get; set; }

	void Render(ITexture texture, Rectanglei destination, bool flipX = false, bool flipY = false);

	void Render(ITexture texture, Rectanglei source, Rectanglei destination, bool flipX = false, bool flipY = false);
}
