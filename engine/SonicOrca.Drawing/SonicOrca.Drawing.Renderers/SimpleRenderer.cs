using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Batches 2D primitives (textured quads, coloured quads, lines and ellipses) and draws them when the renderer is deactivated or the batch is full.
/// </summary>
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

	/// <summary>
	/// Holds a range of batched vertices and the render state to draw them with. The renderer reuses instances between frames.
	/// </summary>
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

	/// <summary>
	/// Puts the model matrix and the clip rectangle back to their saved values when it is disposed.
	/// </summary>
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

	// Draw the batch when it has more vertices than this
	private const int MaxBatchedVertexCount = 1024;

	private static readonly Dictionary<Renderer, SimpleRenderer> RendererDictionary = new();

	private readonly Renderer _renderer;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IBuffer _vbo;

	private readonly IVertexArray _vao;

	private Vertex[] _verticesData = new Vertex[20];

	private int _vertexDataCount;

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

	private int _batchOperationCount;

	private int _batchedVertexIndex;

	private int _batchedVertexCount;

	private Vector2[] _vertexPositions = new Vector2[4];

	private Vector2[] _vertexUVs = new Vector2[4];

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
				// Note: this condition looks inverted. A different texture is not stored.
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
			ITexture[] newTextures = value.ToArray();
			if (_textures.Length != newTextures.Length)
			{
				PushBatchOperation();
				_textures = newTextures;
				return;
			}
			bool changed = false;
			for (int i = 0; i < _textures.Length; i++)
			{
				if (_textures[i] != newTextures[i])
				{
					changed = true;
					break;
				}
			}
			if (changed)
			{
				PushBatchOperation();
				_textures = newTextures;
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
			_vbo.SetData(_verticesData, 0, _vertexDataCount);
			_projectionMatrix = _renderer.Window.GraphicsContext.CurrentFramebuffer.CreateOrthographic();
			for (int i = 0; i < _batchOperationCount; i++)
			{
				_batchOperations[i].Render();
			}
			_batchOperationCount = 0;
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
		if (_batchOperationCount >= _batchOperations.Length)
		{
			BatchOperation[] expandedOperations = new BatchOperation[_batchOperations.Length * 2];
			for (int i = 0; i < _batchOperations.Length; i++)
			{
				expandedOperations[i] = _batchOperations[i];
			}
			_batchOperations = expandedOperations;
		}
		BatchOperation operation = _batchOperations[_batchOperationCount];
		if (operation == null)
		{
			_batchOperations[_batchOperationCount] = new BatchOperation(this, _primitiveType, _batchedVertexIndex, _batchedVertexCount - _batchedVertexIndex, _blendMode, _clipRectangle, _textures, _modelMatrix, _additiveColour);
		}
		else
		{
			operation._simpleRenderer = this;
			operation._primitiveType = _primitiveType;
			operation._vertexBufferIndex = _batchedVertexIndex;
			operation._vertexCount = _batchedVertexCount - _batchedVertexIndex;
			operation._blendMode = _blendMode;
			operation._clipRectangle = _clipRectangle;
			operation._textures = _textures.ToArray();
			operation._modelMatrix = _modelMatrix;
			operation._additiveColour = _additiveColour;
		}
		_batchOperationCount++;
		_batchedVertexIndex = _batchedVertexCount;
	}

	private void PushVertices(IReadOnlyList<Vector2> positions)
	{
		Colour[] colours = new Colour[positions.Count];
		Vector2[] textureMappings = new Vector2[positions.Count];
		for (int i = 0; i < positions.Count; i++)
		{
			colours[i] = Colour;
		}
		PushVertices(positions, colours, textureMappings);
	}

	public void PushVertices(IReadOnlyList<Vector2> positions, IReadOnlyList<Colour> colours, IReadOnlyList<Vector2> textureMappings)
	{
		_renderer.ActivateRenderer(this);
		if (_batchedVertexCount == 0)
		{
			_batchedVertexIndex = 0;
			_vertexDataCount = 0;
		}
		for (int i = 0; i < positions.Count; i++)
		{
			_verticesData[_vertexDataCount].position.x = (float)positions[i].X;
			_verticesData[_vertexDataCount].position.y = (float)positions[i].Y;
			_verticesData[_vertexDataCount].colour.r = colours[i].Red / 255f;
			_verticesData[_vertexDataCount].colour.g = colours[i].Green / 255f;
			_verticesData[_vertexDataCount].colour.b = colours[i].Blue / 255f;
			_verticesData[_vertexDataCount].colour.a = colours[i].Alpha / 255f;
			_verticesData[_vertexDataCount].texcoords.s = (float)textureMappings[i].X;
			_verticesData[_vertexDataCount].texcoords.t = (float)textureMappings[i].Y;
			_vertexDataCount++;
			if (_vertexDataCount >= _verticesData.Length)
			{
				Vertex[] expandedVertices = new Vertex[_verticesData.Length * 2];
				Array.Copy(_verticesData, expandedVertices, _verticesData.Length);
				_verticesData = expandedVertices;
			}
		}
		_batchedVertexCount += positions.Count;
		if (_batchedVertexCount > MaxBatchedVertexCount)
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
			throw new ArgumentException("No textures specified", nameof(textures));
		}
		Renderer.GetVertices(destination, _vertexPositions);
		ITexture texture = textures.First();
		// Use half-texel-inset mappings when the model matrix scales the texture
		if (_modelMatrix.M11 != 1.0 || _modelMatrix.M22 != 1.0)
		{
			Renderer.GetTextureMappingsHalfIn(texture, source, ref _vertexUVs, flipX, flipY);
		}
		else
		{
			Renderer.GetTextureMappings(texture, source, _vertexUVs, flipX, flipY);
		}
		Textures = textures;
		PushVertices(_vertexPositions, new Colour[4] { Colour, Colour, Colour, Colour }, _vertexUVs);
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
		// One quad for each sector. The angle starts at the top and goes clockwise.
		Vector2[] points = new Vector2[sectors * 4];
		int pointIndex = 0;
		for (int i = 0; i < sectors; i++)
		{
			double startAngle = i * (Math.PI * 2.0) / sectors;
			double endAngle = (i + 1) * (Math.PI * 2.0) / sectors;
			points[pointIndex++] = new Vector2(Math.Sin(startAngle) * outerRadius, (0.0 - Math.Cos(startAngle)) * outerRadius) + centre;
			points[pointIndex++] = new Vector2(Math.Sin(startAngle) * innerRadius, (0.0 - Math.Cos(startAngle)) * innerRadius) + centre;
			points[pointIndex++] = new Vector2(Math.Sin(endAngle) * innerRadius, (0.0 - Math.Cos(endAngle)) * innerRadius) + centre;
			points[pointIndex++] = new Vector2(Math.Sin(endAngle) * outerRadius, (0.0 - Math.Cos(endAngle)) * outerRadius) + centre;
		}
		PushVertices(points);
	}

	public void RenderRectangle(Colour colour, Rectangle rect, double thickness)
	{
		double sideThickness = Math.Min(thickness, rect.Width);
		double edgeThickness = Math.Min(thickness, rect.Height);
		// Left and right sides at full height, then the top and bottom edges between them
		RenderQuad(colour, new Rectangle(rect.X, rect.Y, sideThickness, rect.Height));
		RenderQuad(colour, new Rectangle(rect.Right - sideThickness, rect.Y, sideThickness, rect.Height));
		RenderQuad(colour, new Rectangle(rect.X + sideThickness, rect.Y, rect.Width - sideThickness * 2.0, edgeThickness));
		RenderQuad(colour, new Rectangle(rect.X + sideThickness, rect.Bottom - edgeThickness, rect.Width - sideThickness * 2.0, edgeThickness));
	}

	public void RenderLine(Colour colour, Vector2 a, Vector2 b, double thickness)
	{
		Colour = colour;
		Texture = null;
		a += new Vector2(0.5, 0.5);
		b += new Vector2(0.5, 0.5);
		thickness /= 2.0;
		// The two directions at right angles to the line
		double leftAngle = Math.Atan2(a.Y - b.Y, a.X - b.X) + Math.PI / 2.0;
		double rightAngle = Math.Atan2(a.Y - b.Y, a.X - b.X) - Math.PI / 2.0;
		// The corners go around the quad, so that the two triangles of the quad (0, 1, 2 and 0, 2, 3) fill it
		PushVertices(new Vector2[4]
		{
			a + new Vector2(Math.Cos(leftAngle) * thickness, Math.Sin(leftAngle) * thickness),
			a + new Vector2(Math.Cos(rightAngle) * thickness, Math.Sin(rightAngle) * thickness),
			b + new Vector2(Math.Cos(rightAngle) * thickness, Math.Sin(rightAngle) * thickness),
			b + new Vector2(Math.Cos(leftAngle) * thickness, Math.Sin(leftAngle) * thickness)
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
