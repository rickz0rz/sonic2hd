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

/// <summary>
/// Keeps the object entries and the active objects of a level. It activates the entries in the lifetime areas,
/// updates and draws the active objects, and deactivates the objects that go out of the lifetime areas.
/// </summary>
public class ObjectManager
{
	/// <summary>
	/// An object mapping can refer to a public or a private field or property.
	/// </summary>
	private const BindingFlags MappingBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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

	public IEnumerable<ICharacter> Characters => _activeObjects.Where(x => x.Type.Classification == ObjectClassification.Character).Cast<ICharacter>();

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

	/// <summary>
	/// Stops all the active objects, also the characters, and makes the entries again from the binding. The level
	/// editor calls this method after each change, so that the level shows all the placements as they are.
	/// </summary>
	public void Rebind(LevelBinding binding)
	{
		foreach (ActiveObject activeObject in _activeObjects.Concat(_newActiveObjects).ToArray())
		{
			activeObject.Finish();
			activeObject.Stop();
			if (activeObject.Entry != null)
			{
				activeObject.Entry.Active = null;
			}
		}
		_activeObjects.Clear();
		_newActiveObjects.Clear();
		_respawnPrevention.Clear();
		_objectEntryTable.Initialise(binding);
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
		// The priority pass draws the objects with a positive priority. The other pass draws the objects with
		// a negative priority. Objects with a priority of zero are not drawn.
		IEnumerable<ActiveObject> objectsToDraw = _activeObjects
			.Where(activeObject =>
			{
				if (activeObject.Layer != layer || activeObject.Priority == 0)
				{
					return false;
				}
				return priority ? activeObject.Priority > 0 : activeObject.Priority < 0;
			})
			.OrderBy(x => x.Priority);
		foreach (ActiveObject activeObject in objectsToDraw)
		{
			activeObject.Draw(renderer, viewport, viewOptions);
		}
	}

	public void DrawDebugInfo(Renderer renderer)
	{
		double y = 500.0;
		y += _level.DebugContext.DrawText(renderer, $"TOTAL OBJECTS: {_objectEntryTable.Count}", FontAlignment.Left, 8.0, y, 0.75, 0);
		y += _level.DebugContext.DrawText(renderer, $"ACTIVE OBJECTS: {_activeObjects.Count}", FontAlignment.Left, 8.0, y, 0.75, 0);
		y += _level.DebugContext.DrawText(renderer, $"RESPAWN PREVENTED OBJECTS: {_respawnPrevention.Count}", FontAlignment.Left, 8.0, y, 0.75, 0);
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
		// Iterate over a copy, because DeactivateObject removes the object from the list.
		ActiveObject[] activeObjects = _activeObjects.ToArray();
		foreach (ActiveObject activeObject in activeObjects)
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
			int sameTypeCount = _objectEntryTable.Count(e => e.Type == objectEntry.Type);
			objectEntry.Name = objectEntry.Type.Name + " " + sameTypeCount;
		}
		_objectEntryTable.Add(objectEntry);
	}

	public T AddSubObject<T>(ActiveObject parentObject) where T : ActiveObject
	{
		ObjectEntry objectEntry = new ObjectEntry(_level, new ObjectPlacement(parentObject.Type.ResourceKey, _level.Map.Layers.IndexOf(parentObject.Layer), parentObject.Position));
		return ActiveSubObject<T>(objectEntry, parentObject);
	}

	/// <summary>
	/// Connects a new active object to the objects that its mappings refer to,
	/// and connects the objects whose mappings refer to the new object.
	/// </summary>
	private void MapInstancesOf(ActiveObject activeObject)
	{
		ObjectEntry entry = _objectEntryTable.FirstOrDefault(e => e.Uid == activeObject.Uid);
		IEnumerable<ActiveObject> allObjects = _activeObjects.Concat(_newActiveObjects);
		if (entry == null)
		{
			return;
		}
		foreach (ObjectMapping mapping in entry.Mappings)
		{
			ActiveObject target = allObjects.FirstOrDefault(e => e.Uid == mapping.Target);
			if (target != null)
			{
				SetMappedMember(activeObject, mapping.Field, target);
			}
		}
		foreach (ActiveObject mappedObject in allObjects.Where(ao => ao.Entry.Mappings.FirstOrDefault(m => m.Target == entry.Uid) != null))
		{
			foreach (ObjectMapping mapping in mappedObject.Entry.Mappings)
			{
				if (mapping.Target == activeObject.Uid)
				{
					SetMappedMember(mappedObject, mapping.Field, activeObject);
				}
			}
		}
	}

	/// <summary>
	/// Clears the mapped members of an active object that stops, and clears the members of other objects
	/// that refer to it.
	/// </summary>
	private void UnMapInstancesOf(ActiveObject activeObject)
	{
		ObjectEntry entry = _objectEntryTable.FirstOrDefault(e => e.Uid == activeObject.Uid);
		if (entry == null)
		{
			return;
		}
		foreach (ObjectMapping mapping in entry.Mappings)
		{
			SetMappedMember(activeObject, mapping.Field, null);
		}
		foreach (ActiveObject mappedObject in _activeObjects.Where(ao => ao.Entry.Mappings.FirstOrDefault(m => m.Target == entry.Uid) != null))
		{
			foreach (ObjectMapping mapping in mappedObject.Entry.Mappings)
			{
				if (mapping.Target == activeObject.Uid)
				{
					SetMappedMember(mappedObject, mapping.Field, null);
				}
			}
		}
	}

	private static void SetMappedMember(ActiveObject owner, string field, ActiveObject value)
	{
		// First() throws when the member does not exist, so the null check is always true.
		MemberInfo memberInfo = owner.GetType().GetMember(field, MappingBindingFlags).First();
		if (memberInfo != null)
		{
			memberInfo.SetUnderlyingValue(owner, value);
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
		T subObject = objectEntry.CreateSubObject<T>();
		_newActiveObjects.Add(subObject);
		subObject.ParentObject = parentObject;
		subObject.Start();
		return subObject;
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
		return _lifeTimeAreas.Any(r => rect.IntersectsWith(r));
	}

	private void ManageObjects()
	{
		// An entry that leaves all lifetime areas can respawn again.
		IEnumerable<ObjectEntry> entriesOutOfRange = _respawnPrevention.Where(x => !IsInLifetimeArea(x.LifetimeArea)).ToArray();
		_respawnPrevention.ExceptWith(entriesOutOfRange);

		// Deactivate the objects that are out of the lifetime areas.
		ActiveObject[] objectsOutOfRange = _activeObjects.Where(x => !x.LockLifetime && !IsInLifetimeArea(x.LifetimeArea) && (!IsInLifetimeArea(x.Entry.LifetimeArea) || x.IsSubObject)).ToArray();
		foreach (ActiveObject activeObject in objectsOutOfRange)
		{
			DeactivateObject(activeObject);
			_respawnPrevention.Remove(activeObject.Entry);
		}

		// Activate the entries in the lifetime areas.
		List<ObjectEntry> entriesInRange = new List<ObjectEntry>();
		foreach (Rectanglei lifeTimeArea in _lifeTimeAreas)
		{
			entriesInRange.AddRange(_objectEntryTable.GetAllInRegion(lifeTimeArea));
		}
		foreach (ObjectEntry entry in entriesInRange)
		{
			if (entry.Active == null && !_respawnPrevention.Contains(entry))
			{
				ActivateObjectEntry(entry);
			}
		}
	}

	public void RegisterObjectTypes()
	{
		ObjectType[] newTypes = ObjectType.LoadedTypes.Except(_registeredTypes).ToArray();
		foreach (ObjectType objectType in newTypes)
		{
			_registeredTypes.Add(objectType);
			objectType.Register(_level);
		}
	}

	/// <summary>
	/// Registers and starts the object types that loaded after the level started, for example a type that the level
	/// editor loaded.
	/// </summary>
	public void RegisterAndStartNewObjectTypes()
	{
		ObjectType[] newTypes = ObjectType.LoadedTypes.Except(_registeredTypes).ToArray();
		RegisterObjectTypes();
		foreach (ObjectType objectType in newTypes)
		{
			objectType.Start();
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
		ActiveObject[] finishedObjects = _activeObjects.Where(x => x.Finished).ToArray();
		foreach (ActiveObject activeObject in finishedObjects)
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
		IEnumerable<ICharacter> candidates = from x in _activeObjects
											 where x.Type.Classification == ObjectClassification.Character
											 select (ICharacter)x into x
											 where !x.IsDead && !x.IsDebug && !x.IsDying
											 select x;
		ICharacter closestCharacter = null;
		double closestDistance = double.NaN;
		foreach (ICharacter character in candidates)
		{
			double distance = (position - character.Position).Length;
			if (closestCharacter == null || distance < closestDistance)
			{
				closestCharacter = character;
				closestDistance = distance;
			}
		}
		return closestCharacter;
	}

	public void FinishSubObjects(ActiveObject parent)
	{
		foreach (ActiveObject subObject in _activeObjects.Where(x => x.ParentObject == parent))
		{
			subObject.Finish();
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
