using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

internal class GLShaderProgram : IShaderProgram, IDisposable
{
	private readonly GLGraphicsContext _context;

	private readonly int _glId;

	private readonly Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();

	public GLShaderProgram(GLGraphicsContext context, IEnumerable<GLShader> shaders)
	{
		_context = context;
		_glId = CreateShaderProgram(shaders);
	}

	public virtual void Dispose()
	{
		GL.DeleteProgram(_glId);
		_context.ShaderPrograms.Remove(this);
	}

	private int CreateShaderProgram(IEnumerable<GLShader> shaders)
	{
		int num = GL.CreateProgram();
		try
		{
			foreach (GLShader shader in shaders)
			{
				GL.AttachShader(num, shader.Id);
			}
			GL.LinkProgram(num);
			GL.GetProgram(num, GetProgramParameterName.LinkStatus, out var @params);
			if (@params != 1)
			{
				throw new Exception(GL.GetProgramInfoLog(num));
			}
			_context.ShaderPrograms.Add(this);
			return num;
		}
		catch
		{
			GL.DeleteProgram(num);
			throw;
		}
	}

	public void Activate()
	{
		GL.UseProgram(_glId);
	}

	public int GetAttributeLocation(string name)
	{
		return GL.GetAttribLocation(_glId, name);
	}

	public int GetUniformLocation(string name)
	{
		if (!_uniformLocations.TryGetValue(name, out var value))
		{
			return _uniformLocations[name] = GL.GetUniformLocation(_glId, name);
		}
		return value;
	}

	public void SetUniform(string name, int value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.Uniform1(uniformLocation, value);
		}
	}

	public void SetUniform(string name, float value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.Uniform1(uniformLocation, value);
		}
	}

	public void SetUniform(string name, double value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.Uniform1(uniformLocation, (float)value);
		}
	}

	public void SetUniform(string name, Vector2 value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.Uniform2(uniformLocation, (float)value.X, (float)value.Y);
		}
	}

	public void SetUniform(string name, Vector3 value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.Uniform3(uniformLocation, (float)value.X, (float)value.Y, (float)value.Z);
		}
	}

	public void SetUniform(string name, Vector4 value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.Uniform4(uniformLocation, (float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
		}
	}

	public void SetUniform(string name, Matrix4 value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.UniformMatrix4(uniformLocation, 1, transpose: false, new float[16]
			{
				(float)value.M11,
				(float)value.M21,
				(float)value.M31,
				(float)value.M41,
				(float)value.M12,
				(float)value.M22,
				(float)value.M32,
				(float)value.M42,
				(float)value.M13,
				(float)value.M23,
				(float)value.M33,
				(float)value.M43,
				(float)value.M14,
				(float)value.M24,
				(float)value.M34,
				(float)value.M44
			});
		}
	}

	public void SetUniform(string name, Colour value)
	{
		int uniformLocation = GetUniformLocation(name);
		if (uniformLocation >= 0)
		{
			GL.Uniform4(uniformLocation, value.Red / 255f, value.Green / 255f, value.Blue / 255f, value.Alpha / 255f);
		}
	}
}
