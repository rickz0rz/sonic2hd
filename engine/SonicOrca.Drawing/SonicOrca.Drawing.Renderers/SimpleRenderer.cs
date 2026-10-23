using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

public class SimpleRenderer : I2dRenderer, IDisposable, IRenderer
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexColour")]
		public vec4 colour;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;
	}

	private class BatchOperation
	{
		public SimpleRenderer _simpleRenderer;

		public PrimitiveType _primitiveType;

		public int _vertexBufferIndex;

		public int _vertexCount;

		public BlendMode _blendMode;

		public Rectangle _clipRectangle;

		public IReadOnlyList<ITexture> _textures;

		public Matrix4 _modelMatrix;

		public Colour _additiveColour;

		private Vector2[] vertexPositions = new Vector2[4];

		public BatchOperation(SimpleRenderer simpleRenderer, PrimitiveType primitiveType, int vertexBufferIndex, int vertexCount, BlendMode blendMode, Rectangle clipRectangle, IEnumerable<ITexture> textures, Matrix4 modelMatrix, Colour additiveColour)
		{
			_simpleRenderer = simpleRenderer;
			_primitiveType = primitiveType;
			_vertexBufferIndex = vertexBufferIndex;
			_vertexCount = vertexCount;
			_blendMode = blendMode;
			_clipRectangle = clipRectangle;
			_textures = textures.ToArray();
			_modelMatrix = modelMatrix;
			_additiveColour = additiveColour;
		}

		public void Render()
		{
			IGraphicsContext graphicsContext = _simpleRenderer._graphicsContext;
			IShaderProgram program = _simpleRenderer._shaderProgram.Program;
			graphicsContext.BlendMode = _blendMode;
			graphicsContext.SetTextures(_textures);
			program.Activate();
			program.SetUniform("ModelViewMatrix", _modelMatrix);
			program.SetUniform("ProjectionMatrix", _simpleRenderer._projectionMatrix);
			program.SetUniform("InputTextureCount", _textures.Count);
			for (int i = 0; i < _textures.Count; i++)
			{
				program.SetUniform("InputTexture[" + i + "]", i);
			}
			program.SetUniform("InputClipRectangle", new Vector4(_clipRectangle.X, _clipRectangle.Y, _clipRectangle.Right, _clipRectangle.Bottom));
			program.SetUniform("InputAdditiveColour", _additiveColour);
			_simpleRenderer._vao.Render(_primitiveType, _vertexBufferIndex, _vertexCount);
		}
	}

	private class MatrixState : IDisposable
	{
		private readonly SimpleRenderer _simpleRenderer;

		private readonly Matrix4 _matrix;

		private readonly Rectangle _clipRectangle;

		public MatrixState(SimpleRenderer simpleRenderer, Matrix4 matrix, Rectangle clipRectangle)
		{
			_simpleRenderer = simpleRenderer;
			_matrix = matrix;
			_clipRectangle = clipRectangle;
		}

		public void Dispose()
		{
			_simpleRenderer.ModelMatrix = _matrix;
			_simpleRenderer.ClipRectangle = _clipRectangle;
		}
	}

	private static readonly Dictionary<Renderer, SimpleRenderer> RendererDictionary = new Dictionary<Renderer, SimpleRenderer>();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private Vertex[] _verticesData = new Vertex[20];

	private int numVerticesData;

	private ManagedShaderProgram _shaderProgram;

	private Matrix4 _projectionMatrix;

	private Matrix4 _modelMatrix;

	private PrimitiveType _primitiveType = PrimitiveType.Quads;

	private ITexture[] _textures = new ITexture[0];

	private Vector2[] _vertices;

	private BlendMode _blendMode;

	private Rectangle _clipRectangle;

	private Colour _colour;

	private Colour _additiveColour;

	private BatchOperation[] _batchOperations = new BatchOperation[4];

	private int numBatchOperations;

	private int _batchedVertexIndex;

	private int _batchedVertexCount;

	private Vector2[] vertexPositions = new Vector2[4];

	private Vector2[] vertexUVs = new Vector2[4];

	public Matrix4 ModelMatrix
	{
		get
		{
			return _modelMatrix;
		}
		set
		{
			if (_modelMatrix != value)
			{
				PushBatchOperation();
				_modelMatrix = value;
			}
		}
	}

	public BlendMode BlendMode
	{
		get
		{
			return _blendMode;
		}
		set
		{
			if (_blendMode != value)
			{
				PushBatchOperation();
				_blendMode = value;
			}
		}
	}

	public Rectangle ClipRectangle
	{
		get
		{
			return _clipRectangle;
		}
		set
		{
			if (_clipRectangle != value)
			{
				PushBatchOperation();
				_clipRectangle = value;
			}
		}
	}

	public Colour Colour
	{
		get
		{
			return _colour;
		}
		set
		{
			if (_colour != value)
			{
				PushBatchOperation();
				_colour = value;
			}
		}
	}

	public Colour AdditiveColour
	{
		get
		{
			return _additiveColour;
		}
		set
		{
			if (_additiveColour != value)
			{
				PushBatchOperation();
				_additiveColour = value;
			}
		}
	}

	public ITexture Texture
	{
		get
		{
			if (_textures == null)
			{
				return null;
			}
			if (_textures.Length == 0)
			{
				return null;
			}
			return _textures[0];
		}
		set
		{
			if (value == null)
			{
				if (_textures.Length != 0)
				{
					PushBatchOperation();
					_textures = new ITexture[0];
				}
			}
			else if (_textures.Length == 1)
			{
				if (_textures[0] == value)
				{
					PushBatchOperation();
					_textures[0] = value;
				}
			}
			else
			{
				PushBatchOperation();
				_textures = new ITexture[1] { value };
			}
		}
	}

	public IEnumerable<ITexture> Textures
	{
		get
		{
			return _textures;
		}
		set
		{
			if (value == null)
			{
				Texture = null;
				return;
			}
			ITexture[] array = value.ToArray();
			if (_textures.Length != array.Length)
			{
				PushBatchOperation();
				_textures = array;
				return;
			}
			bool flag = false;
			for (int i = 0; i < _textures.Length; i++)
			{
				if (_textures[i] != array[i])
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				PushBatchOperation();
				_textures = array;
			}
		}
	}

	public static SimpleRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new SimpleRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	private SimpleRenderer(Renderer renderer)
	{
		_renderer = renderer;
		renderer.RegisterRenderer(this);
		_graphicsContext = renderer.Window.GraphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/simple.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		_modelMatrix = Matrix4.Identity;
		BlendMode = BlendMode.Alpha;
		ClipRectangle = new Rectangle(0.0, 0.0, _renderer.Window.ClientSize.X, _renderer.Window.ClientSize.Y);
		Colour = Colours.White;
	}

	public void Dispose()
	{
		_vbo.Dispose();
		_vao.Dispose();
		_shaderProgram.Dispose();
	}

	public void Deactivate()
	{
		Render();
	}

	private void Render()
	{
		if (_batchedVertexCount != 0)
		{
			PushBatchOperation();
			_vbo.SetData(_verticesData, 0, numVerticesData);
			_projectionMatrix = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
			for (int i = 0; i < numBatchOperations; i++)
			{
				_batchOperations[i].Render();
			}
			numBatchOperations = 0;
			_batchedVertexIndex = 0;
			_batchedVertexCount = 0;
		}
	}

	private void PushBatchOperation()
	{
		if (_batchedVertexIndex == _batchedVertexCount)
		{
			return;
		}
		if (numBatchOperations >= _batchOperations.Length)
		{
			BatchOperation[] array = new BatchOperation[_batchOperations.Length * 2];
			for (int i = 0; i < _batchOperations.Length; i++)
			{
				array[i] = _batchOperations[i];
			}
			_batchOperations = array;
		}
		if (_batchOperations[numBatchOperations] == null)
		{
			_batchOperations[numBatchOperations] = new BatchOperation(this, _primitiveType, _batchedVertexIndex, _batchedVertexCount - _batchedVertexIndex, _blendMode, _clipRectangle, _textures, _modelMatrix, _additiveColour);
		}
		else
		{
			_batchOperations[numBatchOperations]._simpleRenderer = this;
			_batchOperations[numBatchOperations]._primitiveType = _primitiveType;
			_batchOperations[numBatchOperations]._vertexBufferIndex = _batchedVertexIndex;
			_batchOperations[numBatchOperations]._vertexCount = _batchedVertexCount - _batchedVertexIndex;
			_batchOperations[numBatchOperations]._blendMode = _blendMode;
			_batchOperations[numBatchOperations]._clipRectangle = _clipRectangle;
			_batchOperations[numBatchOperations]._textures = _textures.ToArray();
			_batchOperations[numBatchOperations]._modelMatrix = _modelMatrix;
			_batchOperations[numBatchOperations]._additiveColour = _additiveColour;
		}
		numBatchOperations++;
		_batchedVertexIndex = _batchedVertexCount;
	}

	private void PushVertices(IReadOnlyList<Vector2> positions)
	{
		Colour[] array = new Colour[positions.Count];
		Vector2[] textureMappings = new Vector2[positions.Count];
		for (int i = 0; i < positions.Count; i++)
		{
			array[i] = Colour;
		}
		PushVertices(positions, array, textureMappings);
	}

	public void PushVertices(IReadOnlyList<Vector2> positions, IReadOnlyList<Colour> colours, IReadOnlyList<Vector2> textureMappings)
	{
		_renderer.ActivateRenderer(this);
		if (_batchedVertexCount == 0)
		{
			_batchedVertexIndex = 0;
			numVerticesData = 0;
		}
		for (int i = 0; i < positions.Count; i++)
		{
			_verticesData[numVerticesData].position.x = (float)positions[i].X;
			_verticesData[numVerticesData].position.y = (float)positions[i].Y;
			_verticesData[numVerticesData].colour.r = colours[i].Red / 255f;
			_verticesData[numVerticesData].colour.g = colours[i].Green / 255f;
			_verticesData[numVerticesData].colour.b = colours[i].Blue / 255f;
			_verticesData[numVerticesData].colour.a = colours[i].Alpha / 255f;
			_verticesData[numVerticesData].texcoords.s = (float)textureMappings[i].X;
			_verticesData[numVerticesData].texcoords.t = (float)textureMappings[i].Y;
			numVerticesData++;
			if (numVerticesData >= _verticesData.Length)
			{
				Vertex[] array = new Vertex[_verticesData.Length * 2];
				Array.Copy(_verticesData, array, _verticesData.Length);
				_verticesData = array;
			}
		}
		_batchedVertexCount += positions.Count;
		if (_batchedVertexCount > 1024)
		{
			Render();
		}
	}

	public IDisposable BeginMatixState()
	{
		return new MatrixState(this, _modelMatrix, _clipRectangle);
	}

	public void SetVertices(Rectangle rectangle)
	{
		_vertices = new Vector2[4]
		{
			new Vector2(rectangle.Left, rectangle.Top),
			new Vector2(rectangle.Left, rectangle.Bottom),
			new Vector2(rectangle.Right, rectangle.Bottom),
			new Vector2(rectangle.Right, rectangle.Top)
		};
	}

	public void SetVerticesFromTextureSize(Vector2 position)
	{
		ITexture texture = Texture;
		if (texture == null)
		{
			throw new InvalidOperationException("Texture not set");
		}
		SetVertices(new Rectangle(position.X, position.Y, texture.Width, texture.Height));
	}

	public void RenderTexture(ITexture texture, Vector2 destination, bool flipX = false, bool flipY = false)
	{
		RenderTexture(texture, new Rectangle(0.0, 0.0, texture.Width, texture.Height), destination, flipX, flipY);
	}

	public void RenderTexture(ITexture texture, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		RenderTexture(texture, new Rectangle(0.0, 0.0, texture.Width, texture.Height), destination, flipX, flipY);
	}

	public void RenderTexture(ITexture texture, Rectangle source, Vector2 destination, bool flipX = false, bool flipY = false)
	{
		RenderTexture(texture, source, new Rectangle(destination.X - (int)(texture.Width / 2.0), destination.Y - (int)(texture.Height / 2.0), texture.Width, texture.Height), flipX, flipY);
	}

	public void RenderTexture(IEnumerable<ITexture> textures, Vector2 destination)
	{
		ITexture texture = textures.First();
		RenderTexture(textures, new Rectangle(0.0, 0.0, texture.Width, texture.Height), destination);
	}

	public void RenderTexture(IEnumerable<ITexture> textures, Rectangle destination)
	{
		ITexture texture = textures.First();
		RenderTexture(textures, new Rectangle(0.0, 0.0, texture.Width, texture.Height), destination);
	}

	public void RenderTexture(IEnumerable<ITexture> textures, Rectangle source, Vector2 destination)
	{
		ITexture texture = textures.First();
		RenderTexture(textures, source, new Rectangle(destination.X - texture.Width / 2.0, destination.Y - texture.Height / 2.0, texture.Width, texture.Height));
	}

	public void RenderTexture(ITexture texture, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		RenderTexture(new ITexture[1] { texture }, source, destination, flipX, flipY);
	}

	public void RenderTexture(IEnumerable<ITexture> textures, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		if (textures.Count() == 0)
		{
			throw new ArgumentException("No textures specified", "textures");
		}
		Renderer.GetVertices(destination, vertexPositions);
		ITexture texture = textures.First();
		if (_modelMatrix.M11 != 1.0 || _modelMatrix.M22 != 1.0)
		{
			Renderer.GetTextureMappingsHalfIn(texture, source, ref vertexUVs, flipX, flipY);
		}
		else
		{
			Renderer.GetTextureMappings(texture, source, vertexUVs, flipX, flipY);
		}
		Textures = textures;
		PushVertices(vertexPositions, new Colour[4] { Colour, Colour, Colour, Colour }, vertexUVs);
	}

	public void RenderQuad(Colour colour, Rectangle rectangle)
	{
		Colour = colour;
		Texture = null;
		PushVertices(new Vector2[4]
		{
			new Vector2(rectangle.Left, rectangle.Top),
			new Vector2(rectangle.Left, rectangle.Bottom),
			new Vector2(rectangle.Right, rectangle.Bottom),
			new Vector2(rectangle.Right, rectangle.Top)
		});
	}

	public void RenderQuad(Colour colour, Vector2[] points)
	{
		Colour = colour;
		Texture = null;
		PushVertices(points);
	}

	public void RenderEllipse(Colour colour, Vector2 centre, double innerRadius, double outerRadius, int sectors)
	{
		Colour = colour;
		Texture = null;
		Vector2[] array = new Vector2[sectors * 4];
		int num = 0;
		for (int i = 0; i < sectors; i++)
		{
			double num2 = i * (Math.PI * 2.0) / sectors;
			double num3 = (i + 1) * (Math.PI * 2.0) / sectors;
			array[num++] = new Vector2(Math.Sin(num2) * outerRadius, (0.0 - Math.Cos(num2)) * outerRadius) + centre;
			array[num++] = new Vector2(Math.Sin(num2) * innerRadius, (0.0 - Math.Cos(num2)) * innerRadius) + centre;
			array[num++] = new Vector2(Math.Sin(num3) * innerRadius, (0.0 - Math.Cos(num3)) * innerRadius) + centre;
			array[num++] = new Vector2(Math.Sin(num3) * outerRadius, (0.0 - Math.Cos(num3)) * outerRadius) + centre;
		}
		PushVertices(array);
	}

	public void RenderRectangle(Colour colour, Rectangle rect, double thickness)
	{
		double num = Math.Min(thickness, rect.Width);
		double num2 = Math.Min(thickness, rect.Height);
		RenderQuad(colour, new Rectangle(rect.X, rect.Y, num, rect.Height));
		RenderQuad(colour, new Rectangle(rect.Right - num, rect.Y, num, rect.Height));
		RenderQuad(colour, new Rectangle(rect.X + num, rect.Y, rect.Width - num * 2.0, num2));
		RenderQuad(colour, new Rectangle(rect.X + num, rect.Bottom - num2, rect.Width - num * 2.0, num2));
	}

	public void RenderLine(Colour colour, Vector2 a, Vector2 b, double thickness)
	{
		Colour = colour;
		Texture = null;
		a += new Vector2(0.5, 0.5);
		b += new Vector2(0.5, 0.5);
		thickness /= 2.0;
		double num = Math.Atan2(a.Y - b.Y, a.X - b.X) + Math.PI / 2.0;
		double num2 = Math.Atan2(a.Y - b.Y, a.X - b.X) - Math.PI / 2.0;
		PushVertices(new Vector2[4]
		{
			a + new Vector2(Math.Cos(num) * thickness, Math.Sin(num) * thickness),
			a + new Vector2(Math.Cos(num2) * thickness, Math.Sin(num2) * thickness),
			b + new Vector2(Math.Cos(num) * thickness, Math.Sin(num) * thickness),
			b + new Vector2(Math.Cos(num2) * thickness, Math.Sin(num2) * thickness)
		});
	}

	public Rectangle RenderText(TextRenderInfo textRenderInfo)
	{
		return TextRenderingHelpers.RenderWith2d(this, textRenderInfo);
	}

	public Rectangle MeasureText(TextRenderInfo textRenderInfo)
	{
		return TextRenderingHelpers.MeasureWith2d(this, textRenderInfo);
	}
}
