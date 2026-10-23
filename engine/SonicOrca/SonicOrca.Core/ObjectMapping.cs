using System;

namespace SonicOrca.Core;

/// <summary>
/// Links a field of an object to the unique ID of a target object.
/// </summary>
public class ObjectMapping
{
	private readonly string _field;

	private Guid _target;

	public string Field => _field;

	public Guid Target
	{
		get
		{
			return _target;
		}
		set
		{
			_target = value;
		}
	}

	public ObjectMapping(string field, Guid target)
	{
		_field = field;
		_target = target;
	}
}
