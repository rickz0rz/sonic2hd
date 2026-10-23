using System;
using System.Collections.Generic;
using System.IO;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Draws a texture through the greyscale filter shader.
/// </summary>
internal class FilterRenderer : IRenderer, IDisposable
{
	private static readonly Dictionary<Renderer, FilterRenderer> RendererDictionary = new();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private IShader _vertexShader;

	private IShader _fragmentShader;

	private Vector2[] _vertexPositions = new Vector2[4];

	private Vector2[] _vertexUVs = new Vector2[4];

	private IShaderProgram _shaderProgram;

	private VertexBuffer _vertexBuffer;

	public static FilterRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new FilterRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private FilterRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		OrcaShader.Parse(File.ReadAllText(ContentDirectory.Combine("shaders/greyscale_filter.shader")), out string vertexSource, out string fragmentSource);
		_vertexShader = _graphicsContext.CreateShader(ShaderType.Vertex, vertexSource);
		_fragmentShader = _graphicsContext.CreateShader(ShaderType.Fragment, fragmentSource);
		_shaderProgram = _graphicsContext.CreateShaderProgram(_vertexShader, _fragmentShader);
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(2, 2);
	}

	public void Dispose()
	{
		_vertexBuffer.Dispose();
		_vertexShader.Dispose();
		_fragmentShader.Dispose();
		_shaderProgram.Dispose();
	}

	public void Deactivate()
	{
	}

	public void Render(ITexture texture, Rectanglei destination, bool flipX = false, bool flipY = false)
	{
		Render(texture, new Rectanglei(0, 0, texture.Width, texture.Height), destination, flipX, flipY);
	}

	public void Render(ITexture texture, Rectanglei source, Rectanglei destination, bool flipX = false, bool flipY = false)
	{
		Renderer.GetVertices(destination, _vertexPositions);
		Renderer.GetTextureMappings(texture, source, _vertexUVs, flipX, flipY);
		_vertexBuffer.Begin();
		for (int i = 0; i < 4; i++)
		{
			_vertexBuffer.AddValue(0, _vertexPositions[i]);
			_vertexBuffer.AddValue(0, _vertexUVs[i]);
		}
		_vertexBuffer.End();
		Matrix4 projectionMatrix = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.SetTexture(texture);
		_shaderProgram.Activate();
		_shaderProgram.SetUniform("ProjectionMatrix", projectionMatrix);
		_shaderProgram.SetUniform("InputTexture", 0);
		_graphicsContext.BlendMode = BlendMode.Opaque;
		_vertexBuffer.Render(PrimitiveType.Quads);
	}
}
