using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SonicOrca;

/// <summary>
/// Reads attributes from types and from enumeration values.
/// </summary>
public static class AttributeHelpers
{
	public static string GetDescription(object value)
	{
		return GetAttribute<DescriptionAttribute>(value)?.Description;
	}

	/// <summary>
	/// Gets the attribute of a <see cref="Type"/>, or of the field that defines an enumeration value.
	/// </summary>
	public static T GetAttribute<T>(object value) where T : Attribute
	{
		if (value is Type type)
		{
			return type.GetCustomAttribute<T>();
		}
		Type valueType = value.GetType();
		if (!valueType.IsEnum)
		{
			throw new ArgumentException("Value must be a Type or Enum value.", "value");
		}
		return valueType.GetMember(value.ToString()).First().GetCustomAttribute<T>();
	}
}
