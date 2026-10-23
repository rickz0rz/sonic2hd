using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

/// <summary>
/// A vertex array with one float buffer per attribute. Values are collected between <see cref="Begin"/> and
/// <see cref="End"/>, then uploaded together.
/// </summary>
internal class GLVertexBuffer : VertexBuffer
{
	private readonly GLGraphicsContext _context;

	/// <summary>The number of floats per vertex, for each attribute.</summary>
	private readonly int[] _vectorCounts;

	private readonly int _vertexArrayId;

	private readonly int[] _bufferIds;

	private readonly List<float>[] _data;

	private int _vertexCount;

	/// <summary>Set by <see cref="End"/> when no values were collected. Data set with SetBufferData still renders.</summary>
	private bool _empty;

	public override IReadOnlyList<int> VectorCounts => _vectorCounts;

	public GLVertexBuffer(GLGraphicsContext context, IEnumerable<int> vectorCounts)
	{
		_context = context;
		_vectorCounts = vectorCounts.ToArray();
		(_vertexArrayId, _bufferIds, _data) = CreateBuffers(_vectorCounts);
		_context.VertexBuffers.Add(this);
	}

	/// <summary>Creates the buffers at the attribute locations that the shader program gives for each name.</summary>
	public GLVertexBuffer(GLGraphicsContext context, IShaderProgram shaderProgram, IEnumerable<string> names, IEnumerable<int> vectorCounts)
	{
		_context = context;
		_vectorCounts = SetAttributeLocations(shaderProgram, names, vectorCounts).ToArray();
		(_vertexArrayId, _bufferIds, _data) = CreateBuffers(_vectorCounts);
		_context.VertexBuffers.Add(this);
	}

	private static (int VertexArrayId, int[] BufferIds, List<float>[] Data) CreateBuffers(int[] vectorCounts)
	{
		GL.GenVertexArrays(1, out int vertexArrayId);
		GL.BindVertexArray(vertexArrayId);
		int[] bufferIds = new int[vectorCounts.Length];
		List<float>[] data = new List<float>[vectorCounts.Length];
		GL.GenBuffers(bufferIds.Length, bufferIds);
		for (int i = 0; i < bufferIds.Length; i++)
		{
			GL.EnableVertexAttribArray(i);
			GL.BindBuffer(BufferTarget.ArrayBuffer, bufferIds[i]);
			GL.VertexAttribPointer(i, vectorCounts[i], VertexAttribPointerType.Float, normalized: false, 0, 0);
			data[i] = new List<float>();
		}
		return (vertexArrayId, bufferIds, data);
	}

	public override void Dispose()
	{
		GL.DeleteVertexArrays(1, new[] { _vertexArrayId });
		GL.DeleteBuffers(_bufferIds.Length, _bufferIds);
		_context.VertexBuffers.Remove(this);
	}

	public override void SetBufferData(int index, IEnumerable<double> data)
	{
		SetBufferData(index, data.Select(x => (float)x));
	}

	public void SetBufferData(int index, IEnumerable<float> data)
	{
		float[] values = data.ToArray();
		GL.BindBuffer(BufferTarget.ArrayBuffer, _bufferIds[index]);
		GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(values.Length * sizeof(float)), values, BufferUsageHint.StreamDraw);
	}

	public override void AddValue(int index, double value)
	{
		_data[index].Add((float)value);
	}

	public override void Begin()
	{
		foreach (List<float> values in _data)
		{
			values.Clear();
		}
	}

	/// <summary>Uploads the collected values. The first attribute gives the vertex count.</summary>
	public override void End()
	{
		_vertexCount = _data.Length > 0 ? _data[0].Count / _vectorCounts[0] : 0;
		_empty = _vertexCount == 0;
		if (_empty)
		{
			return;
		}
		for (int i = 0; i < _data.Length; i++)
		{
			SetBufferData(i, _data[i]);
		}
	}

	public override void Render(SonicOrca.Graphics.PrimitiveType type)
	{
		Render(type, 0, _vertexCount);
	}

	public override void Render(SonicOrca.Graphics.PrimitiveType type, int index, int count)
	{
		if (!_empty)
		{
			GL.BindVertexArray(_vertexArrayId);
			GLPrimitives.Draw(type, index, count);
		}
	}
}
