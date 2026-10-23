using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Draws a part of the level with the waterfall distortion effect.
/// </summary>
internal class WaterfallRenderer : IRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;

		[VertexAttribute("DistortionTextureMapping")]
		public vec2 distortiontexcoords;

		[VertexAttribute("VertexNormalisation")]
		public vec2 normalisation;
	}

	private static readonly Dictionary<Renderer, WaterfallRenderer> RendererDictionary = new();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly Vertex[] _vertices = new Vertex[4];

	private Vector2[] _vertexPositions = new Vector2[4];

	private Vector2[] _vertexUVs = new Vector2[4];

	private Vector2[] _distortionUVs = new Vector2[4];

	private ManagedShaderProgram _shaderProgram;

	private Vector2[] _vertexNormalisation = new Vector2[4];

	public ITexture DistortionTexture { get; set; }

	public double DistortionAmount { get; set; }

	public Vector2 DistortionOffset { get; set; }

	public Vector2i NonDistortionRadius { get; set; }

	public static WaterfallRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new WaterfallRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private WaterfallRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/waterfall.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		_shaderProgram.Program.Activate();
		_shaderProgram.Program.SetUniform("InputTexture", 0);
		_shaderProgram.Program.SetUniform("InputDistortionTexture", 1);
	}

	public void Dispose()
	{
		_vbo.Dispose();
		_vao.Dispose();
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
		Renderer.GetTextureMappings(DistortionTexture, new Rectangle(DistortionOffset.X * DistortionTexture.Width, DistortionOffset.Y * DistortionTexture.Height, destination.Width, destination.Height), _distortionUVs);
		// Corner positions of a unit square, so that the shader knows where each pixel is inside the quad
		Renderer.GetVertices(new Rectangle(0.0, 0.0, 1.0, 1.0), _vertexNormalisation);
		for (int i = 0; i < 4; i++)
		{
			_vertices[i] = new Vertex
			{
				position = _vertexPositions[i].ToVec2(),
				texcoords = _vertexUVs[i].ToVec2(),
				distortiontexcoords = _distortionUVs[i].ToVec2(),
				normalisation = _vertexNormalisation[i].ToVec2()
			};
		}
		_vbo.SetData(_vertices, 0, 4);
		Matrix4 projectionMatrix = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Opaque;
		_graphicsContext.SetTextures(new ITexture[2] { texture, DistortionTexture });
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", projectionMatrix);
		program.SetUniform("InputDistortionAmount", 1.0 / texture.Height * DistortionAmount);
		program.SetUniform("InputNonDistortionRadius", new Vector2(1.0 / destination.Width * NonDistortionRadius.X, 1.0 / destination.Height * NonDistortionRadius.Y));
		_vao.Render(PrimitiveType.Quads, 0, 4);
	}
}
