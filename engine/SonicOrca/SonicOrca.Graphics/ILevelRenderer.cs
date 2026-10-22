using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface ILevelRenderer : IDisposable
{
	string[] LastDebugLog { get; }

	Task LoadAsync(CancellationToken ct = default(CancellationToken));

	void Initialise();

	void Render(Renderer renderer, Viewport viewport, LayerViewOptions layerViewOptions);

	void RenderToClipboard(Viewport viewport, LayerViewOptions layerViewOptions);
}
