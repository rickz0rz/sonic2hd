using System;

namespace SonicOrca.Core.Objects.Metadata;

[AttributeUsage(AttributeTargets.Class)]
public class NameAttribute : Attribute
{
	private readonly string _name;

	public string Name => _name;

	public NameAttribute(string name)
	{
		_name = name;
	}

	public static NameAttribute FromObject(object obj)
	{
		return AttributeHelpers.GetAttribute<NameAttribute>(obj.GetType());
	}
}
