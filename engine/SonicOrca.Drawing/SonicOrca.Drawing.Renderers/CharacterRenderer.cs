using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Draws a character sprite from a skin texture and a body texture, with an optional ghost effect.
/// </summary>
public class CharacterRenderer : ICharacterRenderer, IRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;
	}

	private const double ScreenWidth = 1920.0;

	private const double ScreenHeight = 1080.0;

	private static readonly Dictionary<Renderer, CharacterRenderer> RendererDictionary = new();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly IVertexArray _vaoGhost;

	private Vertex[] _vertices = new Vertex[4];

	private Vector2[] _vertexPositions = new Vector2[4];

	private Vector2[] _vertexUVs = new Vector2[4];

	private ManagedShaderProgram _shaderProgram;

	private ManagedShaderProgram _ghostShaderProgram;

	public Matrix4 ModelMatrix { get; set; }

	public Rectangle ClipRectangle { get; set; }

	public int Filter { get; set; }

	public double FilterAmount { get; set; }

	public float Brightness { get; set; }

	public static CharacterRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new CharacterRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private CharacterRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/character.shader");
		_ghostShaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/ghost.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vaoGhost = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		_vaoGhost.DefineAttributes(_ghostShaderProgram.Program, _vbo, typeof(Vertex));
		ModelMatrix = Matrix4.Identity;
		ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
		_shaderProgram.Program.Activate();
		_shaderProgram.Program.SetUniform("InputTextureSkin", 0);
		_shaderProgram.Program.SetUniform("InputTextureBody", 1);
	}

	public void Dispose()
	{
		_vao.Dispose();
		_vaoGhost.Dispose();
		_vbo.Dispose();
		_shaderProgram.Dispose();
		_ghostShaderProgram.Dispose();
	}

	public void Deactivate()
	{
	}

	public void RenderTexture(ITexture skinTexture, ITexture bodyTexture, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		RenderTexture(skinTexture, bodyTexture, 0.0, 0.0, 0.0, source, destination, flipX, flipY);
	}

	public void RenderTexture(ITexture skinTexture, ITexture bodyTexture, double hueShift, double satuationShift, double luminosityShift, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		RenderTexture(_shaderProgram, skinTexture, bodyTexture, hueShift, satuationShift, luminosityShift, source, destination, flipX, flipY);
	}

	public void RenderTextureGhost(ITexture skinTexture, ITexture bodyTexture, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		RenderTexture(_ghostShaderProgram, skinTexture, bodyTexture, 0.0, 0.0, 0.0, source, destination, flipX, flipY);
	}

	private void RenderTexture(ManagedShaderProgram managedShaderProgram, ITexture skinTexture, ITexture bodyTexture, double hueShift, double saturationShift, double luminosityShift, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		Renderer.GetVertices(destination, _vertexPositions);
		Renderer.GetTextureMappings(skinTexture, source, _vertexUVs, flipX, flipY);
		for (int i = 0; i < 4; i++)
		{
			_vertices[i] = new Vertex
			{
				position = _vertexPositions[i].ToVec2(),
				texcoords = _vertexUVs[i].ToVec2()
			};
		}
		_vbo.SetData(_vertices, 0, 4);
		Matrix4 projectionMatrix = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.SetTextures(new ITexture[2] { skinTexture, bodyTexture });
		IShaderProgram program = managedShaderProgram.Program;
		program.Activate();
		program.SetUniform("AlphaGrayscale", ShadowRenderer.IsShadowing ? 1f : 0f);
		program.SetUniform("ModelViewMatrix", ModelMatrix);
		program.SetUniform("ProjectionMatrix", projectionMatrix);
		program.SetUniform("InputHSLShift", new Vector3(hueShift, saturationShift, luminosityShift));
		program.SetUniform("InputClipRectangle", new Vector4(ClipRectangle.X, ClipRectangle.Y, ClipRectangle.Right, ClipRectangle.Bottom));
		program.SetUniform("InputFilter", Filter);
		program.SetUniform("InputFilterAmount", FilterAmount);
		if (managedShaderProgram == _shaderProgram)
		{
			program.SetUniform("InputBrightness", Brightness);
		}
		IVertexArray vao = (managedShaderProgram == _ghostShaderProgram) ? _vaoGhost : _vao;
		vao.Render(PrimitiveType.Quads, 0, 4);
	}
}
