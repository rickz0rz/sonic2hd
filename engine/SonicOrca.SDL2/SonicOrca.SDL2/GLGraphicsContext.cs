using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Core.Debugging;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

/// <summary>
/// The OpenGL implementation of <see cref="IGraphicsContext"/>. GL objects must be created on the window's context
/// thread: textures requested from other threads (resource loading) are created there through the window's dispatch
/// queue.
/// </summary>
internal class GLGraphicsContext : IGraphicsContext
{
	private readonly IGLWindow _window;

	private BlendMode _blendMode;

	private readonly List<ITexture> _textures = new List<ITexture>();

	/// <summary>The texture units from 0 that can have a texture bound.</summary>
	private int _boundTextureUnits;

	private readonly List<IShaderProgram> _shaderPrograms = new List<IShaderProgram>();

	private readonly List<VertexBuffer> _vertexBuffers = new List<VertexBuffer>();

	private readonly List<IFramebuffer> _renderTargets = new List<IFramebuffer>();

	internal ICollection<ITexture> Textures => _textures;

	internal ICollection<IShaderProgram> ShaderPrograms => _shaderPrograms;

	internal ICollection<VertexBuffer> VertexBuffers => _vertexBuffers;

	internal ICollection<IFramebuffer> Framebuffers => _renderTargets;

	IReadOnlyCollection<ITexture> IGraphicsContext.Textures => _textures;

	IReadOnlyCollection<IShaderProgram> IGraphicsContext.ShaderPrograms => _shaderPrograms;

	IReadOnlyCollection<VertexBuffer> IGraphicsContext.VertexBuffers => _vertexBuffers;

	IReadOnlyCollection<IFramebuffer> IGraphicsContext.RenderTargets => _renderTargets;

	public bool DepthTesting
	{
		get
		{
			return GL.IsEnabled(EnableCap.DepthTest);
		}
		set
		{
			if (value)
			{
				GL.Enable(EnableCap.DepthTest);
			}
			else
			{
				GL.Disable(EnableCap.DepthTest);
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
				_blendMode = value;
				switch (value)
				{
					default:
						GL.Disable(EnableCap.Blend);
						break;
					case BlendMode.Alpha:
						GL.Enable(EnableCap.Blend);
						GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
						break;
					case BlendMode.Additive:
						GL.Enable(EnableCap.Blend);
						GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
						break;
				}
			}
		}
	}

	// Point, Line and Fill have the same order as GL_POINT, GL_LINE and GL_FILL
	public SonicOrca.Graphics.PolygonMode PolygonMode
	{
		get
		{
			GL.GetInteger(GetPName.PolygonMode, out int mode);
			return (SonicOrca.Graphics.PolygonMode)(mode - (int)OpenTK.Graphics.OpenGL.PolygonMode.Point);
		}
		set
		{
			GL.PolygonMode(TriangleFace.FrontAndBack, OpenTK.Graphics.OpenGL.PolygonMode.Point + (int)value);
		}
	}

	public IFramebuffer CurrentFramebuffer { get; internal set; }

	public GLGraphicsContext(IGLWindow window)
	{
		_window = window;
	}

	public IShader CreateShader(SonicOrca.Graphics.ShaderType type, string sourceCode)
	{
		long start = FrameProfiler.Timestamp();
		GLShader shader = new GLShader(type, sourceCode);
		FrameProfiler.Note("Compile " + type + " shader", start);
		return shader;
	}

	public IShaderProgram CreateShaderProgram(params IShader[] shaders)
	{
		return CreateShaderProgram((IEnumerable<IShader>)shaders);
	}

	public IShaderProgram CreateShaderProgram(IEnumerable<IShader> shaders)
	{
		long start = FrameProfiler.Timestamp();
		GLShaderProgram program = new GLShaderProgram(this, shaders.Cast<GLShader>());
		FrameProfiler.Note("Link shader program", start);
		return program;
	}

	public VertexBuffer CreateVertexBuffer(params int[] vectorCounts)
	{
		return CreateVertexBuffer((IEnumerable<int>)vectorCounts);
	}

	public VertexBuffer CreateVertexBuffer(IEnumerable<int> vectorCounts)
	{
		return new GLVertexBuffer(this, vectorCounts);
	}

	public VertexBuffer CreateVertexBuffer(IShaderProgram shaderProgram, IEnumerable<string> names, IEnumerable<int> vectorCounts)
	{
		return new GLVertexBuffer(this, shaderProgram, names, vectorCounts);
	}

	public ITexture CreateTexture(int width, int height)
	{
		return CreateTexture(width, height, 4, new byte[width * height * 4]);
	}

	public ITexture CreateTexture(int width, int height, int channels, byte[] pixels, bool toCompress = false)
	{
		if (Thread.CurrentThread == _window.ContextThread)
		{
			long start = FrameProfiler.Timestamp();
			GLTexture created = new GLTexture(this, width, height, channels, pixels, toCompress);
			FrameProfiler.Note($"Create {width}x{height} texture", start, 2.0);
			return created;
		}
		// Called from a loader thread: create the texture on the context thread and wait for it
		GLTexture texture = null;
		using AutoResetEvent textureCreated = new AutoResetEvent(initialState: false);
		_window.Dispatch(() =>
		{
			long start = FrameProfiler.Timestamp();
			texture = new GLTexture(this, width, height, channels, pixels, toCompress);
			FrameProfiler.Note($"Create {width}x{height} texture (dispatched)", start, 2.0);
			textureCreated.Set();
		});
		if (!textureCreated.WaitOne(5000))
		{
			throw new TimeoutException("OpenGL texture creation timed out.");
		}
		return texture;
	}

	public void SetTexture(ITexture texture)
	{
		SetTexture(0, texture);
	}

	public void SetTexture(int index, ITexture texture)
	{
		if (texture != null)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + index);
			GL.BindTexture(TextureTarget.Texture2D, ((GLTexture)texture).Id);
			_boundTextureUnits = Math.Max(_boundTextureUnits, index + 1);
		}
	}

	/// <summary>
	/// Binds the textures to the units from 0, and unbinds the units after them. An old texture must not stay bound.
	/// A shader sampler that gets no texture reads unit 0. When the old texture is also the target of the draw,
	/// WebGL refuses the draw as a feedback loop. For example, the canvas of the last frame stays bound, and the
	/// first untextured quad of the next frame draws to that canvas.
	/// </summary>
	public void SetTextures(IEnumerable<ITexture> textures)
	{
		int index = 0;
		foreach (ITexture texture in textures)
		{
			SetTexture(index++, texture);
		}
		for (int unit = index; unit < _boundTextureUnits; unit++)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + unit);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
		_boundTextureUnits = index;
	}

	public IFramebuffer CreateFrameBuffer(int width, int height, int numTextures = 1)
	{
		long start = FrameProfiler.Timestamp();
		GLFramebuffer framebuffer = new GLFramebuffer(this, width, height, numTextures);
		FrameProfiler.Note($"Create {width}x{height} framebuffer", start);
		return framebuffer;
	}

	public void RenderToBackBuffer()
	{
		CurrentFramebuffer = GLFramebuffer.FromBackBuffer(this, _window.ClientSize.X, _window.ClientSize.Y);
		GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		GL.Viewport(0, 0, _window.ClientSize.X, _window.ClientSize.Y);
	}

	public void Update()
	{
	}

	public void ClearBuffer()
	{
		GL.Clear(ClearBufferMask.StencilBufferBit | ClearBufferMask.ColorBufferBit);
	}

	public void ClearDepthBuffer()
	{
		GL.Clear(ClearBufferMask.DepthBufferBit);
	}

	public void SetStencilTest(bool enabled)
	{
		if (enabled)
		{
			GL.Enable(EnableCap.StencilTest);
		}
		else
		{
			GL.Disable(EnableCap.StencilTest);
		}
	}

	public void ClearStencil()
	{
		GL.Clear(ClearBufferMask.StencilBufferBit);
	}

	public void SetStencilFunction(SonicOrca.Graphics.StencilFunction function, int reference, int mask)
	{
		GL.StencilFunc(function == SonicOrca.Graphics.StencilFunction.Equal ? OpenTK.Graphics.OpenGL.StencilFunction.Equal : OpenTK.Graphics.OpenGL.StencilFunction.Always, reference, mask);
	}

	public void SetStencilOperation(StencilOperation stencilFail, StencilOperation depthFail, StencilOperation pass)
	{
		GL.StencilOp(ToGL(stencilFail), ToGL(depthFail), ToGL(pass));
	}

	public void SetStencilWriteMask(int mask)
	{
		GL.StencilMask(mask);
	}

	private static StencilOp ToGL(StencilOperation operation)
	{
		return operation switch
		{
			StencilOperation.Zero => StencilOp.Zero,
			StencilOperation.Replace => StencilOp.Replace,
			_ => StencilOp.Keep,
		};
	}

	public void ClearColourBuffer(int index)
	{
		GL.ClearBuffer(OpenTK.Graphics.OpenGL.ClearBuffer.Color, index, new int[4]);
	}

	public IBuffer CreateBuffer()
	{
		return new GLBuffer();
	}

	public IVertexArray CreateVertexArray()
	{
		return new GLVertexArray();
	}
}
