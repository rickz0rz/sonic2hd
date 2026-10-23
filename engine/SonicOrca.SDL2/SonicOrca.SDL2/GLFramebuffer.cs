using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

/// <summary>
/// An OpenGL framebuffer with one or more colour textures and a depth/stencil buffer, or the window's back buffer.
/// </summary>
internal class GLFramebuffer : IFramebuffer, IDisposable
{
	private readonly GLGraphicsContext _context;

	private readonly int _width;

	private readonly int _height;

	/// <summary>The OpenGL framebuffer name. 0 is the window's back buffer.</summary>
	private readonly uint _name;

	/// <summary>The colour textures. Null for the back buffer.</summary>
	private readonly GLTexture[] _textures;

	private readonly int _depthStencilBufferName;

	public int Width => _width;

	public int Height => _height;

	public IReadOnlyList<ITexture> Textures => _textures;

	public static GLFramebuffer FromBackBuffer(GLGraphicsContext context, int width, int height)
	{
		return new GLFramebuffer(context, width, height, 0u);
	}

	private GLFramebuffer(GLGraphicsContext context, int width, int height, uint name)
	{
		_context = context;
		_width = width;
		_height = height;
		_name = name;
	}

	public GLFramebuffer(GLGraphicsContext context, int width, int height, int textureCount = 1)
	{
		_context = context;
		_width = width;
		_height = height;
		GL.GenFramebuffers(1, out _name);
		GL.BindFramebuffer(FramebufferTarget.Framebuffer, _name);
		_textures = new GLTexture[textureCount];
		for (int i = 0; i < textureCount; i++)
		{
			_textures[i] = (GLTexture)context.CreateTexture(width, height);
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, _textures[i].Id, 0);
		}
		GL.DrawBuffers(textureCount, Enumerable.Range(0, textureCount).Select(i => DrawBuffersEnum.ColorAttachment0 + i).ToArray());
		_depthStencilBufferName = GL.GenRenderbuffer();
		GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthStencilBufferName);
		GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
		GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _depthStencilBufferName);
		if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
		{
			throw new SDL2Exception("Unable to create framebuffer.");
		}
		_context.Framebuffers.Add(this);
	}

	public void Dispose()
	{
		if (_textures != null)
		{
			foreach (GLTexture texture in _textures)
			{
				texture.Dispose();
			}
		}
		GL.DeleteFramebuffer(_name);
		_context?.Framebuffers.Remove(this);
	}

	public void Activate()
	{
		GL.BindFramebuffer(FramebufferTarget.Framebuffer, _name);
		GL.Viewport(0, 0, _width, _height);
		_context.CurrentFramebuffer = this;
	}
}
