using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

/// <summary>An OpenGL array buffer for vertex data.</summary>
internal class GLBuffer : IBuffer, IDisposable
{
	private readonly int _id;

	public GLBuffer()
	{
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

	/// <summary>Uploads <paramref name="length"/> elements of <paramref name="data"/>, from <paramref name="offset"/>.</summary>
	public void SetData<T>(T[] data, int offset, int length)
	{
		IntPtr start = Marshal.UnsafeAddrOfPinnedArrayElement(data, offset);
		IntPtr size = new IntPtr(Marshal.SizeOf<T>() * length);
		Bind();
		GL.BufferData(BufferTarget.ArrayBuffer, size, start, BufferUsageHint.StreamDraw);
	}
}
