using System;
using System.Collections.Generic;
using System.Reflection;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Declares a resource that an object type needs.
/// Put it on the class with a resource key, or on a static string field or property that holds the key. An optional
/// resource loads only when the resources have it, for example a resource that only an import from the disassembly
/// makes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
public class DependencyAttribute : Attribute
{
	private readonly string _resourceKey;

	public string ResourceKey => _resourceKey;

	/// <summary>True when the object type works without the resource.</summary>
	public bool Optional { get; set; }

	public DependencyAttribute()
	{
	}

	public DependencyAttribute(string resourceKey)
	{
		_resourceKey = resourceKey;
	}

	/// <summary>Gets the resources that the object needs.</summary>
	public static IEnumerable<string> GetDependencies(object obj)
	{
		foreach ((string key, bool optional) in GetAllDependencies(obj))
		{
			if (!optional)
			{
				yield return key;
			}
		}
	}

	/// <summary>Gets the resources that the object uses when the resources have them.</summary>
	public static IEnumerable<string> GetOptionalDependencies(object obj)
	{
		foreach ((string key, bool optional) in GetAllDependencies(obj))
		{
			if (optional)
			{
				yield return key;
			}
		}
	}

	private static IEnumerable<(string Key, bool Optional)> GetAllDependencies(object obj)
	{
		Type objType = obj.GetType();
		foreach (DependencyAttribute classAttribute in objType.GetCustomAttributes<DependencyAttribute>())
		{
			yield return (classAttribute.ResourceKey, classAttribute.Optional);
		}
		MemberInfo[] members = objType.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MemberInfo memberInfo in members)
		{
			DependencyAttribute memberAttribute = memberInfo.GetCustomAttribute<DependencyAttribute>();
			if (memberAttribute == null)
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
			yield return (resourceKey, memberAttribute.Optional);
		}
	}
}
