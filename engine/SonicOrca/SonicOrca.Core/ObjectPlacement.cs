using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using SonicOrca.Core.Extensions;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

public class ObjectPlacement
{
	private string _key;

	private Guid _uid;

	private string _name;

	private int _layer;

	private Vector2i _position;

	private IReadOnlyCollection<KeyValuePair<string, object>> _entry = new List<KeyValuePair<string, object>>();

	private IReadOnlyCollection<KeyValuePair<string, object>> _behaviour = new List<KeyValuePair<string, object>>();

	private IReadOnlyCollection<KeyValuePair<string, object>> _mappings = new List<KeyValuePair<string, object>>();

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

	public IReadOnlyCollection<KeyValuePair<string, object>> Entry => _entry;

	public IReadOnlyCollection<KeyValuePair<string, object>> Behaviour => _behaviour;

	public IReadOnlyCollection<KeyValuePair<string, object>> Mappings
	{
		get
		{
			return _mappings;
		}
		set
		{
			_mappings = value;
		}
	}

	public ObjectPlacement(string key, int layer, Vector2i position)
	{
		var anon = new
		{
			Key = key,
			Uid = Guid.NewGuid(),
			Name = "",
			Layer = layer,
			Position = new { position.X, position.Y }
		};
		var state = new { };
		_key = key;
		_uid = anon.Uid;
		_name = "";
		_layer = layer;
		_position = position;
		ParseProperties(anon, state);
	}

	public ObjectPlacement(string key, int layer, Vector2i position, object state)
	{
		var anon = new
		{
			Key = key,
			Uid = Guid.NewGuid(),
			Name = "",
			Layer = layer,
			Position = new { position.X, position.Y }
		};
		_key = key;
		_uid = anon.Uid;
		_name = "";
		_layer = layer;
		_position = position;
		ParseProperties(anon, state);
	}

	public ObjectPlacement(string key, Guid uid, string name, int layer, Vector2i position, object state)
	{
		var entry = new
		{
			Key = key,
			Uid = uid,
			Name = name,
			Layer = layer,
			Position = new { position.X, position.Y }
		};
		_key = key;
		_uid = uid;
		_name = name;
		_layer = layer;
		_position = position;
		ParseProperties(entry, state);
	}

	private void ParseProperties(object entry, object state)
	{
		_entry = BehaviourToKeyPairs(entry).ToArray();
		if (state == null)
		{
			throw new Exception();
		}
		if (state.GetType().IsAnonymous() || state.GetType() == typeof(ExpandoObject))
		{
			_behaviour = BehaviourToKeyPairs(state).ToArray();
		}
		else
		{
			_behaviour = StateToKeyPairs(state).ToArray();
		}
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendFormat("Key = {0} X = {1} Y = {2}", _key, _position.X, _position.Y);
		if (_behaviour.Count > 0)
		{
			stringBuilder.Append(" Entry = ");
			WriteBehaviourString(stringBuilder, _entry);
			stringBuilder.Append(" Behaviour = ");
			WriteBehaviourString(stringBuilder, _behaviour);
		}
		return stringBuilder.ToString();
	}

	private static void WriteBehaviourString(StringBuilder sb, IEnumerable<KeyValuePair<string, object>> behaviourObject)
	{
		sb.Append("{ ");
		foreach (KeyValuePair<string, object> item in behaviourObject)
		{
			sb.Append(item.Key);
			sb.Append(" = ");
			if (item.Value is IEnumerable<KeyValuePair<string, object>>)
			{
				WriteBehaviourString(sb, behaviourObject);
			}
			else
			{
				sb.Append(item.Value);
			}
			sb.Append(" ");
		}
		sb.Append("}");
	}

	private static IEnumerable<KeyValuePair<string, object>> StateToKeyPairs(object obj)
	{
		IEnumerable<Tuple<MemberInfo, StateVariableAttribute>> stateVariables = StateVariableAttribute.GetStateVariables(obj.GetType());
		object defaultState = Activator.CreateInstance(obj.GetType()) as ActiveObject;
		Type defaultType = defaultState.GetType();
		BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		foreach (Tuple<MemberInfo, StateVariableAttribute> item2 in stateVariables)
		{
			MemberInfo item = item2.Item1;
			Type underlyingType = item.GetUnderlyingType();
			string name = item.Name;
			object value = ParseStateValue(item.GetUnderlyingValue(obj), underlyingType);
			MemberInfo member = defaultType.GetMember(item.Name, bindingFlags).First();
			member.GetUnderlyingType();
			object underlyingValue = item.GetUnderlyingValue(obj);
			object underlyingValue2 = member.GetUnderlyingValue(defaultState);
			if (!StateValueEquals(underlyingValue, underlyingValue2, underlyingType))
			{
				yield return new KeyValuePair<string, object>(name, value);
			}
		}
	}

	private static object ParseStateValue(object value, Type type)
	{
		if (value == null)
		{
			return value;
		}
		if (type.IsSubclassOf(typeof(ActiveObject)) || type == typeof(ActiveObject))
		{
			return new KeyValuePair<string, object>("Target", (value as ActiveObject).Uid.ToString());
		}
		if (type == typeof(Vector2))
		{
			List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>();
			KeyValuePair<string, object> item = new KeyValuePair<string, object>("X", ((Vector2)value).X);
			KeyValuePair<string, object> item2 = new KeyValuePair<string, object>("Y", ((Vector2)value).Y);
			list.Add(item);
			list.Add(item2);
			return list;
		}
		if (type == typeof(Vector2i))
		{
			List<KeyValuePair<string, object>> list2 = new List<KeyValuePair<string, object>>();
			KeyValuePair<string, object> item3 = new KeyValuePair<string, object>("X", ((Vector2i)value).X);
			KeyValuePair<string, object> item4 = new KeyValuePair<string, object>("Y", ((Vector2i)value).Y);
			list2.Add(item3);
			list2.Add(item4);
			return list2;
		}
		return value.ToString();
	}

	private static bool StateValueEquals(object value1, object value2, Type type)
	{
		if (value1 == null || value2 == null)
		{
			if (value1 == null)
			{
				return value2 == null;
			}
			return false;
		}
		if (type.IsSubclassOf(typeof(ActiveObject)) || type == typeof(ActiveObject))
		{
			return (value1 as ActiveObject).Uid.ToString() == (value2 as ActiveObject).Uid.ToString();
		}
		if (type == typeof(Vector2))
		{
			return ((Vector2)value1).Equals((Vector2)value2);
		}
		if (type == typeof(Vector2i))
		{
			return ((Vector2i)value1).Equals((Vector2i)value2);
		}
		return value1.ToString() == value2.ToString();
	}

	private static IEnumerable<KeyValuePair<string, object>> BehaviourToKeyPairs(object behaviour)
	{
		if (behaviour.GetType() == typeof(ExpandoObject))
		{
			return behaviour as IDictionary<string, object>;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(behaviour))
		{
			object value = property.GetValue(behaviour);
			dictionary.Add(property.Name, value);
		}
		return dictionary;
	}
}
