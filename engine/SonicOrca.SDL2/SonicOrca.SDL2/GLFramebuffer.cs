using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

internal class GLFramebuffer : IFramebuffer, IDisposable
{
	private readonly GLGraphicsContext _context;

	private readonly int _width;

	private readonly int _height;

	private readonly uint _name;

	private readonly GLTexture[] _textures;

	private readonly int _depthBufferName;

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

	public GLFramebuffer(GLGraphicsContext context, int width, int height, int numTextures = 1)
	{
		_context = context;
		_width = width;
		_height = height;
		GL.GenFramebuffers(1, out _name);
		GL.BindFramebuffer(FramebufferTarget.Framebuffer, _name);
		_textures = new GLTexture[numTextures];
		for (int i = 0; i < numTextures; i++)
		{
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, (FramebufferAttachment)(36064 + i), (_textures[i] = (GLTexture)context.CreateTexture(width, height)).Id, 0);
		}
		GL.DrawBuffers(numTextures, (from num in Enumerable.Range(0, numTextures)
									 select (DrawBuffersEnum)(36064 + num)).ToArray());
		_depthBufferName = GL.GenRenderbuffer();
		GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBufferName);
		GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
		GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _depthBufferName);
		if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
		{
			throw new SDL2Exception("Unable to create framebuffer.");
		}
		_context.Framebuffers.Add(this);
	}

	public void Dispose()
	{
		GLTexture[] textures = _textures;
		for (int i = 0; i < textures.Length; i++)
		{
			textures[i].Dispose();
		}
		GL.DeleteFramebuffer(_name);
		if (_context != null)
		{
			_context.Framebuffers.Remove(this);
		}
	}

	public void Activate()
	{
		GL.BindFramebuffer(FramebufferTarget.Framebuffer, _name);
		GL.Viewport(0, 0, _width, _height);
		_context.CurrentFramebuffer = this;
	}
}
