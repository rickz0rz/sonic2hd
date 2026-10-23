using System;
using System.Collections.Generic;
using System.Reflection;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Declares a resource that an object type needs.
/// Put it on the class with a resource key, or on a static string field or property that holds the key.
/// </summary>
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
		foreach (DependencyAttribute classAttribute in objType.GetCustomAttributes<DependencyAttribute>())
		{
			yield return classAttribute.ResourceKey;
		}
		MemberInfo[] members = objType.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MemberInfo memberInfo in members)
		{
			if (memberInfo.GetCustomAttribute<DependencyAttribute>() == null)
			{
				continue;
			}
			// A marked member that is not a readable string gives a null key.
			string resourceKey = null;
			if (memberInfo.MemberType == MemberTypes.Field)
			{
				FieldInfo fieldInfo = (FieldInfo)memberInfo;
				if (fieldInfo.FieldType == typeof(string))
				{
					resourceKey = (string)fieldInfo.GetValue(obj);
				}
			}
			else if (memberInfo.MemberType == MemberTypes.Property)
			{
				PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
				if (propertyInfo.PropertyType == typeof(string) && propertyInfo.CanRead)
				{
					resourceKey = (string)propertyInfo.GetValue(obj);
				}
			}
			yield return resourceKey;
		}
	}
}
