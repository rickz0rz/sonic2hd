using System;
using System.Reflection;
using SonicOrca.Resources;

namespace SonicOrca.Extensions;

public static class ResourceExtensions
{
	public static string GetAbsolutePath(this ILoadedResource parentLoadedResource, string keyPath)
	{
		return parentLoadedResource.Resource.GetAbsolutePath(keyPath);
	}

	public static void FullfillLoadedResourcesByAttribute(this ResourceTree tree, object instance)
	{
		MemberInfo[] members = instance.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MemberInfo memberInfo in members)
		{
			switch (memberInfo.MemberType)
			{
			case MemberTypes.Field:
			{
				ResourcePathAttribute customAttribute = memberInfo.GetCustomAttribute<ResourcePathAttribute>();
				if (customAttribute != null)
				{
					FieldInfo fieldInfo = (FieldInfo)memberInfo;
					string path2 = customAttribute.Path;
					ILoadedResource loadedResource2 = tree.GetLoadedResource(path2);
					if (!fieldInfo.FieldType.IsAssignableFrom(loadedResource2.GetType()))
					{
						throw new ResourceException(path2 + " doesn't have the correct resource type.");
					}
					fieldInfo.SetValue(instance, loadedResource2);
				}
				break;
			}
			case MemberTypes.Property:
			{
				ResourcePathAttribute customAttribute = memberInfo.GetCustomAttribute<ResourcePathAttribute>();
				if (customAttribute != null)
				{
					PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
					string path = customAttribute.Path;
					ILoadedResource loadedResource = tree.GetLoadedResource(path);
					Type type = loadedResource.GetType();
					if (!propertyInfo.PropertyType.IsAssignableFrom(type))
					{
						throw new ResourceException(path + " doesn't have the correct resource type.");
					}
					propertyInfo.SetValue(instance, loadedResource);
				}
				break;
			}
			}
		}
	}

	public static void PushDependenciesByAttribute(this ResourceSession session, object instance)
	{
		MemberInfo[] members = instance.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		for (int i = 0; i < members.Length; i++)
		{
			ResourcePathAttribute customAttribute = members[i].GetCustomAttribute<ResourcePathAttribute>();
			if (customAttribute != null)
			{
				session.PushDependency(customAttribute.Path);
			}
		}
	}
}
