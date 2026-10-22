using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

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

	private static readonly Dictionary<Renderer, WaterfallRenderer> RendererDictionary = new Dictionary<Renderer, WaterfallRenderer>();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly Vertex[] _vertices = new Vertex[4];

	private Vector2[] vertexPositions = new Vector2[4];

	private Vector2[] vertexUVs = new Vector2[4];

	private Vector2[] distortionUVs = new Vector2[4];

	private ManagedShaderProgram _shaderProgram;

	private Vector2[] vertexNormalisation = new Vector2[4];

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
		Renderer.GetVertices(destination, vertexPositions);
		Renderer.GetTextureMappings(texture, source, vertexUVs, flipX, flipY);
		Renderer.GetTextureMappings(DistortionTexture, new Rectangle(DistortionOffset.X * (double)DistortionTexture.Width, DistortionOffset.Y * (double)DistortionTexture.Height, destination.Width, destination.Height), distortionUVs);
		Renderer.GetVertices(new Rectangle(0.0, 0.0, 1.0, 1.0), vertexNormalisation);
		for (int i = 0; i < 4; i++)
		{
			_vertices[i] = new Vertex
			{
				position = new vec2
				{
					x = (float)vertexPositions[i].X,
					y = (float)vertexPositions[i].Y
				},
				texcoords = new vec2
				{
					s = (float)vertexUVs[i].X,
					t = (float)vertexUVs[i].Y
				},
				distortiontexcoords = new vec2
				{
					s = (float)distortionUVs[i].X,
					t = (float)distortionUVs[i].Y
				},
				normalisation = new vec2
				{
					x = (float)vertexNormalisation[i].X,
					y = (float)vertexNormalisation[i].Y
				}
			};
		}
		_vbo.SetData(_vertices, 0, 4);
		Matrix4 value = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Opaque;
		_graphicsContext.SetTextures(new ITexture[2] { texture, DistortionTexture });
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", value);
		program.SetUniform("InputDistortionAmount", 1.0 / (double)texture.Height * DistortionAmount);
		program.SetUniform("InputNonDistortionRadius", new Vector2(1.0 / (double)destination.Width * (double)NonDistortionRadius.X, 1.0 / (double)destination.Height * (double)NonDistortionRadius.Y));
		_vao.Render(PrimitiveType.Quads, 0, 4);
	}
}
