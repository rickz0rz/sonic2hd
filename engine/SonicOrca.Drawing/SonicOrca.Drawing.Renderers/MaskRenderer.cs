using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

internal class MaskRenderer : IMaskRenderer, IDisposable, IRenderer
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;

		[VertexAttribute("VertexMaskTextureMapping")]
		public vec2 masktexcoords;
	}

	private class MatrixState : IDisposable
	{
		private readonly MaskRenderer _maskRenderer;

		private readonly Matrix4 _maskMatrix;

		private readonly Matrix4 _targetMatrix;

		private readonly Matrix4 _intersectionMatrix;

		public MatrixState(MaskRenderer maskRenderer, Matrix4 intersectionMatrix, Matrix4 maskMatrix, Matrix4 targetMatrix)
		{
			_maskRenderer = maskRenderer;
			_intersectionMatrix = intersectionMatrix;
			_maskMatrix = maskMatrix;
			_targetMatrix = targetMatrix;
		}

		public void Dispose()
		{
			_maskRenderer.IntersectionModelMatrix = _intersectionMatrix;
			_maskRenderer.MaskModelMatrix = _maskMatrix;
			_maskRenderer.TargetModelMatrix = _targetMatrix;
		}
	}

	private readonly IGraphicsContext _graphicsContext;

	private ManagedShaderProgram _shaderProgram;

	private IBuffer _vbo;

	private IVertexArray _vao;

	private Vertex[] _vertices = new Vertex[4];

	private Matrix4 _maskModelMatrix;

	private Matrix4 _targetModelMatrix;

	private Matrix4 _intersectionModelMatrix;

	private static readonly Dictionary<Renderer, MaskRenderer> RendererDictionary = new Dictionary<Renderer, MaskRenderer>();

	public ITexture Texture { get; set; }

	public Rectanglei Source { get; set; }

	public Rectanglei Destination { get; set; }

	public ITexture MaskTexture { get; set; }

	public Rectanglei MaskSource { get; set; }

	public Rectanglei MaskDestination { get; set; }

	public BlendMode BlendMode { get; set; } = BlendMode.Alpha;

	public Colour Colour { get; set; } = Colours.White;

	public Matrix4 MaskModelMatrix
	{
		get
		{
			return _maskModelMatrix;
		}
		set
		{
			if (_maskModelMatrix != value)
			{
				_maskModelMatrix = value;
			}
		}
	}

	public Matrix4 IntersectionModelMatrix
	{
		get
		{
			return _intersectionModelMatrix;
		}
		set
		{
			if (_intersectionModelMatrix != value)
			{
				_intersectionModelMatrix = value;
			}
		}
	}

	public Matrix4 TargetModelMatrix
	{
		get
		{
			return _targetModelMatrix;
		}
		set
		{
			if (_targetModelMatrix != value)
			{
				_targetModelMatrix = value;
			}
		}
	}

	public static MaskRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new MaskRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	public MaskRenderer(Renderer renderer)
	{
		_graphicsContext = renderer.Window.GraphicsContext;
		renderer.RegisterRenderer(this);
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/mask.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		_shaderProgram.Program.Activate();
		_shaderProgram.Program.SetUniform("InputTexture", 0);
		_shaderProgram.Program.SetUniform("InputTextureMask", 1);
		_intersectionModelMatrix = Matrix4.Identity;
		_targetModelMatrix = Matrix4.Identity;
		_maskModelMatrix = Matrix4.Identity;
	}

	public void Dispose()
	{
		_vao.Dispose();
		_vbo.Dispose();
		_shaderProgram.Dispose();
	}

	public IDisposable BeginMatixState()
	{
		return new MatrixState(this, _intersectionModelMatrix, _maskModelMatrix, _targetModelMatrix);
	}

	public void Render(bool maskColorMultiply = false)
	{
		IGraphicsContext graphicsContext = _graphicsContext;
		IShaderProgram program = _shaderProgram.Program;
		Rectanglei clippedDestination = Rectanglei.Intersect(Destination, MaskDestination);
		if (Texture == null)
		{
			throw new InvalidOperationException("Texture was null.");
		}
		int num = 0;
		if (MaskTexture != null)
		{
			num++;
			if (clippedDestination.Width <= 0 || clippedDestination.Height <= 0)
			{
				return;
			}
		}
		else
		{
			clippedDestination = Destination;
		}
		_vertices[0].position.x = clippedDestination.Left;
		_vertices[0].position.y = clippedDestination.Top;
		_vertices[1].position.x = clippedDestination.Left;
		_vertices[1].position.y = clippedDestination.Bottom;
		_vertices[2].position.x = clippedDestination.Right;
		_vertices[2].position.y = clippedDestination.Bottom;
		_vertices[3].position.x = clippedDestination.Right;
		_vertices[3].position.y = clippedDestination.Top;
		GetClippedSourceRect(Texture, Source, Destination, clippedDestination, out var left, out var top, out var right, out var bottom);
		ITexture texture = Texture;
		_ = texture.Width;
		_ = texture.Height;
		_vertices[0].texcoords.s = left;
		_vertices[0].texcoords.t = top;
		_vertices[1].texcoords.s = left;
		_vertices[1].texcoords.t = bottom;
		_vertices[2].texcoords.s = right;
		_vertices[2].texcoords.t = bottom;
		_vertices[3].texcoords.s = right;
		_vertices[3].texcoords.t = top;
		if (num > 0)
		{
			GetClippedSourceRect(MaskTexture, MaskSource, MaskDestination, clippedDestination, out var left2, out var top2, out var right2, out var bottom2);
			_vertices[0].masktexcoords.s = left2;
			_vertices[0].masktexcoords.t = top2;
			_vertices[1].masktexcoords.s = left2;
			_vertices[1].masktexcoords.t = bottom2;
			_vertices[2].masktexcoords.s = right2;
			_vertices[2].masktexcoords.t = bottom2;
			_vertices[3].masktexcoords.s = right2;
			_vertices[3].masktexcoords.t = top2;
		}
		_vbo.SetData(_vertices);
		graphicsContext.BlendMode = BlendMode;
		graphicsContext.SetTexture(0, Texture);
		graphicsContext.SetTexture(1, MaskTexture);
		program.Activate();
		program.SetUniform("IntersectionModelViewMatrix", _intersectionModelMatrix);
		program.SetUniform("MaskModelViewMatrix", _maskModelMatrix);
		program.SetUniform("TargetModelViewMatrix", _targetModelMatrix);
		program.SetUniform("ProjectionMatrix", _graphicsContext.CurrentFramebuffer.CreateOrthographic());
		program.SetUniform("MaskInput", num);
		program.SetUniform("InputColour", Colour);
		program.SetUniform("MaskColorMultiply", maskColorMultiply ? 1 : 0);
		_vao.Render(PrimitiveType.Quads, _vertices);
	}

	private void GetClippedSourceRect(ITexture sourceTexture, Rectanglei source, Rectanglei destination, Rectanglei clippedDestination, out float left, out float top, out float right, out float bottom)
	{
		left = source.Left / (float)sourceTexture.Width;
		top = source.Top / (float)sourceTexture.Height;
		right = source.Right / (float)sourceTexture.Width;
		bottom = source.Bottom / (float)sourceTexture.Height;
		float num = right - left;
		float num2 = bottom - top;
		left += (clippedDestination.Left - destination.Left) / (float)destination.Width / num;
		top += (clippedDestination.Top - destination.Top) / (float)destination.Height / num2;
		right -= (destination.Right - clippedDestination.Right) / (float)destination.Width / num2;
		bottom -= (destination.Bottom - clippedDestination.Bottom) / (float)destination.Height / num2;
	}

	public void Deactivate()
	{
	}
}
