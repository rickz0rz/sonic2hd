using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;
using SonicOrca.Graphics.V2.Animation;

namespace SonicOrca.Drawing.Renderers;

public class ObjectRenderer : IObjectRenderer, IRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;
	}

	private class BatchOperation
	{
		private readonly ObjectRenderer _objectRenderer;

		private readonly int _vertexBufferIndex;

		private readonly int _vertexCount;

		private readonly BlendMode _blendMode;

		private readonly Rectangle _clipRectangle;

		private readonly Matrix4 _modelMatrix;

		private readonly ITexture _texture;

		private readonly Colour _multiplyColour;

		private readonly Colour _additiveColour;

		private readonly bool _shadow;

		private readonly int _filter;

		private readonly double _filterAmount;

		private Vector2[] vertexPositions = new Vector2[4];

		public BatchOperation(ObjectRenderer objectRenderer, int vertexBufferIndex, int vertexCount, BlendMode blendMode, Rectangle clipRectangle, Matrix4 modelMatrix, ITexture texture, Colour multiplyColour, Colour additiveColour, bool shadow, int filter, double filterAmount)
		{
			_objectRenderer = objectRenderer;
			_vertexBufferIndex = vertexBufferIndex;
			_vertexCount = vertexCount;
			_blendMode = blendMode;
			_clipRectangle = clipRectangle;
			_modelMatrix = modelMatrix;
			_texture = texture;
			_multiplyColour = multiplyColour;
			_additiveColour = additiveColour;
			_shadow = shadow;
			_filter = filter;
			_filterAmount = filterAmount;
		}

		public void Render()
		{
			IGraphicsContext graphicsContext = _objectRenderer._graphicsContext;
			IShaderProgram shaderProgram = (_shadow ? _objectRenderer._shadowShaderProgram.Program : _objectRenderer._shaderProgram.Program);
			graphicsContext.BlendMode = _blendMode;
			graphicsContext.SetTexture(_texture);
			shaderProgram.Activate();
			if (ShadowRenderer.IsShadowing)
			{
				shaderProgram.SetUniform("AlphaGrayscale", 1f);
			}
			else
			{
				shaderProgram.SetUniform("AlphaGrayscale", 0f);
			}
			shaderProgram.SetUniform("ModelViewMatrix", _modelMatrix);
			shaderProgram.SetUniform("ProjectionMatrix", _objectRenderer._projectionMatrix);
			shaderProgram.SetUniform("InputTexture", 0);
			shaderProgram.SetUniform("InputClipRectangle", new Vector4(_clipRectangle.X, _clipRectangle.Y, _clipRectangle.Right, _clipRectangle.Bottom));
			shaderProgram.SetUniform("InputColourMultiply", _multiplyColour);
			shaderProgram.SetUniform("InputColourAdd", _additiveColour);
			shaderProgram.SetUniform("InputEnableMask", (_blendMode != BlendMode.Additive) ? 1 : 0);
			shaderProgram.SetUniform("InputFilter", _filter);
			shaderProgram.SetUniform("InputFilterAmount", _filterAmount);
			(_shadow ? _objectRenderer._vaoShadow : _objectRenderer._vao).Render(PrimitiveType.Quads, _vertexBufferIndex, _vertexCount);
		}
	}

	private class MatrixState : IDisposable
	{
		private readonly ObjectRenderer _objectRenderer;

		private readonly Matrix4 _matrix;

		private readonly Rectangle _clipRectangle;

		public MatrixState(ObjectRenderer objectRenderer, Matrix4 matrix, Rectangle clipRectangle)
		{
			_objectRenderer = objectRenderer;
			_matrix = matrix;
			_clipRectangle = clipRectangle;
		}

		public void Dispose()
		{
			_objectRenderer.ModelMatrix = _matrix;
			_objectRenderer.ClipRectangle = _clipRectangle;
		}
	}

	private static readonly Dictionary<Renderer, ObjectRenderer> RendererDictionary = new Dictionary<Renderer, ObjectRenderer>();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private readonly IVertexArray _vaoShadow;

	private readonly List<Vertex> _vertices = new List<Vertex>();

	private readonly ManagedShaderProgram _shaderProgram;

	private readonly ManagedShaderProgram _shadowShaderProgram;

	private Matrix4 _projectionMatrix;

	private readonly List<BatchOperation> _batchOperations = new List<BatchOperation>();

	private int _batchedVertexIndex;

	private int _batchedVertexCount;

	private Vector2 _scale;

	private Vector2[] vertexPositions = new Vector2[4];

	private Vector2[] vertexUVs = new Vector2[4];

	public Matrix4 ModelMatrix { get; set; }

	public BlendMode BlendMode { get; set; }

	public Rectangle ClipRectangle { get; set; }

	public Colour MultiplyColour { get; set; }

	public Colour AdditiveColour { get; set; }

	public ITexture Texture { get; set; }

	public Vector2 Scale
	{
		get
		{
			return _scale;
		}
		set
		{
			_scale = value;
		}
	}

	public bool EmitsLight { get; set; }

	public bool Shadow { get; set; }

	public int Filter { get; set; }

	public double FilterAmount { get; set; }

	public static ObjectRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new ObjectRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private ObjectRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/object.shader");
		_shadowShaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/object_shadow.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vaoShadow = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		_vaoShadow.DefineAttributes(_shadowShaderProgram.Program, _vbo, typeof(Vertex));
		ModelMatrix = Matrix4.Identity;
		BlendMode = BlendMode.Alpha;
		ClipRectangle = new Rectangle(0.0, 0.0, _renderer.Window.ClientSize.X, _renderer.Window.ClientSize.Y);
		MultiplyColour = Colours.White;
		Scale = new Vector2(1.0, 1.0);
	}

	public void Dispose()
	{
		_vao.Dispose();
		_vaoShadow.Dispose();
		_vbo.Dispose();
		_shaderProgram.Dispose();
		_shadowShaderProgram.Dispose();
	}

	public void Deactivate()
	{
		Render();
	}

	private void Render()
	{
		if (_batchedVertexCount == 0)
		{
			return;
		}
		PushBatchOperation();
		_vbo.SetData(_vertices.ToArray(), 0, _vertices.Count);
		_projectionMatrix = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
		foreach (BatchOperation batchOperation in _batchOperations)
		{
			batchOperation.Render();
		}
		_batchOperations.Clear();
		_batchedVertexIndex = 0;
		_batchedVertexCount = 0;
	}

	private void PushBatchOperation()
	{
		if (_batchedVertexIndex != _batchedVertexCount)
		{
			_batchOperations.Add(new BatchOperation(this, _batchedVertexIndex, _batchedVertexCount - _batchedVertexIndex, BlendMode, ClipRectangle, ModelMatrix, Texture, MultiplyColour, AdditiveColour, Shadow, (!EmitsLight) ? Filter : 0, FilterAmount));
			_batchedVertexIndex = _batchedVertexCount;
		}
	}

	private void PushVertices(IReadOnlyList<Vector2> positions, IReadOnlyList<Vector2> textureMappings)
	{
		_renderer.ActivateRenderer(this);
		if (_batchedVertexCount == 0)
		{
			_batchedVertexIndex = 0;
			_vertices.Clear();
		}
		for (int i = 0; i < positions.Count; i++)
		{
			_vertices.Add(new Vertex
			{
				position = new vec2
				{
					x = (float)positions[i].X,
					y = (float)positions[i].Y
				},
				texcoords = new vec2
				{
					s = (float)textureMappings[i].X,
					t = (float)textureMappings[i].Y
				}
			});
		}
		_batchedVertexCount += positions.Count;
		PushBatchOperation();
		if (_batchedVertexCount > 1024)
		{
			Render();
		}
	}

	public IDisposable BeginMatixState()
	{
		return new MatrixState(this, ModelMatrix, ClipRectangle);
	}

	public void SetDefault()
	{
		BlendMode = BlendMode.Alpha;
		MultiplyColour = Colours.White;
		AdditiveColour = Colours.Black;
		Shadow = false;
		EmitsLight = false;
	}

	public void Render(AnimationInstance animationInstance, bool flipX = false, bool flipY = false)
	{
		Animation.Frame currentFrame = animationInstance.CurrentFrame;
		Texture = animationInstance.CurrentTexture;
		Render(currentFrame.Source, currentFrame.Offset, flipX, flipY);
	}

	public void Render(AnimationInstance animationInstance, Vector2 destination, bool flipX = false, bool flipY = false)
	{
		Animation.Frame currentFrame = animationInstance.CurrentFrame;
		Texture = animationInstance.CurrentTexture;
		Render(currentFrame.Source, destination + currentFrame.Offset, flipX, flipY);
	}

	public void Render(CompositionInstance compositionInstance, Vector2 destination, bool flipX = false, bool flipY = false)
	{
	}

	public void Render(Vector2 destination = default(Vector2), bool flipX = false, bool flipY = false)
	{
		Render(new Rectangle(0.0, 0.0, Texture.Width, Texture.Height), destination, flipX, flipY);
	}

	public void Render(Rectangle destination, bool flipX = false, bool flipY = false)
	{
		Render(new Rectangle(0.0, 0.0, Texture.Width, Texture.Height), destination, flipX, flipY);
	}

	public void Render(Rectangle source, Vector2 offset, bool flipX = false, bool flipY = false)
	{
		Render(source, new Rectangle((offset.X - (double)(int)(source.Width / 2.0)) * Scale.X, (offset.Y - (double)(int)(source.Height / 2.0)) * Scale.Y, source.Width * Scale.X, source.Height * Scale.Y), flipX, flipY);
	}

	public void Render(Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		Renderer.GetVertices(destination, vertexPositions);
		if (Math.Abs(ModelMatrix.M11) != 1.0 || Math.Abs(ModelMatrix.M22) != 1.0)
		{
			Renderer.GetTextureMappingsHalfIn(Texture, source, ref vertexUVs, flipX, flipY);
		}
		else
		{
			Renderer.GetTextureMappings(Texture, source, vertexUVs, flipX, flipY);
		}
		PushVertices(vertexPositions, vertexUVs);
	}
}
