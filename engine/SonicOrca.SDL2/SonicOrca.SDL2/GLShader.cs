using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

internal class GLShader : IShader, IDisposable
{
	private static readonly IReadOnlyList<OpenTK.Graphics.OpenGL.ShaderType> ShaderTypeConversionTable = new OpenTK.Graphics.OpenGL.ShaderType[6]
	{
		OpenTK.Graphics.OpenGL.ShaderType.FragmentShader,
		OpenTK.Graphics.OpenGL.ShaderType.VertexShader,
		OpenTK.Graphics.OpenGL.ShaderType.GeometryShader,
		OpenTK.Graphics.OpenGL.ShaderType.GeometryShader,
		OpenTK.Graphics.OpenGL.ShaderType.TessEvaluationShader,
		OpenTK.Graphics.OpenGL.ShaderType.TessControlShader
	};

	private readonly OpenTK.Graphics.OpenGL.ShaderType _type;

	private readonly string _sourceCode;

	private int _glId;

	public int Id => _glId;

	public GLShader(SonicOrca.Graphics.ShaderType type, string sourceCode)
	{
		_type = ShaderTypeConversionTable[(int)type];
		_sourceCode = sourceCode;
		_glId = GL.CreateShader(_type);
		try
		{
			GL.ShaderSource(_glId, _sourceCode);
			GL.CompileShader(_glId);
			GL.GetShader(_glId, ShaderParameter.CompileStatus, out var @params);
			if (@params != 1)
			{
				string shaderInfoLog = GL.GetShaderInfoLog(_glId);
				throw new SDL2Exception($"Error compiling shader.\n\n{shaderInfoLog}", new InvalidDataException(shaderInfoLog));
			}
		}
		catch
		{
			GL.DeleteShader(_glId);
			throw;
		}
	}

	public void Dispose()
	{
		GL.DeleteShader(_glId);
	}
}
