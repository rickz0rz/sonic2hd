using System;

namespace SonicOrca.Core;

public class ObjectEditorProperty
{
	private readonly string _name;

	private readonly string _key;

	private readonly Type _type;

	private readonly object _defaultValue;

	private readonly string _description;

	public string Name => _name;

	public string Key => _key;

	public Type Type => _type;

	public object DefaultValue => _defaultValue;

	public string Description => _description;

	public ObjectEditorProperty(string name, string key, Type type, object defaultValue, string description = null)
	{
		_name = name;
		_key = key;
		_type = type;
		_defaultValue = defaultValue;
		_description = description;
	}

	public virtual bool Validate(ref object value)
	{
		return true;
	}

	public override string ToString()
	{
		return $"{_name} [{_key} : {_type}]";
	}
}
