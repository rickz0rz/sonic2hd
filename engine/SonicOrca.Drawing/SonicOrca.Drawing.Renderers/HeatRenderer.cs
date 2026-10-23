using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Draws a texture with a heat haze distortion.
/// </summary>
internal class HeatRenderer : IHeatRenderer, IRenderer, IDisposable
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
	}

	private static readonly Dictionary<Renderer, HeatRenderer> RendererDictionary = new();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly Vertex[] _vertices = new Vertex[4];

	private Vector2[] _vertexPositions = new Vector2[4];

	private Vector2[] _vertexUVs = new Vector2[4];

	private Vector2[] _distortionUVs = new Vector2[4];

	private ManagedShaderProgram _shaderProgram;

	public ITexture DistortionTexture { get; set; }

	public double DistortionAmount { get; set; }

	public static HeatRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new HeatRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private HeatRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/heat.shader");
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
		Renderer.GetTextureMappings(DistortionTexture, new Rectanglei(0, 0, DistortionTexture.Width, DistortionTexture.Height), _distortionUVs);
		for (int i = 0; i < 4; i++)
		{
			_vertices[i] = new Vertex
			{
				position = _vertexPositions[i].ToVec2(),
				texcoords = _vertexUVs[i].ToVec2(),
				distortiontexcoords = _distortionUVs[i].ToVec2()
			};
		}
		_vbo.SetData(_vertices, 0, 4);
		Matrix4 projectionMatrix = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.SetTextures(new ITexture[2] { texture, DistortionTexture });
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", projectionMatrix);
		program.SetUniform("InputDistortionAmount", DistortionAmount);
		_vao.Render(PrimitiveType.Quads, 0, 4);
	}
}
