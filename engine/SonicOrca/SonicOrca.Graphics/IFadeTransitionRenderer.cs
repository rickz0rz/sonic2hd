using System;

namespace SonicOrca.Graphics;

public interface IFadeTransitionRenderer : IDisposable
{
	float Opacity { get; set; }

	void Render();

	void Render(IFramebuffer sourceFramebuffer, IFramebuffer destFramebuffer);
}
