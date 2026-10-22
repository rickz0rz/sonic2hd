using System;

namespace SonicOrca.Graphics;

public interface IVertexArray : IDisposable
{
	void Bind();

	void DefineAttribute(int attributeLocation, VertexAttributePointerType type, int size, int stride, int offset);

	void Render(PrimitiveType type, int index, int count);
}
