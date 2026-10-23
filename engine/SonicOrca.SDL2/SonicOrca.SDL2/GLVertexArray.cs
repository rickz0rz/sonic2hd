using System;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

internal class GLVertexArray : IVertexArray, IDisposable
{
	private readonly GLGraphicsContext _context;

	private readonly int _id;

	public GLVertexArray(GLGraphicsContext context)
	{
		_context = context;
		_id = GL.GenVertexArray();
	}

	public void Dispose()
	{
		GL.DeleteVertexArray(_id);
	}

	public void Bind()
	{
		GL.BindVertexArray(_id);
	}

	public void DefineAttribute(int attributeLocation, VertexAttributePointerType type, int size, int stride, int offset)
	{
		GL.VertexAttribPointer(attributeLocation, size, (VertexAttribPointerType)type, normalized: false, stride, offset);
		GL.EnableVertexAttribArray(attributeLocation);
	}

	public void Render(SonicOrca.Graphics.PrimitiveType type, int index, int count)
	{
		Bind();
		GLPrimitives.Draw(type, index, count);
	}
}
