using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

internal class GLVertexBuffer : VertexBuffer
{
	private readonly GLGraphicsContext _context;

	private readonly int _numBuffers;

	private readonly int[] _vectorCounts;

	private readonly int _glId;

	private readonly int[] _glBufferIds;

	private readonly List<float>[] _data;

	private int _numPoints;

	private bool _noRender;

	public override IReadOnlyList<int> VectorCounts => _vectorCounts;

	public GLVertexBuffer(GLGraphicsContext context, IEnumerable<int> vectorCounts)
	{
		_context = context;
		_vectorCounts = vectorCounts.ToArray();
		_numBuffers = _vectorCounts.Length;
		GL.GenVertexArrays(1, out _glId);
		GL.BindVertexArray(_glId);
		_glBufferIds = new int[_numBuffers];
		_data = new List<float>[_numBuffers];
		GL.GenBuffers(_numBuffers, _glBufferIds);
		for (int i = 0; i < _numBuffers; i++)
		{
			GL.EnableVertexAttribArray(i);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _glBufferIds[i]);
			GL.VertexAttribPointer(i, _vectorCounts[i], VertexAttribPointerType.Float, normalized: false, 0, 0);
			_data[i] = new List<float>();
		}
		_context.VertexBuffers.Add(this);
	}

	public GLVertexBuffer(GLGraphicsContext context, IShaderProgram shaderProgram, IEnumerable<string> names, IEnumerable<int> vectorCounts)
	{
		_context = context;
		_vectorCounts = SetAttributeLocations(shaderProgram, names, vectorCounts).ToArray();
		_numBuffers = _vectorCounts.Length;
		GL.GenVertexArrays(1, out _glId);
		GL.BindVertexArray(_glId);
		_glBufferIds = new int[_numBuffers];
		_data = new List<float>[_numBuffers];
		GL.GenBuffers(_numBuffers, _glBufferIds);
		for (int i = 0; i < _numBuffers; i++)
		{
			GL.EnableVertexAttribArray(i);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _glBufferIds[i]);
			GL.VertexAttribPointer(i, _vectorCounts[i], VertexAttribPointerType.Float, normalized: false, 0, 0);
			_data[i] = new List<float>();
		}
		_context.VertexBuffers.Add(this);
	}

	public override void Dispose()
	{
		GL.DeleteVertexArrays(1, new int[1] { _glId });
		GL.DeleteBuffers(_numBuffers, _glBufferIds);
		_context.VertexBuffers.Remove(this);
	}

	public override void SetBufferData(int index, IEnumerable<double> data)
	{
		SetBufferData(index, data.Select((double x) => (float)x));
	}

	public void SetBufferData(int index, IEnumerable<float> data)
	{
		GL.BindBuffer(BufferTarget.ArrayBuffer, _glBufferIds[index]);
		GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(data.Count() * 4), data.Select((float x) => x).ToArray(), BufferUsageHint.StreamDraw);
	}

	public override void AddValue(int index, double value)
	{
		_data[index].Add((float)value);
	}

	public override void Begin()
	{
		for (int i = 0; i < _numBuffers; i++)
		{
			_data[i].Clear();
		}
	}

	public override void End()
	{
		_numPoints = 0;
		for (int i = 0; i < _numBuffers; i++)
		{
			int numPoints = _data[i].Count / _vectorCounts[i];
			if (i == 0)
			{
				_numPoints = numPoints;
			}
			else
			{
				_ = _numPoints;
			}
		}
		if (_numPoints == 0)
		{
			_noRender = true;
			return;
		}
		for (int j = 0; j < _numBuffers; j++)
		{
			SetBufferData(j, _data[j]);
		}
		_noRender = false;
	}

	public override void Render(SonicOrca.Graphics.PrimitiveType type)
	{
		if (!_noRender)
		{
			GL.BindVertexArray(_glId);
			GLPrimitives.Draw(type, 0, _numPoints);
		}
	}

	public override void Render(SonicOrca.Graphics.PrimitiveType type, int index, int count)
	{
		if (!_noRender)
		{
			GL.BindVertexArray(_glId);
			GLPrimitives.Draw(type, index, count);
		}
	}
}
