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

/// <summary>
/// The data to create an object entry: the type key, unique ID, name, layer and position,
/// and the behaviour and mapping values of the object, as key and value pairs.
/// </summary>
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
		: this(key, layer, position, new { })
	{
	}

	public ObjectPlacement(string key, int layer, Vector2i position, object state)
		: this(key, Guid.NewGuid(), "", layer, position, state)
	{
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

	/// <summary>
	/// Makes a copy of the placement with a different unique ID, position, layer, behaviour and name (null keeps the
	/// name). The copy keeps the key and the mappings. The level editor changes a placement with a copy, so that the undo history
	/// can keep the old placement.
	/// </summary>
	public ObjectPlacement With(Guid uid, Vector2i position, int layer, IEnumerable<KeyValuePair<string, object>> behaviour, string name = null)
	{
		IDictionary<string, object> state = new ExpandoObject();
		foreach (KeyValuePair<string, object> pair in behaviour)
		{
			state[pair.Key] = pair.Value;
		}
		return new ObjectPlacement(_key, uid, name ?? _name ?? "", layer, position, state)
		{
			Mappings = _mappings.ToList()
		};
	}

	/// <summary>Makes a copy of the placement at a different position.</summary>
	public ObjectPlacement WithPosition(Vector2i position) => With(_uid, position, _layer, _behaviour);

	/// <summary>
	/// Converts the entry values and the state object to key and value pairs.
	/// An anonymous or dynamic state gives all its properties. Other states give only the state variables
	/// that are different from the default value.
	/// </summary>
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
			if (item.Value is IEnumerable<KeyValuePair<string, object>> nestedPairs)
			{
				WriteBehaviourString(sb, nestedPairs);
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
		foreach (Tuple<MemberInfo, StateVariableAttribute> stateVariable in stateVariables)
		{
			MemberInfo member = stateVariable.Item1;
			Type underlyingType = member.GetUnderlyingType();
			string name = member.Name;
			object value = ParseStateValue(member.GetUnderlyingValue(obj), underlyingType);
			MemberInfo defaultMember = defaultType.GetMember(member.Name, bindingFlags).First();
			// The result is not used, but the call throws when the member is not a field, property, method or event.
			defaultMember.GetUnderlyingType();
			object currentValue = member.GetUnderlyingValue(obj);
			object defaultValue = defaultMember.GetUnderlyingValue(defaultState);
			if (!StateValueEquals(currentValue, defaultValue, underlyingType))
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
			Vector2 vector = (Vector2)value;
			return new List<KeyValuePair<string, object>>
			{
				new KeyValuePair<string, object>("X", vector.X),
				new KeyValuePair<string, object>("Y", vector.Y)
			};
		}
		if (type == typeof(Vector2i))
		{
			Vector2i vector = (Vector2i)value;
			return new List<KeyValuePair<string, object>>
			{
				new KeyValuePair<string, object>("X", vector.X),
				new KeyValuePair<string, object>("Y", vector.Y)
			};
		}
		return value.ToString();
	}

	private static bool StateValueEquals(object value1, object value2, Type type)
	{
		if (value1 == null || value2 == null)
		{
			return value1 == null && value2 == null;
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
