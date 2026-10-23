using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Blurs a texture in two passes: first horizontally into an intermediate framebuffer, then vertically into the current framebuffer.
/// </summary>
public class GaussianBlurRenderer : IRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;
	}

	// The number of weights the shader uses: the centre weight and four weights on each side
	private const int WeightCount = 5;

	private static readonly Dictionary<Renderer, GaussianBlurRenderer> RendererDictionary = new();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly Vertex[] _vertices = new Vertex[4];

	private Vector2[] _vertexPositions = new Vector2[4];

	private Vector2[] _vertexUVs = new Vector2[4];

	private ManagedShaderProgram _shaderProgram;

	private IFramebuffer _intermediateTarget;

	private double[] _gaussianWeights;

	private double _softness;

	public double Softness
	{
		get
		{
			return _softness;
		}
		set
		{
			if (_softness != value)
			{
				if (value == 0.0)
				{
					throw new ArgumentException("Softness can not be 0.", nameof(value));
				}
				_softness = value;
				_gaussianWeights = CalculateWeights(value);
			}
		}
	}

	public static GaussianBlurRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new GaussianBlurRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private GaussianBlurRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/gaussian_blur.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		Softness = 8.0;
		_shaderProgram.Program.Activate();
		_shaderProgram.Program.SetUniform("InputTexture", 0);
	}

	public void Dispose()
	{
		_intermediateTarget?.Dispose();
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
		_graphicsContext.SetTexture(texture);
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", projectionMatrix);
		program.SetUniform("InputAxis", 0);
		for (int i = 0; i < WeightCount; i++)
		{
			program.SetUniform("InputWeight[" + i + "]", _gaussianWeights[i]);
		}
		IFramebuffer currentFramebuffer = _graphicsContext.CurrentFramebuffer;
		if (_intermediateTarget == null || _intermediateTarget.Width < destination.Width || _intermediateTarget.Height < destination.Height)
		{
			_intermediateTarget?.Dispose();
			_intermediateTarget = _graphicsContext.CreateFrameBuffer(destination.Width, destination.Height);
		}
		_intermediateTarget.Activate();
		_graphicsContext.ClearBuffer();
		_graphicsContext.BlendMode = BlendMode.Opaque;
		_vao.Render(PrimitiveType.Quads, 0, 4);
		_graphicsContext.SetTexture(_intermediateTarget.Textures[0]);
		program.SetUniform("InputAxis", 1);
		currentFramebuffer.Activate();
		_vao.Render(PrimitiveType.Quads, 0, 4);
	}

	/// <summary>
	/// Calculates the blur weights and normalises them so that the full kernel (both sides) adds up to 1.
	/// </summary>
	private static double[] CalculateWeights(double sigma2)
	{
		double[] weights = new double[WeightCount];
		weights[0] = Gaussian(0.0, sigma2);
		double total = weights[0];
		for (int i = 1; i < weights.Length; i++)
		{
			weights[i] = Gaussian(i, sigma2);
			// Each side weight is used on the two sides of the centre
			total += 2.0 * weights[i];
		}
		for (int i = 0; i < weights.Length; i++)
		{
			weights[i] /= total;
		}
		return weights;
	}

	private static double Gaussian(double x, double sigma2)
	{
		double scale = 1.0 / (Math.PI * 2.0 * sigma2);
		double exponent = (0.0 - x * x) / (2.0 * sigma2);
		return scale * Math.Exp(exponent);
	}
}
