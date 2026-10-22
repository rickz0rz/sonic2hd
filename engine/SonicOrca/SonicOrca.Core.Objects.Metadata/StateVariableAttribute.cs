using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SonicOrca.Core.Extensions;

namespace SonicOrca.Core.Objects.Metadata;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class StateVariableAttribute : Attribute
{
	private object _defaultValue;

	private Type _type;

	private string _name = "";

	public string Name
	{
		get
		{
			if (_type == null)
			{
				throw new TypeAccessException();
			}
			return _name;
		}
		private set
		{
			if (_type == null)
			{
				throw new TypeAccessException();
			}
			_name = value;
		}
	}

	public object DefaultValue
	{
		get
		{
			if (_type == null)
			{
				throw new TypeAccessException();
			}
			return _defaultValue;
		}
		private set
		{
			if (_type == null)
			{
				throw new TypeAccessException();
			}
			_defaultValue = value;
		}
	}

	public Type Type
	{
		get
		{
			if (_type == null)
			{
				throw new TypeAccessException();
			}
			return _type;
		}
		private set
		{
			_type = value;
		}
	}

	public static IEnumerable<Tuple<MemberInfo, StateVariableAttribute>> GetStateVariables(Type type)
	{
		object instance = Activator.CreateInstance(type);
		IEnumerable<MemberInfo> publicDeclaredMembers = type.GetUnderlyingMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
		foreach (MemberInfo item in publicDeclaredMembers)
		{
			if (item.GetCustomAttribute<HideInEditorAttribute>() == null)
			{
				StateVariableAttribute stateVariableAttribute = new StateVariableAttribute();
				stateVariableAttribute.Type = item.GetUnderlyingType();
				stateVariableAttribute.Name = item.Name;
				stateVariableAttribute.DefaultValue = item.GetUnderlyingValue(instance);
				yield return new Tuple<MemberInfo, StateVariableAttribute>(item, stateVariableAttribute);
			}
		}
		IEnumerable<MemberInfo> enumerable = type.GetUnderlyingMembers(BindingFlags.Instance | BindingFlags.Public).Except(publicDeclaredMembers);
		foreach (MemberInfo item2 in enumerable)
		{
			StateVariableAttribute customAttribute = item2.GetCustomAttribute<StateVariableAttribute>();
			if (customAttribute != null)
			{
				customAttribute.Type = item2.GetUnderlyingType();
				customAttribute.Name = item2.Name;
				customAttribute.DefaultValue = item2.GetUnderlyingValue(instance);
				yield return new Tuple<MemberInfo, StateVariableAttribute>(item2, customAttribute);
			}
		}
		IEnumerable<MemberInfo> underlyingMembers = type.GetUnderlyingMembers(BindingFlags.Instance | BindingFlags.NonPublic);
		foreach (MemberInfo item3 in underlyingMembers)
		{
			StateVariableAttribute customAttribute2 = item3.GetCustomAttribute<StateVariableAttribute>();
			if (customAttribute2 != null)
			{
				customAttribute2.Type = item3.GetUnderlyingType();
				customAttribute2.Name = item3.Name;
				customAttribute2.DefaultValue = item3.GetUnderlyingValue(instance);
				yield return new Tuple<MemberInfo, StateVariableAttribute>(item3, customAttribute2);
			}
		}
	}

	public static void SetObjectState(IActiveObject instance, IActiveObject state)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		foreach (Tuple<MemberInfo, StateVariableAttribute> stateVariable in GetStateVariables(instance.GetType()))
		{
			MemberInfo item = stateVariable.Item1;
			MemberInfo memberInfo = state.GetType().GetMember(item.Name, bindingAttr).FirstOrDefault();
			if (memberInfo != null)
			{
				object underlyingValue = memberInfo.GetUnderlyingValue(state);
				item.SetUnderlyingValue(instance, underlyingValue);
			}
		}
	}

	private static bool EnumTryParse(Type enumType, string name, out object enumValue, bool ignoreCase = false)
	{
		enumValue = null;
		try
		{
			enumValue = Enum.Parse(enumType, name, ignoreCase);
			return true;
		}
		catch
		{
		}
		return false;
	}

	public static ObjectEditorProperty[] GetEditorProperties(ObjectType objType)
	{
		ObjectInstanceAttribute objectInstanceAttribute = ObjectInstanceAttribute.FromObject(objType);
		if (objectInstanceAttribute == null)
		{
			return new ObjectEditorProperty[0];
		}
		Type objectInstanceType = objectInstanceAttribute.ObjectInstanceType;
		List<ObjectEditorProperty> list = new List<ObjectEditorProperty>();
		foreach (Tuple<MemberInfo, StateVariableAttribute> stateVariable in GetStateVariables(objectInstanceType))
		{
			_ = stateVariable.Item1;
			StateVariableAttribute item = stateVariable.Item2;
			list.Add(new ObjectEditorProperty(item.Name, item.Name, item.Type, item.DefaultValue));
		}
		return list.ToArray();
	}
}
