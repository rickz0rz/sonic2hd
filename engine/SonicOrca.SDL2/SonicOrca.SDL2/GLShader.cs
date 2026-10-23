using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace SonicOrca.SDL2;

/// <summary>A compiled OpenGL shader. Throws <see cref="SDL2Exception"/> with the compiler log when it does not compile.</summary>
internal class GLShader : SonicOrca.Graphics.IShader, IDisposable
{
	/// <summary>OpenGL shader types, indexed by <see cref="SonicOrca.Graphics.ShaderType"/>.</summary>
	private static readonly IReadOnlyList<ShaderType> GLShaderTypes = new[]
	{
		ShaderType.FragmentShader,
		ShaderType.VertexShader,
		ShaderType.GeometryShader, // GeometryExt
		ShaderType.GeometryShader,
		ShaderType.TessEvaluationShader,
		ShaderType.TessControlShader
	};

	private readonly int _id;

	public int Id => _id;

	public GLShader(SonicOrca.Graphics.ShaderType type, string sourceCode)
	{
		_id = GL.CreateShader(GLShaderTypes[(int)type]);
		try
		{
			GL.ShaderSource(_id, sourceCode);
			GL.CompileShader(_id);
			GL.GetShader(_id, ShaderParameter.CompileStatus, out int compiled);
			if (compiled != 1)
			{
				string log = GL.GetShaderInfoLog(_id);
				throw new SDL2Exception($"Error compiling shader.\n\n{log}", new InvalidDataException(log));
			}
		}
		catch
		{
			GL.DeleteShader(_id);
			throw;
		}
	}

	public void Dispose()
	{
		GL.DeleteShader(_id);
	}
}
