using System;
using System.Collections.Generic;

namespace SonicOrca.Graphics;

/// <summary>
/// A shader program together with a vertex buffer for it.
/// </summary>
public class ShaderInstance : IDisposable
{
	private readonly IGraphicsContext _graphicsContext;

	private readonly ManagedShaderProgram _shaderProgram;

	private readonly VertexBuffer _vertexBuffer;

	public IGraphicsContext GraphicsContext => _graphicsContext;

	public IShaderProgram ShaderProgram => _shaderProgram.Program;

	public VertexBuffer VertexBuffer => _vertexBuffer;

	public ShaderInstance(IGraphicsContext graphicsContext, string vertex, string fragment, IEnumerable<int> vertexCounts)
	{
		_graphicsContext = graphicsContext;
		_shaderProgram = new ManagedShaderProgram(graphicsContext, vertex, fragment);
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(vertexCounts);
	}

	public ShaderInstance(IGraphicsContext graphicsContext, string orcaGLSL, IEnumerable<int> vertexCounts)
	{
		_graphicsContext = graphicsContext;
		_shaderProgram = OrcaShader.Create(graphicsContext, orcaGLSL);
		_vertexBuffer = _graphicsContext.CreateVertexBuffer(vertexCounts);
	}

	public void Dispose()
	{
		_vertexBuffer.Dispose();
		_shaderProgram.Dispose();
	}
}
