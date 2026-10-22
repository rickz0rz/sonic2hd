using System;
using System.Collections.Generic;
using System.Reflection;

namespace SonicOrca.Core.Objects.Metadata;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
public class DependencyAttribute : Attribute
{
	private readonly string _resourceKey;

	public string ResourceKey => _resourceKey;

	public DependencyAttribute()
	{
	}

	public DependencyAttribute(string resourceKey)
	{
		_resourceKey = resourceKey;
	}

	public static IEnumerable<string> GetDependencies(object obj)
	{
		Type objType = obj.GetType();
		foreach (DependencyAttribute customAttribute in objType.GetCustomAttributes<DependencyAttribute>())
		{
			yield return customAttribute.ResourceKey;
		}
		MemberInfo[] members = objType.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		MemberInfo[] array = members;
		foreach (MemberInfo memberInfo in array)
		{
			if (memberInfo.GetCustomAttribute<DependencyAttribute>() == null)
			{
				continue;
			}
			string text = null;
			if (memberInfo.MemberType == MemberTypes.Field)
			{
				FieldInfo fieldInfo = (FieldInfo)memberInfo;
				if (fieldInfo.FieldType == typeof(string))
				{
					text = (string)fieldInfo.GetValue(obj);
				}
			}
			else if (memberInfo.MemberType == MemberTypes.Property)
			{
				PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
				if (propertyInfo.PropertyType == typeof(string) && propertyInfo.CanRead)
				{
					text = (string)propertyInfo.GetValue(obj);
				}
			}
			yield return text;
		}
	}
}
