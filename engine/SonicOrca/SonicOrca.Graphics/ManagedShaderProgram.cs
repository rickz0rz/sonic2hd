using System;

namespace SonicOrca.Graphics;

public class ManagedShaderProgram : IDisposable
{
	private readonly IShader _vertexShader;

	private readonly IShader _fragmentShader;

	private readonly IShaderProgram _shaderProgram;

	public IShader Vertex => _vertexShader;

	public IShader Fragment => _fragmentShader;

	public IShaderProgram Program => _shaderProgram;

	public ManagedShaderProgram(IGraphicsContext context, string vertexSourceCode, string fragmentSourceCode)
	{
		_vertexShader = context.CreateShader(ShaderType.Vertex, vertexSourceCode);
		_fragmentShader = context.CreateShader(ShaderType.Fragment, fragmentSourceCode);
		_shaderProgram = context.CreateShaderProgram(_vertexShader, _fragmentShader);
	}

	public void Dispose()
	{
		_shaderProgram.Dispose();
		_vertexShader.Dispose();
		_fragmentShader.Dispose();
	}
}
