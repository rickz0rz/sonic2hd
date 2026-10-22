using System;
using System.Collections.Generic;

namespace SonicOrca.Graphics;

public interface IFramebuffer : IDisposable
{
	int Width { get; }

	int Height { get; }

	IReadOnlyList<ITexture> Textures { get; }

	void Activate();
}
