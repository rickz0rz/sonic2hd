using System;

namespace SonicOrca.Graphics;

/// <summary>
/// Gives the GL component type and the component count of a vertex attribute type.
/// </summary>
public class VertexAttributeTypeAttribute : Attribute
{
	private readonly VertexAttributePointerType _type;

	private readonly int _size;

	public VertexAttributePointerType Type => _type;

	public int Size => _size;

	public VertexAttributeTypeAttribute(VertexAttributePointerType type, int size)
	{
		_type = type;
		_size = size;
	}
}
