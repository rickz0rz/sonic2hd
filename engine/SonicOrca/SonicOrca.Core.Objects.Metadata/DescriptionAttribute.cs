using System;

namespace SonicOrca.Core.Objects.Metadata;

[AttributeUsage(AttributeTargets.Class)]
public class DescriptionAttribute : Attribute
{
	private readonly string _description;

	public string Description => _description;

	public DescriptionAttribute(string description)
	{
		_description = description;
	}

	public static DescriptionAttribute FromObject(object obj)
	{
		return AttributeHelpers.GetAttribute<DescriptionAttribute>(obj.GetType());
	}
}
