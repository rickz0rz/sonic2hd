using System;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Identifies the active object class that an object type creates for each instance.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ObjectInstanceAttribute : Attribute
{
	private readonly Type _objectInstanceType;

	public Type ObjectInstanceType => _objectInstanceType;

	public ObjectInstanceAttribute(Type objectInstanceType)
	{
		_objectInstanceType = objectInstanceType;
	}

	public static ObjectInstanceAttribute FromObject(object obj)
	{
		return AttributeHelpers.GetAttribute<ObjectInstanceAttribute>(obj.GetType());
	}
}
