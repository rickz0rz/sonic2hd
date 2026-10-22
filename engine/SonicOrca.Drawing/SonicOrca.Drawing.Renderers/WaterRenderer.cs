using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

internal class WaterRenderer : IWaterRenderer, IRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;
	}

	private static readonly Dictionary<Renderer, WaterRenderer> RendererDictionary = new Dictionary<Renderer, WaterRenderer>();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private Vector2[] vertexPositions = new Vector2[4];

	private Vector2[] vertexUVs = new Vector2[4];

	private readonly Vertex[] _vertices = new Vertex[4];

	private ManagedShaderProgram _shaderProgram;

	private IFramebuffer _canvas;

	public double HueTarget { get; set; }

	public double HueAmount { get; set; }

	public double SaturationChange { get; set; }

	public double LuminosityChange { get; set; }

	public double WavePhase { get; set; }

	public double NumWaves { get; set; }

	public double WaveSize { get; set; }

	public float Time { get; set; }

	public static WaterRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new WaterRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private WaterRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/water.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		_canvas = _graphicsContext.CreateFrameBuffer(1920, 1080);
	}

	public void Dispose()
	{
		_canvas.Dispose();
		_vbo.Dispose();
		_vao.Dispose();
		_shaderProgram.Dispose();
	}

	public void Deactivate()
	{
	}

	public void Render(Rectanglei regionToFilter)
	{
		IFramebuffer currentFramebuffer = _graphicsContext.CurrentFramebuffer;
		// Was Repeat followed by a raw GL call clamping T on whichever texture happened to be bound
		WaterManager.waveTexture.Wrapping = TextureWrapping.RepeatHorizontal;
		int num = (int)(WaterManager.viewportWaterLevel - 80f);
		if (num < 0)
		{
			num = 0;
		}
		if (num > 1080)
		{
			num = 1080;
		}
		_canvas.Activate();
		Render(currentFramebuffer.Textures[0], new Rectanglei(0, 0, 1920, 1080 - num), new Rectanglei(0, 0, 1920, 1080 - num));
		currentFramebuffer.Activate();
		SimpleRenderer simpleRenderer = SimpleRenderer.FromRenderer(_renderer);
		simpleRenderer.BlendMode = BlendMode.Alpha;
		simpleRenderer.Colour = Colours.White;
		simpleRenderer.RenderTexture(_canvas.Textures[0], new Rectanglei(0, num, 1920, 1080), new Rectanglei(0, num, 1920, 1080));
		simpleRenderer.BlendMode = BlendMode.Alpha;
		simpleRenderer.Deactivate();
	}

	public void Render(ITexture texture, Rectanglei destination, bool flipX = false, bool flipY = false)
	{
		Render(texture, new Rectanglei(0, 0, texture.Width, texture.Height), destination, flipX, flipY);
	}

	public void Render(ITexture texture, Rectanglei source, Rectanglei destination, bool flipX = false, bool flipY = false)
	{
		Renderer.GetVertices(destination, vertexPositions);
		Renderer.GetTextureMappings(texture, source, vertexUVs, flipX, flipY);
		for (int i = 0; i < 4; i++)
		{
			_vertices[i].position = vertexPositions[i].ToVec2();
			_vertices[i].texcoords = vertexUVs[i].ToVec2();
		}
		_vbo.SetData(_vertices, 0, 4);
		Matrix4 value = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.SetTexture(0, texture);
		_graphicsContext.SetTexture(1, WaterManager.waveTexture);
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", value);
		program.SetUniform("InputTexture", 0);
		program.SetUniform("WaveTexture", 1);
		program.SetUniform("InputHueTarget", HueTarget);
		program.SetUniform("InputHueAmount", HueAmount);
		program.SetUniform("InputSaturationChange", SaturationChange);
		program.SetUniform("InputLuminosityChange", LuminosityChange);
		program.SetUniform("InputWavePhase", WavePhase);
		program.SetUniform("InputNumWaves", NumWaves);
		program.SetUniform("InputWaveSize", WaveSize);
		program.SetUniform("iGlobalTime", Time);
		program.SetUniform("InputPositionX", WaterManager.offsetX);
		program.SetUniform("InputPositionY", 0f - WaterManager.offsetY);
		program.SetUniform("InputWaterLevel", WaterManager.viewportWaterLevel / 1080f);
		_graphicsContext.BlendMode = BlendMode.Opaque;
		_vao.Render(SonicOrca.Graphics.PrimitiveType.Quads, 0, 4);
	}
}
