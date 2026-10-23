using System;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Sets the classification of an object type, for example ring, character or capsule.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ClassificationAttribute : Attribute
{
	private readonly ObjectClassification _classification;

	public ObjectClassification Classification => _classification;

	public ClassificationAttribute(ObjectClassification classification)
	{
		_classification = classification;
	}

	public static ClassificationAttribute FromObject(object obj)
	{
		return AttributeHelpers.GetAttribute<ClassificationAttribute>(obj.GetType());
	}
}
