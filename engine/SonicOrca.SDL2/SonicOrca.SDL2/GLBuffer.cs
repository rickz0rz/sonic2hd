using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

internal class GLBuffer : IBuffer, IDisposable
{
	private readonly GLGraphicsContext _context;

	private readonly int _id;

	public GLBuffer(GLGraphicsContext context)
	{
		_context = context;
		_id = GL.GenBuffer();
	}

	public void Dispose()
	{
		GL.DeleteBuffer(_id);
	}

	public void Bind()
	{
		GL.BindBuffer(BufferTarget.ArrayBuffer, _id);
	}

	public void SetData<T>(T[] data, int offset, int length)
	{
		IntPtr data2 = Marshal.UnsafeAddrOfPinnedArrayElement((Array)data, offset);
		IntPtr size = new IntPtr(Marshal.SizeOf(typeof(T)) * length);
		Bind();
		GL.BufferData(BufferTarget.ArrayBuffer, size, data2, BufferUsageHint.StreamDraw);
	}
}
