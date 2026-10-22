using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

internal class GLVertexArray : IVertexArray, IDisposable
{
	private static readonly IReadOnlyList<OpenTK.Graphics.OpenGL.PrimitiveType> BeginModesForPrimitiveTypes = new OpenTK.Graphics.OpenGL.PrimitiveType[10]
	{
		OpenTK.Graphics.OpenGL.PrimitiveType.Points,
		OpenTK.Graphics.OpenGL.PrimitiveType.Lines,
		OpenTK.Graphics.OpenGL.PrimitiveType.LineStrip,
		OpenTK.Graphics.OpenGL.PrimitiveType.LineLoop,
		OpenTK.Graphics.OpenGL.PrimitiveType.Triangles,
		OpenTK.Graphics.OpenGL.PrimitiveType.TriangleStrip,
		OpenTK.Graphics.OpenGL.PrimitiveType.TriangleFan,
		OpenTK.Graphics.OpenGL.PrimitiveType.Quads,
		OpenTK.Graphics.OpenGL.PrimitiveType.QuadStrip,
		OpenTK.Graphics.OpenGL.PrimitiveType.Polygon
	};

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
		GL.DrawArrays(BeginModesForPrimitiveTypes[(int)type], index, count);
	}
}
