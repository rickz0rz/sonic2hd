using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

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

	private static readonly Dictionary<Renderer, GaussianBlurRenderer> RendererDictionary = new Dictionary<Renderer, GaussianBlurRenderer>();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly Vertex[] _vertices = new Vertex[4];

	private Vector2[] vertexPositions = new Vector2[4];

	private Vector2[] vertexUVs = new Vector2[4];

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
					throw new ArgumentException("Softness can not be 0.", "value");
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
		if (_intermediateTarget != null)
		{
			_intermediateTarget.Dispose();
		}
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
				}
			};
		}
		_vbo.SetData(_vertices, 0, 4);
		Matrix4 value = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.SetTexture(texture);
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", value);
		program.SetUniform("InputAxis", 0);
		for (int j = 0; j < 5; j++)
		{
			program.SetUniform("InputWeight[" + j + "]", _gaussianWeights[j]);
		}
		IFramebuffer currentFramebuffer = _graphicsContext.CurrentFramebuffer;
		if (_intermediateTarget == null || _intermediateTarget.Width < destination.Width || _intermediateTarget.Height < destination.Height)
		{
			if (_intermediateTarget != null)
			{
				_intermediateTarget.Dispose();
			}
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

	private static double[] CalculateWeights(double sigma2)
	{
		double[] array = new double[5]
		{
			Gaussian(0.0, sigma2),
			0.0,
			0.0,
			0.0,
			0.0
		};
		double num = array[0];
		for (int i = 1; i < array.Length; i++)
		{
			array[i] = Gaussian(i, sigma2);
			num += 2.0 * array[i];
		}
		for (int j = 0; j < array.Length; j++)
		{
			array[j] /= num;
		}
		return array;
	}

	private static double Gaussian(double x, double sigma2)
	{
		double num = 1.0 / (Math.PI * 2.0 * sigma2);
		double d = (0.0 - x * x) / (2.0 * sigma2);
		return num * Math.Exp(d);
	}
}
