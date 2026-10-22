using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SonicOrca;

public static class AttributeHelpers
{
	public static string GetDescription(object value)
	{
		return GetAttribute<DescriptionAttribute>(value)?.Description;
	}

	public static T GetAttribute<T>(object value) where T : Attribute
	{
		Type type = value as Type;
		if (type == null)
		{
			type = value.GetType();
			if (!type.IsEnum)
			{
				throw new ArgumentException("Value must be a Type or Enum value.", "value");
			}
			return type.GetMember(value.ToString()).First().GetCustomAttribute<T>();
		}
		return type.GetCustomAttribute<T>();
	}
}
