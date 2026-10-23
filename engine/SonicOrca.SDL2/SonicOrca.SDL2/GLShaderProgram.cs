using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

/// <summary>A linked OpenGL shader program. Uniforms that the program does not use are ignored.</summary>
internal class GLShaderProgram : IShaderProgram, IDisposable
{
	private readonly GLGraphicsContext _context;

	private readonly int _id;

	private readonly Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();

	public GLShaderProgram(GLGraphicsContext context, IEnumerable<GLShader> shaders)
	{
		_context = context;
		_id = CreateShaderProgram(shaders);
	}

	public virtual void Dispose()
	{
		GL.DeleteProgram(_id);
		_context.ShaderPrograms.Remove(this);
	}

	private int CreateShaderProgram(IEnumerable<GLShader> shaders)
	{
		int programId = GL.CreateProgram();
		try
		{
			foreach (GLShader shader in shaders)
			{
				GL.AttachShader(programId, shader.Id);
			}
			GL.LinkProgram(programId);
			GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out int linked);
			if (linked != 1)
			{
				throw new Exception(GL.GetProgramInfoLog(programId));
			}
			_context.ShaderPrograms.Add(this);
			return programId;
		}
		catch
		{
			GL.DeleteProgram(programId);
			throw;
		}
	}

	public void Activate()
	{
		GL.UseProgram(_id);
	}

	public int GetAttributeLocation(string name)
	{
		return GL.GetAttribLocation(_id, name);
	}

	public int GetUniformLocation(string name)
	{
		if (!_uniformLocations.TryGetValue(name, out int location))
		{
			location = GL.GetUniformLocation(_id, name);
			_uniformLocations[name] = location;
		}
		return location;
	}

	public void SetUniform(string name, int value)
	{
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			GL.Uniform1(location, value);
		}
	}

	public void SetUniform(string name, float value)
	{
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			GL.Uniform1(location, value);
		}
	}

	public void SetUniform(string name, double value)
	{
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			GL.Uniform1(location, (float)value);
		}
	}

	public void SetUniform(string name, Vector2 value)
	{
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			GL.Uniform2(location, (float)value.X, (float)value.Y);
		}
	}

	public void SetUniform(string name, Vector3 value)
	{
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			GL.Uniform3(location, (float)value.X, (float)value.Y, (float)value.Z);
		}
	}

	public void SetUniform(string name, Vector4 value)
	{
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			GL.Uniform4(location, (float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
		}
	}

	public void SetUniform(string name, Matrix4 value)
	{
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			// OpenGL takes the matrix in column-major order
			GL.UniformMatrix4(location, 1, transpose: false, new float[16]
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
		int location = GetUniformLocation(name);
		if (location >= 0)
		{
			GL.Uniform4(location, value.Red / 255f, value.Green / 255f, value.Blue / 255f, value.Alpha / 255f);
		}
	}
}
