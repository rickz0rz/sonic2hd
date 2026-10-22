using System;
using System.Collections.Generic;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing.Renderers;

internal class ShadowRenderer : IRenderer, IDisposable
{
	private static readonly Dictionary<Renderer, ShadowRenderer> RendererDictionary = new Dictionary<Renderer, ShadowRenderer>();

	private readonly Renderer _renderer;

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
		_renderer = renderer;
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
