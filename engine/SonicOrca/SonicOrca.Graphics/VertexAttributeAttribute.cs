using System;

namespace SonicOrca.Graphics;

public class VertexAttributeAttribute : Attribute
{
	private readonly string _name;

	public string Name => _name;

	public VertexAttributeAttribute(string name)
	{
		_name = name;
	}
}
