using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// The saved data of one object in a level: its type key, unique ID, name, layer, position and behaviour values.
/// </summary>
public class ObjectDefinition
{
	private string _key;

	private Guid _uid;

	private string _name;

	private int _layer;

	private Vector2i _position;

	private readonly IReadOnlyCollection<KeyValuePair<string, object>> _behaviour;

	public string Key
	{
		get
		{
			return _key;
		}
		set
		{
			_key = value;
		}
	}

	public Guid Uid
	{
		get
		{
			return _uid;
		}
		set
		{
			_uid = value;
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public int Layer
	{
		get
		{
			return _layer;
		}
		set
		{
			_layer = value;
		}
	}

	public Vector2i Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = value;
		}
	}

	public IReadOnlyCollection<KeyValuePair<string, object>> Behaviour => _behaviour;

	public ObjectDefinition(string key, Guid uid, string name, int layer, Vector2i position, IEnumerable<KeyValuePair<string, object>> behaviour)
	{
		_key = key;
		_uid = uid;
		_name = name;
		_layer = layer;
		_position = position;
		_behaviour = behaviour.ToArray();
	}
}
