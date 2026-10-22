using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

internal class NonLayerRenderer : INonLayerRenderer, IRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;
	}

	private static readonly Dictionary<Renderer, NonLayerRenderer> RendererDictionary = new Dictionary<Renderer, NonLayerRenderer>();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly List<Vertex> _vertices = new List<Vertex>();

	private Vector2[] vertexPositions = new Vector2[4];

	private ManagedShaderProgram _shaderProgram;

	public static NonLayerRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new NonLayerRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private NonLayerRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/nonlayer.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
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

	public void Render(Rectanglei destination)
	{
		Render(new Rectanglei[1] { destination });
	}

	public void Render(IEnumerable<Rectanglei> destinations)
	{
		_vertices.Clear();
		foreach (Rectanglei destination in destinations)
		{
			Vector2[] array = new Vector2[4];
			Renderer.GetVertices(destination, array);
			for (int i = 0; i < 4; i++)
			{
				_vertices.Add(new Vertex
				{
					position = array[i].ToVec2()
				});
			}
		}
		_vbo.SetData(_vertices.ToArray());
		Matrix4 value = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		_renderer.ActivateRenderer(this);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		IShaderProgram program = _shaderProgram.Program;
		program.Activate();
		program.SetUniform("ProjectionMatrix", value);
		_vao.Render(PrimitiveType.Quads, 0, _vertices.Count);
	}
}
