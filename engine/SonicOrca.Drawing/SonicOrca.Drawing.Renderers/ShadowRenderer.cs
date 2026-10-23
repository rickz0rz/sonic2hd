using System;
using System.Collections.Generic;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Holds the global shadow state. The other renderers read <see cref="IsShadowing"/> to draw shadows in grayscale.
/// </summary>
internal class ShadowRenderer : IRenderer, IDisposable
{
	private static readonly Dictionary<Renderer, ShadowRenderer> RendererDictionary = new();

	private readonly IGraphicsContext _graphicsContext;

	public static bool IsShadowing = false;

	private ManagedShaderProgram _shaderProgram;

	private VertexBuffer _vertexBuffer;

	public static ShadowRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new ShadowRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private ShadowRenderer(Renderer renderer)
	{
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/shadow.shader");
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(2, 2);
	}

	public void Dispose()
	{
		_vertexBuffer.Dispose();
		_shaderProgram.Dispose();
	}

	public void Deactivate()
	{
	}
}
