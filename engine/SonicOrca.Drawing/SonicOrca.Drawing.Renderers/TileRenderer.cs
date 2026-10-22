using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

public class TileRenderer : ITileRenderer, IRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;

		[VertexAttribute("VertexTextureIndex")]
		public float texindex;

		[VertexAttribute("VertexOpacity")]
		public float opacity;
	}

	private static readonly Dictionary<Renderer, TileRenderer> RendererDictionary = new Dictionary<Renderer, TileRenderer>();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private Vertex[] _vertices = new Vertex[20];

	public int numVertices;

	private Vector2[] vertexPositions = new Vector2[4];

	private Vector2[] vertexUVs = new Vector2[4];

	private ManagedShaderProgram _shaderProgram;

	public static Colour ShadowColour;

	private TileBlendMode _tempLastBlendMode;

	public Matrix4 ModelMatrix { get; set; }

	public ITexture[] Textures { get; set; }

	public Colour Colour { get; set; }

	public Rectangle ClipRectangle { get; set; }

	public int Filter { get; set; }

	public double FilterAmount { get; set; }

	public bool Rendering { get; private set; }

	public int NumTiles { get; private set; }

	public static TileRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new TileRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private TileRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/tile.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		ModelMatrix = Matrix4.Identity;
		Colour = Colours.White;
		ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		_shaderProgram.Program.Activate();
		for (int i = 0; i < 4; i++)
		{
			_shaderProgram.Program.SetUniform("InputTexture[" + i + "]", i);
		}
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

	public void BeginRender()
	{
		if (Rendering)
		{
			throw new InvalidOperationException("Already renderering.");
		}
		Rendering = true;
		NumTiles = 0;
		numVertices = 0;
	}

	public void AddTile(Rectanglei source, Rectanglei destination, int textureIndex, bool flipX = false, bool flipY = false, float opacity = 1f, TileBlendMode blend = TileBlendMode.Alpha)
	{
		if (!Rendering)
		{
			throw new Exception("Not currently renderering");
		}
		ITexture texture = Textures[textureIndex];
		Renderer.GetVertices(destination, vertexPositions);
		Renderer.GetTextureMappings(texture, source, vertexUVs, flipX, flipY);
		for (int i = 0; i < 4; i++)
		{
			_vertices[numVertices].position.x = (float)vertexPositions[i].X;
			_vertices[numVertices].position.y = (float)vertexPositions[i].Y;
			_vertices[numVertices].texcoords.s = (float)vertexUVs[i].X;
			_vertices[numVertices].texcoords.t = (float)vertexUVs[i].Y;
			_vertices[numVertices].texindex = textureIndex;
			_vertices[numVertices].opacity = opacity;
			numVertices++;
			if (numVertices >= _vertices.Length)
			{
				Vertex[] array = new Vertex[_vertices.Length * 2];
				Array.Copy(_vertices, array, _vertices.Length);
				_vertices = array;
			}
		}
		NumTiles++;
		_tempLastBlendMode = blend;
	}

	public void EndRender()
	{
		if (!Rendering)
		{
			throw new Exception("Not currently renderering");
		}
		_vbo.SetData(_vertices, 0, numVertices);
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = ((_tempLastBlendMode != TileBlendMode.Additive) ? BlendMode.Alpha : BlendMode.Additive);
		ITexture[] textures = Textures;
		for (int i = 0; i < textures.Length; i++)
		{
			textures[i].Filtering = TextureFiltering.NearestNeighbour;
		}
		_graphicsContext.SetTextures(Textures);
		Matrix4 value = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		if (ShadowRenderer.IsShadowing)
		{
			program.SetUniform("AlphaGrayscale", 1f);
		}
		else
		{
			program.SetUniform("AlphaGrayscale", 0f);
		}
		program.SetUniform("ModelViewMatrix", ModelMatrix);
		program.SetUniform("ProjectionMatrix", value);
		program.SetUniform("InputColour", Colour);
		program.SetUniform("InputClipRectangle", new Vector4(ClipRectangle.X, ClipRectangle.Y, ClipRectangle.Right, ClipRectangle.Bottom));
		program.SetUniform("ShadowColour", ShadowColour);
		program.SetUniform("InputFilter", Filter);
		program.SetUniform("InputFilterAmount", FilterAmount);
		_vao.Render(PrimitiveType.Quads, 0, numVertices);
		Rendering = false;
	}
}
