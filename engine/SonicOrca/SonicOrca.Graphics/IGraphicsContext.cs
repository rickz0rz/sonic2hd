using System.Collections.Generic;

namespace SonicOrca.Graphics;

public interface IGraphicsContext
{
	IReadOnlyCollection<ITexture> Textures { get; }

	IReadOnlyCollection<IShaderProgram> ShaderPrograms { get; }

	IReadOnlyCollection<VertexBuffer> VertexBuffers { get; }

	IReadOnlyCollection<IFramebuffer> RenderTargets { get; }

	bool DepthTesting { get; set; }

	BlendMode BlendMode { get; set; }

	PolygonMode PolygonMode { get; set; }

	IFramebuffer CurrentFramebuffer { get; }

	IShader CreateShader(ShaderType type, string sourceCode);

	IShaderProgram CreateShaderProgram(params IShader[] shaders);

	IShaderProgram CreateShaderProgram(IEnumerable<IShader> shaders);

	VertexBuffer CreateVertexBuffer(params int[] vectorCounts);

	VertexBuffer CreateVertexBuffer(IEnumerable<int> vectorCounts);

	VertexBuffer CreateVertexBuffer(IShaderProgram shaderProgram, IEnumerable<string> names, IEnumerable<int> vectorCounts);

	ITexture CreateTexture(int width, int height);

	ITexture CreateTexture(int width, int height, int channels, byte[] pixels, bool toCompress = false);

	void SetTexture(ITexture texture);

	void SetTexture(int index, ITexture texture);

	void SetTextures(IEnumerable<ITexture> textures);

	IFramebuffer CreateFrameBuffer(int width, int height, int numTextures = 1);

	void RenderToBackBuffer();

	void ClearBuffer();

	// Stencil test (used to mask level shadows)

	void SetStencilTest(bool enabled);

	void ClearStencil();

	void SetStencilFunction(StencilFunction function, int reference, int mask);

	void SetStencilOperation(StencilOperation stencilFail, StencilOperation depthFail, StencilOperation pass);

	void SetStencilWriteMask(int mask);

	void ClearDepthBuffer();

	void ClearColourBuffer(int index);

	IBuffer CreateBuffer();

	IVertexArray CreateVertexArray();
}
