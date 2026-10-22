using System;
using System.Runtime.InteropServices;
using SonicOrca.Extensions;
using SonicOrca.Graphics;
using SonicOrca.Graphics.LowLevel;

namespace SonicOrca.Drawing.Renderers;

internal class ClassicFadeTransitionRenderer : IFadeTransitionRenderer, IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Vertex
	{
		[VertexAttribute("VertexPosition")]
		public vec2 position;

		[VertexAttribute("VertexTextureMapping")]
		public vec2 texcoords;
	}

	private readonly IGraphicsContext _graphicsContext;

	private ManagedShaderProgram _shaderProgram;

	private IBuffer _vbo;

	private IVertexArray _vao;

	private Vertex[] _vertices = new Vertex[4];

	private IFramebuffer _pingPongBuffer;

	public float Opacity { get; set; }

	public ClassicFadeTransitionRenderer(IGraphicsContext graphicsContext)
	{
		_graphicsContext = graphicsContext;
		_shaderProgram = OrcaShader.CreateFromFile(_graphicsContext, "shaders/transition.shader");
		_vbo = _graphicsContext.CreateBuffer();
		_vao = _graphicsContext.CreateVertexArray();
		_vao.DefineAttributes(_shaderProgram.Program, _vbo, typeof(Vertex));
		_shaderProgram.Program.SetUniform("InputTexture", 0);
	}

	public void Dispose()
	{
		_vao.Dispose();
		_vbo.Dispose();
		_shaderProgram.Dispose();
		if (_pingPongBuffer != null)
		{
			_pingPongBuffer.Dispose();
		}
	}

	public void Render()
	{
		IFramebuffer currentFramebuffer = _graphicsContext.CurrentFramebuffer;
		if (_pingPongBuffer == null || _pingPongBuffer.Width != currentFramebuffer.Width || _pingPongBuffer.Height != currentFramebuffer.Height)
		{
			if (_pingPongBuffer != null)
			{
				_pingPongBuffer.Dispose();
			}
			_pingPongBuffer = _graphicsContext.CreateFrameBuffer(currentFramebuffer.Width, currentFramebuffer.Height);
		}
		Render(currentFramebuffer, _pingPongBuffer);
		Opacity = 0f;
		Render(_pingPongBuffer, currentFramebuffer);
	}

	public void Render(IFramebuffer sourceFramebuffer, IFramebuffer destFramebuffer)
	{
		IGraphicsContext graphicsContext = _graphicsContext;
		IShaderProgram program = _shaderProgram.Program;
		destFramebuffer.Activate();
		_vertices[0].position.x = 0f;
		_vertices[0].position.y = 0f;
		_vertices[1].position.x = 0f;
		_vertices[1].position.y = destFramebuffer.Height;
		_vertices[2].position.x = destFramebuffer.Width;
		_vertices[2].position.y = destFramebuffer.Height;
		_vertices[3].position.x = destFramebuffer.Width;
		_vertices[3].position.y = 0f;
		_vertices[0].texcoords.s = 0f;
		_vertices[0].texcoords.t = 1f;
		_vertices[1].texcoords.s = 0f;
		_vertices[1].texcoords.t = 0f;
		_vertices[2].texcoords.s = 1f;
		_vertices[2].texcoords.t = 0f;
		_vertices[3].texcoords.s = 1f;
		_vertices[3].texcoords.t = 1f;
		_vbo.SetData(_vertices);
		graphicsContext.BlendMode = BlendMode.Opaque;
		graphicsContext.SetTexture(sourceFramebuffer.Textures[0]);
		program.Activate();
		program.SetUniform("ProjectionMatrix", destFramebuffer.CreateOrthographic());
		program.SetUniform("InputDelta", Opacity);
		_vao.Render(PrimitiveType.Quads, _vertices);
	}
}
