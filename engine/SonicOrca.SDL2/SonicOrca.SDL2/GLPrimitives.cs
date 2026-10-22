using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace SonicOrca.SDL2;

/// <summary>
/// Draws engine primitive types using only core-profile OpenGL (required on macOS).
/// Quads are drawn as indexed triangle pairs; QuadStrip and Polygon map to equivalent strip/fan types.
/// </summary>
internal static class GLPrimitives
{
	private static readonly IReadOnlyList<OpenTK.Graphics.OpenGL.PrimitiveType> CoreModes = new OpenTK.Graphics.OpenGL.PrimitiveType[10]
	{
		OpenTK.Graphics.OpenGL.PrimitiveType.Points,
		OpenTK.Graphics.OpenGL.PrimitiveType.Lines,
		OpenTK.Graphics.OpenGL.PrimitiveType.LineStrip,
		OpenTK.Graphics.OpenGL.PrimitiveType.LineLoop,
		OpenTK.Graphics.OpenGL.PrimitiveType.Triangles,
		OpenTK.Graphics.OpenGL.PrimitiveType.TriangleStrip,
		OpenTK.Graphics.OpenGL.PrimitiveType.TriangleFan,
		OpenTK.Graphics.OpenGL.PrimitiveType.Triangles,     // Quads, via the quad index buffer
		OpenTK.Graphics.OpenGL.PrimitiveType.TriangleStrip, // QuadStrip has the same vertex order as a triangle strip
		OpenTK.Graphics.OpenGL.PrimitiveType.TriangleFan    // Polygon (convex)
	};

	private static int _quadIndexBuffer;

	private static int _quadIndexCapacity;

	/// <summary>
	/// Draws vertices from the currently bound vertex array.
	/// </summary>
	public static void Draw(SonicOrca.Graphics.PrimitiveType type, int first, int count)
	{
		if (type != SonicOrca.Graphics.PrimitiveType.Quads)
		{
			GL.DrawArrays(CoreModes[(int)type], first, count);
			return;
		}
		int quads = count / 4;
		if (quads == 0)
		{
			return;
		}
		if (first % 4 != 0)
		{
			// Not aligned to the index pattern; rare, so draw each quad as a fan
			for (int i = 0; i < quads; i++)
			{
				GL.DrawArrays(OpenTK.Graphics.OpenGL.PrimitiveType.TriangleFan, first + i * 4, 4);
			}
			return;
		}
		// Quad n uses vertices 4n..4n+3, so starting at vertex `first` means starting at quad first / 4 in the
		// index buffer. (Avoids DrawElementsBaseVertex, which WebGL2 lacks.) Element buffer bindings are vertex
		// array state, so bind it to the current VAO for every draw.
		int firstQuad = first / 4;
		EnsureQuadIndices(firstQuad + quads);
		GL.DrawElements(OpenTK.Graphics.OpenGL.PrimitiveType.Triangles, quads * 6, DrawElementsType.UnsignedInt, (IntPtr)(firstQuad * 6 * sizeof(uint)));
	}

	private static void EnsureQuadIndices(int quads)
	{
		if (_quadIndexBuffer == 0)
		{
			_quadIndexBuffer = GL.GenBuffer();
		}
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, _quadIndexBuffer);
		if (quads <= _quadIndexCapacity)
		{
			return;
		}
		int capacity = Math.Max(quads, Math.Max(256, _quadIndexCapacity * 2));
		uint[] indices = new uint[capacity * 6];
		for (int i = 0; i < capacity; i++)
		{
			uint v = (uint)(i * 4);
			int n = i * 6;
			indices[n] = v;
			indices[n + 1] = v + 1;
			indices[n + 2] = v + 2;
			indices[n + 3] = v;
			indices[n + 4] = v + 2;
			indices[n + 5] = v + 3;
		}
		GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
		_quadIndexCapacity = capacity;
	}
}
