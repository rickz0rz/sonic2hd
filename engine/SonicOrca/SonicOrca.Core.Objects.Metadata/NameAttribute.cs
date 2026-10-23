using System;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Gives an object type a display name.
/// </summary>
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
