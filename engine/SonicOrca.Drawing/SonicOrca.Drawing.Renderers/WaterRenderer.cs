using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Draws the underwater effect below the water level.
/// </summary>
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

	// The effect starts this number of pixels above the water level
	private const float WaterSurfaceMargin = 80f;

	private static readonly Dictionary<Renderer, WaterRenderer> RendererDictionary = new();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private Vector2[] _vertexPositions = new Vector2[4];

	private Vector2[] _vertexUVs = new Vector2[4];

	private readonly Vertex[] _vertices = new Vertex[4];

	private ManagedShaderProgram _shaderProgram;

	private IFramebuffer _canvas;

	public double HueTarget { get; set; }

	/// <summary>The water gradient. The defaults are the colours of the Chemical Plant mega mack.</summary>
	public Colour DarkColour { get; set; } = new Colour(255, 0, 0, 64);

	public Colour MiddleColour { get; set; } = new Colour(255, 255, 0, 255);

	public Colour LightColour { get; set; } = new Colour(255, 255, 162, 223);

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
		_canvas = _graphicsContext.CreateFrameBuffer(ScreenSize.Width, ScreenSize.Height);
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

	/// <summary>
	/// Draws the effect below the water level of the current framebuffer. The framebuffer is the canvas of the level
	/// view: the screen, a half of versus mode, or a larger canvas when the view is zoomed out.
	/// </summary>
	public void Render(Rectanglei regionToFilter)
	{
		IFramebuffer currentFramebuffer = _graphicsContext.CurrentFramebuffer;
		int width = currentFramebuffer.Width;
		int height = currentFramebuffer.Height;
		if (_canvas.Width != width || _canvas.Height != height)
		{
			_canvas.Dispose();
			_canvas = _graphicsContext.CreateFrameBuffer(width, height);
		}
		// Was Repeat followed by a raw GL call clamping T on whichever texture happened to be bound
		WaterManager.waveTexture.Wrapping = TextureWrapping.RepeatHorizontal;
		int effectTop = Math.Clamp((int)(WaterManager.viewportWaterLevel - WaterSurfaceMargin), 0, height);
		// Draw the effect into the canvas, then copy the part below the effect top back to the current framebuffer
		_canvas.Activate();
		Render(currentFramebuffer.Textures[0], new Rectanglei(0, 0, width, height - effectTop), new Rectanglei(0, 0, width, height - effectTop));
		currentFramebuffer.Activate();
		SimpleRenderer simpleRenderer = SimpleRenderer.FromRenderer(_renderer);
		simpleRenderer.BlendMode = BlendMode.Alpha;
		simpleRenderer.Colour = Colours.White;
		Rectangle clipRectangle = simpleRenderer.ClipRectangle;
		simpleRenderer.ClipRectangle = new Rectangle(0.0, 0.0, width, height);
		simpleRenderer.RenderTexture(_canvas.Textures[0], new Rectanglei(0, effectTop, width, height), new Rectanglei(0, effectTop, width, height));
		simpleRenderer.Deactivate();
		simpleRenderer.ClipRectangle = clipRectangle;
		simpleRenderer.BlendMode = BlendMode.Alpha;
	}

	public void Render(ITexture texture, Rectanglei destination, bool flipX = false, bool flipY = false)
	{
		Render(texture, new Rectanglei(0, 0, texture.Width, texture.Height), destination, flipX, flipY);
	}

	public void Render(ITexture texture, Rectanglei source, Rectanglei destination, bool flipX = false, bool flipY = false)
	{
		Renderer.GetVertices(destination, _vertexPositions);
		Renderer.GetTextureMappings(texture, source, _vertexUVs, flipX, flipY);
		for (int i = 0; i < 4; i++)
		{
			_vertices[i].position = _vertexPositions[i].ToVec2();
			_vertices[i].texcoords = _vertexUVs[i].ToVec2();
		}
		_vbo.SetData(_vertices, 0, 4);
		IFramebuffer framebuffer = _renderer.Window.GraphicsContext.CurrentFramebuffer;
		Matrix4 projectionMatrix = framebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.SetTexture(0, texture);
		_graphicsContext.SetTexture(1, WaterManager.waveTexture);
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", projectionMatrix);
		program.SetUniform("InputTexture", 0);
		program.SetUniform("WaveTexture", 1);
		program.SetUniform("InputHueTarget", HueTarget);
		program.SetUniform("InputDarkColour", DarkColour);
		program.SetUniform("InputMiddleColour", MiddleColour);
		program.SetUniform("InputLightColour", LightColour);
		program.SetUniform("InputHueAmount", HueAmount);
		program.SetUniform("InputSaturationChange", SaturationChange);
		program.SetUniform("InputLuminosityChange", LuminosityChange);
		program.SetUniform("InputWavePhase", WavePhase);
		program.SetUniform("InputNumWaves", NumWaves);
		program.SetUniform("InputWaveSize", WaveSize);
		program.SetUniform("iGlobalTime", Time);
		program.SetUniform("InputPositionX", WaterManager.offsetX);
		program.SetUniform("InputPositionY", 0f - WaterManager.offsetY);
		// The size of the source texture in pixels. The shader turns the texture mapping into pixels with it.
		program.SetUniform("InputCanvasSize", new Vector2(texture.Width, texture.Height));
		program.SetUniform("InputWaterLevel", WaterManager.viewportWaterLevel / (float)texture.Height);
		_graphicsContext.BlendMode = BlendMode.Opaque;
		_vao.Render(SonicOrca.Graphics.PrimitiveType.Quads, 0, 4);
	}
}
