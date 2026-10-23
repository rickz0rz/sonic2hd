#define TRACE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Extensions;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

public class ObjectManager
{
	private readonly List<ObjectType> _registeredTypes = new List<ObjectType>();

	private readonly ObjectEntryTable _objectEntryTable;

	private readonly List<ActiveObject> _activeObjects = new List<ActiveObject>();

	private readonly List<ActiveObject> _newActiveObjects = new List<ActiveObject>();

	private readonly HashSet<ObjectEntry> _respawnPrevention = new HashSet<ObjectEntry>();

	private readonly List<Rectanglei> _lifeTimeAreas = new List<Rectanglei>();

	private readonly Level _level;

	public IReadOnlyCollection<ObjectType> RegisteredTypes => _registeredTypes;

	public ICollection<ActiveObject> ActiveObjects => _activeObjects;

	public ObjectEntryTable ObjectEntryTable => _objectEntryTable;

	public IEnumerable<ICharacter> Characters => _activeObjects.Where((ActiveObject x) => x.Type.Classification == ObjectClassification.Character).Cast<ICharacter>();

	public ObjectManager(Level level)
	{
		_level = level;
		_objectEntryTable = new ObjectEntryTable(level);
	}

	public void Setup(LevelMap map)
	{
		Trace.WriteLine("Setting up object manager");
		Trace.Indent();
		Trace.WriteLine("Registering object types");
		RegisterObjectTypes();
		Trace.Unindent();
	}

	public void Bind(LevelBinding binding)
	{
		Trace.Indent();
		Trace.WriteLine("Clearing objects and object entries");
		_activeObjects.Clear();
		_objectEntryTable.Clear();
		Trace.WriteLine("Initialising object entry table");
		_objectEntryTable.Initialise(binding);
		Trace.Unindent();
	}

	public void ResetLifetimeArea()
	{
		_lifeTimeAreas.Clear();
	}

	public void AddLifeArea(Rectanglei area)
	{
		_lifeTimeAreas.Add(area);
	}

	public void Update()
	{
		ManageObjects();
		UpdateObjectTypes();
		UpdatePrepareActiveObjects();
		UpdateActiveObjects();
		UpdateCollisionActiveObjects();
		RemoveFinishedActiveObjects();
		_activeObjects.AddRange(_newActiveObjects);
		_newActiveObjects.Clear();
		_objectEntryTable.RemoveFinishedEntries();
	}

	public void UpdateEditor()
	{
		ManageObjects();
		UpdateEditorActiveObjects();
		RemoveFinishedActiveObjects();
		_respawnPrevention.Clear();
		_activeObjects.AddRange(_newActiveObjects);
		_newActiveObjects.Clear();
		_objectEntryTable.RemoveFinishedEntries();
	}

	public void Animate()
	{
		AnimateObjectTypes();
		AnimateActiveObjects();
	}

	public void Draw(Renderer renderer, Viewport viewport, LevelLayer layer, LayerViewOptions viewOptions, bool priority)
	{
		foreach (ActiveObject item in from x in _activeObjects.Where((ActiveObject activeObject) =>
			{
				if (activeObject.Layer != layer)
				{
					return false;
				}
				if (activeObject.Priority == 0)
				{
					return false;
				}
				if ((activeObject.Priority < 0) & priority)
				{
					return false;
				}
				return (activeObject.Priority <= 0 || priority) ? true : false;
			})
									  orderby x.Priority
									  select x)
		{
			item.Draw(renderer, viewport, viewOptions);
		}
	}

	public void DrawDebugInfo(Renderer renderer)
	{
		double num = 500.0;
		num += _level.DebugContext.DrawText(renderer, $"TOTAL OBJECTS: {_objectEntryTable.Count}", FontAlignment.Left, 8.0, num, 0.75, 0);
		num += _level.DebugContext.DrawText(renderer, $"ACTIVE OBJECTS: {_activeObjects.Count}", FontAlignment.Left, 8.0, num, 0.75, 0);
		num += _level.DebugContext.DrawText(renderer, $"RESPAWN PREVENTED OBJECTS: {_respawnPrevention.Count}", FontAlignment.Left, 8.0, num, 0.75, 0);
	}

	public void Start()
	{
		Trace.WriteLine("Registering more object types");
		RegisterObjectTypes();
		StartObjectTypes();
	}

	public void Stop()
	{
		_objectEntryTable.Clear();
		ActiveObject[] array = _activeObjects.ToArray();
		foreach (ActiveObject activeObject in array)
		{
			DeactivateObject(activeObject);
		}
		StopObjectTypes();
		UnregisterObjectTypes();
	}

	public ActiveObject AddObject(ObjectPlacement objectPlacement)
	{
		ObjectEntry objectEntry = new ObjectEntry(_level, objectPlacement);
		return ActivateObjectEntry(objectEntry);
	}

	public void AddObjectEntry(ObjectEntry objectEntry)
	{
		if (string.IsNullOrEmpty(objectEntry.Name))
		{
			int num = _objectEntryTable.Where((ObjectEntry e) => e.Type == objectEntry.Type).Count();
			objectEntry.Name = objectEntry.Type.Name + " " + num;
		}
		_objectEntryTable.Add(objectEntry);
	}

	public T AddSubObject<T>(ActiveObject parentObject) where T : ActiveObject
	{
		ObjectEntry objectEntry = new ObjectEntry(_level, new ObjectPlacement(parentObject.Type.ResourceKey, _level.Map.Layers.IndexOf(parentObject.Layer), parentObject.Position));
		return ActiveSubObject<T>(objectEntry, parentObject);
	}

	private void MapInstancesOf(ActiveObject activeObject)
	{
		ObjectEntry entry = (from e in _objectEntryTable
							 select (e) into e
							 where e.Uid == activeObject.Uid
							 select e).FirstOrDefault();
		IEnumerable<ActiveObject> source = _activeObjects.Concat(_newActiveObjects);
		if (entry == null)
		{
			return;
		}
		foreach (ObjectMapping mapping in entry.Mappings)
		{
			ActiveObject activeObject2 = (from e in source
										  select (e) into e
										  where e.Uid == mapping.Target
										  select e).FirstOrDefault();
			if (activeObject2 != null)
			{
				BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				MemberInfo memberInfo = activeObject.GetType().GetMember(mapping.Field, bindingAttr).First();
				if (memberInfo != null)
				{
					memberInfo.SetUnderlyingValue(activeObject, activeObject2);
				}
			}
		}
		foreach (ActiveObject item in from ao in source
									  select (ao) into ao
									  where ao.Entry.Mappings.FirstOrDefault((ObjectMapping m) => m.Target == entry.Uid) != null
									  select ao)
		{
			foreach (ObjectMapping mapping2 in item.Entry.Mappings)
			{
				if (mapping2.Target == activeObject.Uid)
				{
					BindingFlags bindingAttr2 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
					MemberInfo memberInfo2 = item.GetType().GetMember(mapping2.Field, bindingAttr2).First();
					if (memberInfo2 != null)
					{
						memberInfo2.SetUnderlyingValue(item, activeObject);
					}
				}
			}
		}
	}

	private void UnMapInstancesOf(ActiveObject activeObject)
	{
		ObjectEntry entry = (from e in _objectEntryTable
							 select (e) into e
							 where e.Uid == activeObject.Uid
							 select e).FirstOrDefault();
		if (entry == null)
		{
			return;
		}
		if (entry != null)
		{
			foreach (ObjectMapping mapping in entry.Mappings)
			{
				BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				MemberInfo memberInfo = activeObject.GetType().GetMember(mapping.Field, bindingAttr).First();
				if (memberInfo != null)
				{
					memberInfo.SetUnderlyingValue(activeObject, null);
				}
			}
		}
		foreach (ActiveObject item in from ao in _activeObjects
									  select (ao) into ao
									  where ao.Entry.Mappings.FirstOrDefault((ObjectMapping m) => m.Target == entry.Uid) != null
									  select ao)
		{
			foreach (ObjectMapping mapping2 in item.Entry.Mappings)
			{
				if (mapping2.Target == activeObject.Uid)
				{
					BindingFlags bindingAttr2 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
					MemberInfo memberInfo2 = item.GetType().GetMember(mapping2.Field, bindingAttr2).First();
					if (memberInfo2 != null)
					{
						memberInfo2.SetUnderlyingValue(item, null);
					}
				}
			}
		}
	}

	public ActiveObject ActivateObjectEntry(ObjectEntry objectEntry)
	{
		ActiveObject activeObject = objectEntry.CreateActiveObject();
		_newActiveObjects.Add(activeObject);
		MapInstancesOf(activeObject);
		activeObject.Start();
		return activeObject;
	}

	public T ActiveSubObject<T>(ObjectEntry objectEntry, ActiveObject parentObject) where T : ActiveObject
	{
		T val = objectEntry.CreateSubObject<T>();
		_newActiveObjects.Add(val);
		val.ParentObject = parentObject;
		val.Start();
		return val;
	}

	public void DeactivateObject(ActiveObject activeObject)
	{
		activeObject.Finish();
		activeObject.Stop();
		ObjectEntry entry = activeObject.Entry;
		entry.Active = null;
		if (entry.FinishedForever)
		{
			_objectEntryTable.Remove(entry);
		}
		else
		{
			_respawnPrevention.Add(entry);
		}
		UnMapInstancesOf(activeObject);
		_activeObjects.Remove(activeObject);
	}

	public bool IsInLifetimeArea(Rectanglei rect)
	{
		return _lifeTimeAreas.Any((Rectanglei r) => rect.IntersectsWith(r));
	}

	private void ManageObjects()
	{
		IEnumerable<ObjectEntry> other = _respawnPrevention.Where((ObjectEntry x) => !IsInLifetimeArea(x.LifetimeArea)).ToArray();
		_respawnPrevention.ExceptWith(other);
		foreach (ActiveObject item in (IEnumerable<ActiveObject>)_activeObjects.Where((ActiveObject x) => !x.LockLifetime && !IsInLifetimeArea(x.LifetimeArea) && (!IsInLifetimeArea(x.Entry.LifetimeArea) || x.IsSubObject)).ToArray())
		{
			DeactivateObject(item);
			_respawnPrevention.Remove(item.Entry);
		}
		List<ObjectEntry> list = new List<ObjectEntry>();
		foreach (Rectanglei lifeTimeArea in _lifeTimeAreas)
		{
			list.AddRange(_objectEntryTable.GetAllInRegion(lifeTimeArea));
		}
		foreach (ObjectEntry item2 in list)
		{
			if (item2.Active == null && !_respawnPrevention.Contains(item2))
			{
				ActivateObjectEntry(item2);
			}
		}
	}

	public void RegisterObjectTypes()
	{
		ObjectType[] array = ObjectType.LoadedTypes.Except(_registeredTypes).ToArray();
		foreach (ObjectType objectType in array)
		{
			_registeredTypes.Add(objectType);
			objectType.Register(_level);
		}
	}

	public void UnregisterObjectTypes()
	{
		foreach (ObjectType registeredType in _registeredTypes)
		{
			registeredType.Unregister();
		}
		_registeredTypes.Clear();
	}

	private void StartObjectTypes()
	{
		foreach (ObjectType registeredType in _registeredTypes)
		{
			registeredType.Start();
		}
	}

	private void StopObjectTypes()
	{
		foreach (ObjectType registeredType in _registeredTypes)
		{
			registeredType.Stop();
		}
	}

	private void UpdateObjectTypes()
	{
		foreach (ObjectType registeredType in _registeredTypes)
		{
			registeredType.Update();
		}
	}

	private void AnimateObjectTypes()
	{
		foreach (ObjectType registeredType in _registeredTypes)
		{
			registeredType.Animate();
		}
	}

	private void UpdateEditorActiveObjects()
	{
		foreach (ActiveObject activeObject in _activeObjects)
		{
			activeObject.UpdateEditor();
		}
	}

	private void UpdatePrepareActiveObjects()
	{
		foreach (ActiveObject activeObject in _activeObjects)
		{
			activeObject.UpdatePrepare();
		}
	}

	private void UpdateActiveObjects()
	{
		foreach (ActiveObject activeObject in _activeObjects)
		{
			activeObject.Update();
		}
	}

	private void UpdateCollisionActiveObjects()
	{
		foreach (ActiveObject activeObject in _activeObjects)
		{
			activeObject.UpdateCollision();
		}
	}

	private void AnimateActiveObjects()
	{
		foreach (ActiveObject activeObject in _activeObjects)
		{
			activeObject.Animate();
		}
	}

	public void RemoveFinishedActiveObjects()
	{
		ActiveObject[] array = _activeObjects.Where((ActiveObject x) => x.Finished).ToArray();
		foreach (ActiveObject activeObject in array)
		{
			DeactivateObject(activeObject);
		}
	}

	public void RemoveFinishedEntries()
	{
		_objectEntryTable.RemoveFinishedEntries();
	}

	public ICharacter GetClosestCharacterTo(Vector2 position)
	{
		IEnumerable<ICharacter> enumerable = from x in _activeObjects
											 where x.Type.Classification == ObjectClassification.Character
											 select (ICharacter)x into x
											 where !x.IsDead && !x.IsDebug && !x.IsDying
											 select x;
		ICharacter character = null;
		double num = double.NaN;
		foreach (ICharacter item in enumerable)
		{
			double length = (position - item.Position).Length;
			if (character == null || length < num)
			{
				character = item;
				num = length;
			}
		}
		return character;
	}

	public void FinishSubObjects(ActiveObject parent)
	{
		foreach (ActiveObject item in _activeObjects.Where((ActiveObject x) => x.ParentObject == parent))
		{
			item.Finish();
		}
	}

	public bool IsCharacterStandingOn(CollisionVector v)
	{
		foreach (ICharacter character in Characters)
		{
			if (character.GroundVector == v)
			{
				return true;
			}
		}
		return false;
	}
}
