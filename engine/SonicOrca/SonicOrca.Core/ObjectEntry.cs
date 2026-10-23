using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SonicOrca.Core.Extensions;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// An object in the level map. The entry exists for all of the level, and it creates an active object
/// when the object comes into a lifetime area.
/// </summary>
/// <remarks>
/// The data in an object placement sets the fields and properties of the entry by name, also the private ones.
/// Do not rename the private fields.
/// </remarks>
public class ObjectEntry
{
	private readonly Level _level;

	private readonly ObjectType _type;

	private string _key;

	private ActiveObject _state;

	private Guid _uid;

	private string _name;

	private List<ObjectMapping> _mappings = new List<ObjectMapping>();

	public int Layer { get; set; }

	public Vector2i Position { get; set; }

	public bool FinishedForever { get; private set; }

	public ActiveObject Active { get; set; }

	public Level Level => _level;

	public ObjectType Type => _type;

	public ActiveObject State => _state;

	public string Key
	{
		get
		{
			return _key;
		}
		private set
		{
			_key = value;
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

	public IList<ObjectMapping> Mappings => _mappings;

	public Rectanglei LifetimeArea
	{
		get
		{
			Vector2 lifeRadius = _type.GetLifeRadius((IActiveObject)_state);
			Vector2 topLeft = new Vector2(Position.X - lifeRadius.X, Position.Y - lifeRadius.Y);
			return new Rectangle(topLeft.X, topLeft.Y, lifeRadius.X * 2.0, lifeRadius.Y * 2.0);
		}
	}

	public ObjectEntry(Level level, ObjectType type, LevelLayer layer, Vector2i position, Guid uid = default(Guid))
	{
		_level = level;
		Layer = Level.Map.Layers.IndexOf(layer);
		Position = position;
		_type = type;
		_uid = uid;
		throw new NotImplementedException();
	}

	public ObjectEntry(Level level, ObjectPlacement placement)
	{
		if (placement.Entry.Count > 0)
		{
			SetKeyValuePairReflection(placement.Entry, this);
		}
		else
		{
			_uid = Guid.NewGuid();
		}
		_level = level;
		_type = level.GameContext.ResourceTree.GetLoadedResource<ObjectType>(_key);
		CreateState();
		if (placement.Behaviour.Count > 0)
		{
			SetKeyValuePairReflection(placement.Behaviour, _state);
		}
		if (placement.Mappings.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<string, object> mapping in placement.Mappings)
		{
			Mappings.Add(new ObjectMapping(mapping.Key, Guid.Parse(mapping.Value.ToString())));
		}
	}

	private void CreateState()
	{
		Type objectInstanceType = (ObjectInstanceAttribute.FromObject(_type) ?? throw new Exception()).ObjectInstanceType;
		_state = Activator.CreateInstance(objectInstanceType) as ActiveObject;
	}


	public void Finish()
	{
		if (Active != null)
		{
			Active.Finish();
		}
	}

	public void FinishForever()
	{
		FinishedForever = true;
	}

	public ActiveObject CreateActiveObject()
	{
		Active = Type.CreateInstance();
		Active.Initialise(this);
		return Active;
	}

	public T CreateSubObject<T>() where T : ActiveObject
	{
		Active = Activator.CreateInstance<T>();
		Active.Initialise(this);
		return (T)Active;
	}

	/// <summary>
	/// Sets the fields and properties of the target object from the key and value pairs, by member name.
	/// A value that is a list of pairs sets the members of the nested object, except for a reference to an active object.
	/// </summary>
	private object SetKeyValuePairReflection(IEnumerable<KeyValuePair<string, object>> behaviour, object targetObject)
	{
		foreach (KeyValuePair<string, object> pair in behaviour)
		{
			MemberInfo memberInfo = FindMember(targetObject, pair.Key);
			if (pair.Value is IEnumerable<KeyValuePair<string, object>> nestedPairs)
			{
				object memberValue = memberInfo.GetUnderlyingValue(targetObject);
				if (!memberInfo.GetUnderlyingType().IsSubclassOf(typeof(ActiveObject)) && !(memberInfo.GetUnderlyingType() == typeof(IActiveObject)))
				{
					memberValue = SetKeyValuePairReflection(nestedPairs, memberValue);
					memberInfo.SetUnderlyingValue(targetObject, memberValue);
				}
			}
			else
			{
				string value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
				memberInfo.SetUnderlyingValue(targetObject, LevelBindingResourceType.ParseBehaviourValue(value, memberInfo.GetUnderlyingType()));
			}
		}
		return targetObject;
	}

	private static MemberInfo FindMember(object targetObject, string name)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		MemberInfo memberInfo = targetObject.GetType().GetMembers(bindingAttr).FirstOrDefault(m => m.Name == name);
		if (memberInfo == null)
		{
			throw new Exception();
		}
		return memberInfo;
	}

	public override string ToString()
	{
		return Name;
	}
}
