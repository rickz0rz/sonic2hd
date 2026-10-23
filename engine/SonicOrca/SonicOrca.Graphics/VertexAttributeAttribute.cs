using System;

namespace SonicOrca.Graphics;

/// <summary>
/// Gives the shader attribute name for a field of a vertex structure.
/// </summary>
public class VertexAttributeAttribute : Attribute
{
	private readonly string _name;

	public string Name => _name;

	public VertexAttributeAttribute(string name)
	{
		_name = name;
	}
}
