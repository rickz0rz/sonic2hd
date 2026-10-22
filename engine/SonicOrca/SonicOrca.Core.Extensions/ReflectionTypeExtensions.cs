using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SonicOrca.Core.Extensions;

public static class ReflectionTypeExtensions
{
	public static Type GetUnderlyingType(this MemberInfo member)
	{
		return member.MemberType switch
		{
			MemberTypes.Event => ((EventInfo)member).EventHandlerType, 
			MemberTypes.Field => ((FieldInfo)member).FieldType, 
			MemberTypes.Method => ((MethodInfo)member).ReturnType, 
			MemberTypes.Property => ((PropertyInfo)member).PropertyType, 
			_ => throw new ArgumentException("Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"), 
		};
	}

	public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
	{
		Type[] interfaces = givenType.GetInterfaces();
		foreach (Type type in interfaces)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
			{
				return true;
			}
		}
		if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
		{
			return true;
		}
		Type baseType = givenType.BaseType;
		if (baseType == null)
		{
			return false;
		}
		return baseType.IsAssignableToGenericType(genericType);
	}

	public static object GetUnderlyingValue(this MemberInfo member, object instance)
	{
		return member.MemberType switch
		{
			MemberTypes.Field => ((FieldInfo)member).GetValue(instance), 
			MemberTypes.Property => ((PropertyInfo)member).GetValue(instance), 
			_ => throw new ArgumentException("Input MemberInfo must be type FieldInfo, or PropertyInfo"), 
		};
	}

	public static void SetUnderlyingValue(this MemberInfo member, object instance, object value)
	{
		switch (member.MemberType)
		{
		case MemberTypes.Field:
			((FieldInfo)member).SetValue(instance, value);
			break;
		case MemberTypes.Property:
			((PropertyInfo)member).SetValue(instance, value);
			break;
		default:
			throw new ArgumentException("Input MemberInfo must be type FieldInfo, or PropertyInfo");
		}
	}

	public static IEnumerable<MemberInfo> GetUnderlyingMembers(this Type type, BindingFlags bindingFlags)
	{
		IEnumerable<MemberInfo> first = type.GetFields(bindingFlags).Cast<MemberInfo>();
		IEnumerable<MemberInfo> second = type.GetProperties(bindingFlags).Cast<MemberInfo>();
		return first.Concat(second);
	}

	public static bool IsAnonymous(this Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: false) && type.Name.Contains("AnonymousType") && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")))
		{
			return (type.Attributes & TypeAttributes.NotPublic) == 0;
		}
		return false;
	}
}
