using System;
using System.Collections.Generic;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface INonLayerRenderer : IRenderer, IDisposable
{
	void Render(IEnumerable<Rectanglei> rects);
}
