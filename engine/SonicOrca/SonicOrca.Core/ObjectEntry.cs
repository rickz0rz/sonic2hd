using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SonicOrca.Core.Extensions;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

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
			Vector2 vector = new Vector2((double)Position.X - lifeRadius.X, (double)Position.Y - lifeRadius.Y);
			return new Rectangle(vector.X, vector.Y, lifeRadius.X * 2.0, lifeRadius.Y * 2.0);
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

	private void RebindState()
	{
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

	private object SetKeyValuePairReflection(IEnumerable<KeyValuePair<string, object>> behaviour, object targetObject)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		foreach (KeyValuePair<string, object> kvp in behaviour)
		{
			if (kvp.Value is IEnumerable<KeyValuePair<string, object>>)
			{
				object obj = null;
				MemberInfo memberInfo = (from m in targetObject.GetType().GetMembers(bindingAttr)
										 select (m) into m
										 where m.Name == kvp.Key
										 select m).FirstOrDefault();
				if (!(memberInfo != null))
				{
					throw new Exception();
				}
				obj = memberInfo.GetUnderlyingValue(targetObject);
				if (!memberInfo.GetUnderlyingType().IsSubclassOf(typeof(ActiveObject)) && !(memberInfo.GetUnderlyingType() == typeof(IActiveObject)))
				{
					obj = SetKeyValuePairReflection((IEnumerable<KeyValuePair<string, object>>)kvp.Value, obj);
					memberInfo.SetUnderlyingValue(targetObject, obj);
				}
			}
			else
			{
				MemberInfo memberInfo2 = (from m in targetObject.GetType().GetMembers(bindingAttr)
										  select (m) into m
										  where m.Name == kvp.Key
										  select m).FirstOrDefault();
				if (!(memberInfo2 != null))
				{
					throw new Exception();
				}
				string value = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture);
				memberInfo2.SetUnderlyingValue(targetObject, LevelBindingResourceType.ParseBehaviourValue(value, memberInfo2.GetUnderlyingType()));
			}
		}
		return targetObject;
	}

	public override string ToString()
	{
		return Name;
	}
}
