using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SonicOrca.Core.Extensions;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Marks a property or field of an active object as a state variable.
/// The editor shows state variables, and the engine copies them from one instance to another.
/// Public members that the class declares are state variables also when they do not have this attribute.
/// </summary>
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
		// A new instance supplies the default value of each state variable.
		object instance = Activator.CreateInstance(type);

		// 1. Public members that the type declares, unless they have HideInEditor.
		IEnumerable<MemberInfo> publicDeclaredMembers = type.GetUnderlyingMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
		foreach (MemberInfo member in publicDeclaredMembers)
		{
			if (member.GetCustomAttribute<HideInEditorAttribute>() == null)
			{
				StateVariableAttribute stateVariableAttribute = new StateVariableAttribute();
				stateVariableAttribute.Type = member.GetUnderlyingType();
				stateVariableAttribute.Name = member.Name;
				stateVariableAttribute.DefaultValue = member.GetUnderlyingValue(instance);
				yield return new Tuple<MemberInfo, StateVariableAttribute>(member, stateVariableAttribute);
			}
		}

		// 2. Inherited public members that have the attribute.
		IEnumerable<MemberInfo> publicInheritedMembers = type.GetUnderlyingMembers(BindingFlags.Instance | BindingFlags.Public).Except(publicDeclaredMembers);
		foreach (MemberInfo member in publicInheritedMembers)
		{
			StateVariableAttribute customAttribute = member.GetCustomAttribute<StateVariableAttribute>();
			if (customAttribute != null)
			{
				customAttribute.Type = member.GetUnderlyingType();
				customAttribute.Name = member.Name;
				customAttribute.DefaultValue = member.GetUnderlyingValue(instance);
				yield return new Tuple<MemberInfo, StateVariableAttribute>(member, customAttribute);
			}
		}

		// 3. Non-public members that have the attribute.
		IEnumerable<MemberInfo> nonPublicMembers = type.GetUnderlyingMembers(BindingFlags.Instance | BindingFlags.NonPublic);
		foreach (MemberInfo member in nonPublicMembers)
		{
			StateVariableAttribute customAttribute = member.GetCustomAttribute<StateVariableAttribute>();
			if (customAttribute != null)
			{
				customAttribute.Type = member.GetUnderlyingType();
				customAttribute.Name = member.Name;
				customAttribute.DefaultValue = member.GetUnderlyingValue(instance);
				yield return new Tuple<MemberInfo, StateVariableAttribute>(member, customAttribute);
			}
		}
	}

	public static void SetObjectState(IActiveObject instance, IActiveObject state)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		foreach (Tuple<MemberInfo, StateVariableAttribute> stateVariable in GetStateVariables(instance.GetType()))
		{
			MemberInfo instanceMember = stateVariable.Item1;
			MemberInfo stateMember = state.GetType().GetMember(instanceMember.Name, bindingAttr).FirstOrDefault();
			if (stateMember != null)
			{
				object value = stateMember.GetUnderlyingValue(state);
				instanceMember.SetUnderlyingValue(instance, value);
			}
		}
	}

	public static ObjectEditorProperty[] GetEditorProperties(ObjectType objType)
	{
		ObjectInstanceAttribute objectInstanceAttribute = ObjectInstanceAttribute.FromObject(objType);
		if (objectInstanceAttribute == null)
		{
			return new ObjectEditorProperty[0];
		}
		Type objectInstanceType = objectInstanceAttribute.ObjectInstanceType;
		List<ObjectEditorProperty> properties = new List<ObjectEditorProperty>();
		foreach (Tuple<MemberInfo, StateVariableAttribute> stateVariable in GetStateVariables(objectInstanceType))
		{
			StateVariableAttribute attribute = stateVariable.Item2;
			properties.Add(new ObjectEditorProperty(attribute.Name, attribute.Name, attribute.Type, attribute.DefaultValue));
		}
		return properties.ToArray();
	}
}
