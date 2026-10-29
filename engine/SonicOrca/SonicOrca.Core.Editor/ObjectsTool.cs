using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Selects, moves, adds and deletes the objects and rings of a level, and changes their properties. The list has all
/// the object types of the game. A type that the level did not load loads when the user clicks it.
/// </summary>
internal sealed class ObjectsTool : EditorTool
{
	private const string ObjectKeyPrefix = "SONICORCA/OBJECTS/";

	/// <summary>The clipboard is shared by the levels, so the user can copy objects from one zone to another.</summary>
	private static readonly List<ObjectPlacement> Clipboard = new List<ObjectPlacement>();

	private readonly Dictionary<Guid, Vector2i> _moveStartPositions = new Dictionary<Guid, Vector2i>();

	/// <summary>The keys of all the object types in the resources, in name order.</summary>
	private string[] _allTypeKeys = Array.Empty<string>();

	private string _placeKey;

	private Guid _moveAnchor;

	private Vector2i _moveDelta;

	private bool _moving;

	private ResourceSession _typeSession;

	private Task _loadTask;

	private string _loadingKey;

	public ObjectsTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Objects";

	public override string Hint => _placeKey != null
		? $"Click in the level to add {ShortName(_placeKey)}. Hold Shift to add more. Esc to stop."
		: "Drag: move or select.  Right drag, Space drag or wheel: scroll.  Ctrl+wheel: zoom.  Del: delete.  Ctrl+Z: undo.";

	public override string Status => $"Objects {Placements.Count}   Selected {Selection.Count}";

	public override string ListHeading => "Object types (click, then click in the level)";

	public override string PropertiesHeading => Selection.Count == 0 ? "Properties (select an object)" : "Properties (click a value to change it)";

	public override bool HasSelection => Selection.Count > 0;

	private IList<ObjectPlacement> Placements => Editor.Placements;

	private HashSet<Guid> Selection => Editor.ObjectSelection;

	private Level Level => Editor.Level;

	public override void Begin()
	{
		_placeKey = null;
		_moving = false;
		GiveUniqueIds();
		_allTypeKeys = Editor.GameContext.ResourceTree.GetResourceListing()
			.Where(x => (x.Value.Identifier == ResourceTypeIdentifier.Object || x.Value.Identifier == ResourceTypeIdentifier.LuaObject) && x.Key.StartsWith(ObjectKeyPrefix, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.Key)
			.OrderBy(ShortName, StringComparer.Ordinal)
			.ToArray();
	}

	public override void Update()
	{
		if (_loadTask == null || !_loadTask.IsCompleted)
		{
			return;
		}
		string key = _loadingKey;
		Task task = _loadTask;
		_loadTask = null;
		_loadingKey = null;
		if (task.IsFaulted)
		{
			Editor.ShowMessage($"{ShortName(key)} did not load: {task.Exception?.InnerException?.Message}");
			return;
		}
		Level.ObjectManager.RegisterAndStartNewObjectTypes();
		if (FindType(key) == null)
		{
			Editor.ShowMessage($"{ShortName(key)} is not an object type that the editor can add.");
			return;
		}
		_placeKey = key;
	}

	public override void Dispose()
	{
		_typeSession?.Dispose();
		_typeSession = null;
	}

	#region List

	/// <summary>The types that the user cannot add: characters, animals and particles.</summary>
	private static bool IsPlaceable(ObjectType type)
	{
		return type.Classification != ObjectClassification.Character && type.Classification != ObjectClassification.Animal &&
			type.Classification != ObjectClassification.Particle;
	}

	private string[] GetListKeys()
	{
		return _allTypeKeys.Where(key => FindType(key) is not ObjectType type || IsPlaceable(type)).ToArray();
	}

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		Dictionary<string, int> counts = Placements.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
		return GetListKeys().Select(key =>
		{
			bool loaded = FindType(key) != null;
			return new EditorListRow
			{
				Text = ShortName(key),
				Detail = key == _loadingKey ? "loading" : counts.TryGetValue(key, out int count) ? count.ToString() : (loaded ? "" : "not loaded"),
				Active = string.Equals(key, _placeKey, StringComparison.OrdinalIgnoreCase),
				Dim = !loaded
			};
		}).ToArray();
	}

	public override void OnListClick(int index, int direction)
	{
		string[] keys = GetListKeys();
		if (index < 0 || index >= keys.Length || _loadTask != null)
		{
			return;
		}
		string key = keys[index];
		if (string.Equals(key, _placeKey, StringComparison.OrdinalIgnoreCase))
		{
			_placeKey = null;
			return;
		}
		if (FindType(key) != null)
		{
			_placeKey = key;
			return;
		}
		// The type loads in the background. The editor keeps it loaded until the area unloads.
		_typeSession ??= new ResourceSession(Editor.GameContext.ResourceTree);
		_typeSession.PushDependency(key);
		_loadingKey = key;
		_loadTask = _typeSession.LoadAsync();
		Editor.ShowMessage($"Loading {ShortName(key)}...");
	}

	#endregion

	#region Level

	public override void OnLevelPress()
	{
		if (_placeKey != null)
		{
			PlaceObject(_placeKey, Editor.Snap(Editor.MouseWorld));
			if (!Editor.IsShiftDown)
			{
				_placeKey = null;
			}
			return;
		}
		ActiveObject hit = HitTest(Editor.MouseWorld);
		if (hit == null)
		{
			if (!Editor.IsShiftDown)
			{
				Selection.Clear();
			}
			Editor.BeginBoxSelect();
			return;
		}
		Guid uid = hit.Entry.Uid;
		if (Editor.IsShiftDown && Selection.Contains(uid))
		{
			Selection.Remove(uid);
			return;
		}
		if (!Selection.Contains(uid))
		{
			if (!Editor.IsShiftDown)
			{
				Selection.Clear();
			}
			Selection.Add(uid);
		}
		_moveAnchor = uid;
		_moveDelta = new Vector2i(0, 0);
		_moveStartPositions.Clear();
		foreach (ObjectPlacement placement in Placements.Where(p => Selection.Contains(p.Uid)))
		{
			_moveStartPositions[placement.Uid] = placement.Position;
		}
		_moving = true;
		Editor.BeginToolDrag();
	}

	public override void OnDrag()
	{
		if (!_moving || !_moveStartPositions.TryGetValue(_moveAnchor, out Vector2i anchorStart))
		{
			return;
		}
		Vector2 target = (Vector2)anchorStart + (Editor.MouseWorld - Editor.DragStartWorld);
		_moveDelta = Editor.Snap(target) - anchorStart;
		// The active objects move while the user drags. The placements change at the end of the drag.
		foreach (ActiveObject activeObject in GetSelectableObjects())
		{
			if (_moveStartPositions.TryGetValue(activeObject.Entry.Uid, out Vector2i start))
			{
				activeObject.Position = start + _moveDelta;
			}
		}
	}

	public override void OnDragEnd()
	{
		_moving = false;
		if (_moveDelta == new Vector2i(0, 0))
		{
			return;
		}
		Editor.RecordHistory();
		ReplacePlacements(p => _moveStartPositions.TryGetValue(p.Uid, out Vector2i start) ? p.WithPosition(start + _moveDelta) : p);
	}

	public override void SelectInBox(Rectangle box)
	{
		foreach (ActiveObject activeObject in GetSelectableObjects().Where(o => GetBounds(o).IntersectsWith(box)))
		{
			Selection.Add(activeObject.Entry.Uid);
		}
	}

	public override bool Cancel()
	{
		if (_placeKey == null)
		{
			return false;
		}
		_placeKey = null;
		return true;
	}

	public override void ClearSelection() => Selection.Clear();

	#endregion

	#region Commands

	/// <summary>Adds an object of a type at a position, on the layer that most objects use.</summary>
	private void PlaceObject(string key, Vector2i position)
	{
		int count = Placements.Count(p => p.Key == key);
		string name = $"{FindType(key)?.Name ?? ShortName(key)} {count}";
		ObjectPlacement placement = new ObjectPlacement(key, Guid.NewGuid(), name, GetDefaultLayer(), position, new { });
		Editor.RecordHistory();
		Placements.Add(placement);
		Selection.Clear();
		Selection.Add(placement.Uid);
		Editor.MarkBindingChanged();
	}

	public override void DeleteSelection()
	{
		if (Selection.Count == 0)
		{
			return;
		}
		Editor.RecordHistory();
		foreach (ObjectPlacement placement in Placements.Where(p => Selection.Contains(p.Uid)).ToArray())
		{
			Placements.Remove(placement);
		}
		Selection.Clear();
		Editor.MarkBindingChanged();
	}

	public override void Nudge(Vector2i delta)
	{
		Editor.RecordHistory();
		ReplacePlacements(p => Selection.Contains(p.Uid) ? p.WithPosition(p.Position + delta) : p);
	}

	public override void Copy()
	{
		if (Selection.Count == 0)
		{
			return;
		}
		Clipboard.Clear();
		Clipboard.AddRange(Placements.Where(p => Selection.Contains(p.Uid)));
		Editor.ShowMessage($"Copied {Clipboard.Count} objects.");
	}

	public override void Duplicate()
	{
		ObjectPlacement[] selected = Placements.Where(p => Selection.Contains(p.Uid)).ToArray();
		if (selected.Length > 0)
		{
			AddCopies(selected, new Vector2i(Editor.GridSize * 2, Editor.GridSize * 2));
		}
	}

	/// <summary>Adds copies of the clipboard objects, with the centre of the group at a position.</summary>
	public override void Paste(Vector2 position)
	{
		ObjectPlacement[] available = Clipboard.Where(p => FindType(p.Key) != null).ToArray();
		if (available.Length == 0)
		{
			if (Clipboard.Count > 0)
			{
				Editor.ShowMessage("Load the object types of the clipboard first: click them in the list.");
			}
			return;
		}
		Vector2 centre = new Vector2(available.Average(p => (double)p.Position.X), available.Average(p => (double)p.Position.Y));
		AddCopies(available, Editor.Snap(position) - Editor.Snap(centre));
	}

	/// <summary>
	/// Adds copies of placements with new IDs. A mapping to an object that is also copied refers to the copy of that
	/// object.
	/// </summary>
	private void AddCopies(IReadOnlyList<ObjectPlacement> sources, Vector2i offset)
	{
		Dictionary<Guid, Guid> newIds = sources.ToDictionary(p => p.Uid, _ => Guid.NewGuid());
		Editor.RecordHistory();
		Selection.Clear();
		foreach (ObjectPlacement source in sources)
		{
			ObjectPlacement copy = source.With(newIds[source.Uid], source.Position + offset, source.Layer, source.Behaviour);
			copy.Mappings = source.Mappings
				.Select(m => Guid.TryParse(Convert.ToString(m.Value), out Guid target) && newIds.TryGetValue(target, out Guid newTarget)
					? new KeyValuePair<string, object>(m.Key, newTarget.ToString())
					: m)
				.ToList();
			Placements.Add(copy);
			Selection.Add(copy.Uid);
		}
		Editor.MarkBindingChanged();
	}

	public override void SelectAllInView()
	{
		Rectangle view = Editor.Camera.Bounds;
		foreach (ActiveObject activeObject in GetSelectableObjects().Where(o => view.IntersectsWith(GetBounds(o))))
		{
			Selection.Add(activeObject.Entry.Uid);
		}
	}

	private void ReplacePlacements(Func<ObjectPlacement, ObjectPlacement> change)
	{
		for (int i = 0; i < Placements.Count; i++)
		{
			Placements[i] = change(Placements[i]);
		}
		Editor.MarkBindingChanged();
	}

	private bool ReplaceWith(ObjectPlacement oldPlacement, ObjectPlacement newPlacement)
	{
		int index = Placements.IndexOf(oldPlacement);
		if (index < 0)
		{
			return false;
		}
		Editor.RecordHistory();
		Placements[index] = newPlacement;
		Editor.MarkBindingChanged();
		return true;
	}

	/// <summary>The editor finds objects by ID, so each placement must have a different ID.</summary>
	private void GiveUniqueIds()
	{
		HashSet<Guid> used = new HashSet<Guid>();
		for (int i = 0; i < Placements.Count; i++)
		{
			ObjectPlacement placement = Placements[i];
			if (placement.Uid == Guid.Empty || !used.Add(placement.Uid))
			{
				placement = placement.With(Guid.NewGuid(), placement.Position, placement.Layer, placement.Behaviour);
				Placements[i] = placement;
				used.Add(placement.Uid);
			}
		}
	}

	#endregion

	#region Properties

	public override List<EditorPropertyRow> GetPropertyRows()
	{
		List<EditorPropertyRow> rows = new List<EditorPropertyRow>();
		ObjectPlacement[] selected = Placements.Where(p => Selection.Contains(p.Uid)).ToArray();
		if (selected.Length != 1)
		{
			if (selected.Length > 1)
			{
				rows.Add(new EditorPropertyRow { Label = "Selected", Value = $"{selected.Length} objects", IsHeading = true });
				foreach (IGrouping<string, ObjectPlacement> group in selected.GroupBy(p => p.Key).OrderBy(g => g.Key))
				{
					rows.Add(new EditorPropertyRow { Label = ShortName(group.Key), Value = group.Count().ToString() });
				}
			}
			return rows;
		}
		ObjectPlacement placement = selected[0];
		rows.Add(new EditorPropertyRow { Label = "Type", Value = ShortName(placement.Key), IsHeading = true });
		rows.Add(new EditorPropertyRow
		{
			Label = "Name",
			Value = placement.Name ?? "",
			Commit = text => ReplaceWith(placement, placement.With(placement.Uid, placement.Position, placement.Layer, placement.Behaviour, text))
		});
		rows.Add(new EditorPropertyRow
		{
			Label = "X",
			Value = placement.Position.X.ToString(),
			Commit = text => int.TryParse(text, out int x) && ReplaceWith(placement, placement.WithPosition(new Vector2i(x, placement.Position.Y)))
		});
		rows.Add(new EditorPropertyRow
		{
			Label = "Y",
			Value = placement.Position.Y.ToString(),
			Commit = text => int.TryParse(text, out int y) && ReplaceWith(placement, placement.WithPosition(new Vector2i(placement.Position.X, y)))
		});
		IList<LevelLayer> layers = Level.Map.Layers;
		rows.Add(new EditorPropertyRow
		{
			Label = "Layer",
			Value = placement.Layer >= 0 && placement.Layer < layers.Count ? $"{placement.Layer}: {layers[placement.Layer].Name}" : placement.Layer.ToString(),
			Step = direction =>
			{
				int layer = ((placement.Layer + direction) % layers.Count + layers.Count) % layers.Count;
				ReplaceWith(placement, placement.With(placement.Uid, placement.Position, layer, placement.Behaviour));
			}
		});
		ObjectType type = FindType(placement.Key);
		if (type == null)
		{
			return rows;
		}
		rows.Add(new EditorPropertyRow { Label = "Properties", Value = "", IsHeading = true });
		foreach (EditorProperty property in EditorProperty.For(type))
		{
			object value = property.GetValue(placement);
			EditorPropertyRow row = new EditorPropertyRow { Label = property.Name, Value = EditorProperty.Format(value) };
			if (property.IsEditable)
			{
				if (property.Type == typeof(bool) || property.Type.IsEnum)
				{
					row.Step = direction => ReplaceWith(placement, property.SetValue(placement, property.Step(value, direction)));
				}
				else
				{
					row.Commit = text => property.TryParse(text, out object newValue) && ReplaceWith(placement, property.SetValue(placement, newValue));
				}
			}
			rows.Add(row);
		}
		return rows;
	}

	#endregion

	#region Drawing

	public override void DrawOverlay(Renderer renderer, I2dRenderer g)
	{
		ActiveObject hover = !_moving && Editor.IsMouseOverLevel && _placeKey == null ? HitTest(Editor.MouseWorld) : null;
		foreach (ActiveObject activeObject in GetSelectableObjects())
		{
			Rectangle bounds = Editor.ToScreen(GetBounds(activeObject));
			if (Selection.Contains(activeObject.Entry.Uid))
			{
				g.RenderQuad(LevelEditor.SelectionFillColour, bounds);
				g.RenderRectangle(LevelEditor.SelectionColour, bounds, 3.0);
			}
			else if (activeObject == hover)
			{
				g.RenderRectangle(LevelEditor.HoverColour, bounds, 2.0);
			}
			else
			{
				g.RenderRectangle(LevelEditor.OutlineColour, bounds, 1.0);
			}
		}
	}

	public override string GetCursorLabel(I2dRenderer g)
	{
		if (_placeKey != null)
		{
			Vector2 position = Editor.ToScreen((Vector2)Editor.Snap(Editor.MouseWorld));
			LevelEditor.DrawCross(g, position, 16.0, LevelEditor.SelectionColour, 2.0);
			return "+ " + ShortName(_placeKey);
		}
		ActiveObject hover = HitTest(Editor.MouseWorld);
		return hover == null ? null : ShortName(hover.Type.ResourceKey);
	}

	#endregion

	#region Objects

	/// <summary>The active objects that a placement made. Sub-objects are not selectable.</summary>
	private IEnumerable<ActiveObject> GetSelectableObjects()
	{
		return Level.ObjectManager.ActiveObjects.Where(o => !o.IsSubObject && o.Entry != null && o.Entry.Active == o);
	}

	private static Rectangle GetBounds(ActiveObject activeObject)
	{
		Rectanglei designBounds = activeObject.DesignBounds;
		double width = Math.Max(designBounds.Width, 32);
		double height = Math.Max(designBounds.Height, 32);
		double x = activeObject.Position.X + designBounds.X + (designBounds.Width - width) / 2.0;
		double y = activeObject.Position.Y + designBounds.Y + (designBounds.Height - height) / 2.0;
		return new Rectangle(x, y, width, height);
	}

	/// <summary>Finds the object under a position. When objects overlap, the smallest object is found.</summary>
	private ActiveObject HitTest(Vector2 world)
	{
		return GetSelectableObjects()
			.Where(o => GetBounds(o).Contains(world))
			.OrderBy(o => GetBounds(o).Width * GetBounds(o).Height)
			.FirstOrDefault();
	}

	private ObjectType FindType(string key)
	{
		return Level.ObjectManager.RegisteredTypes.FirstOrDefault(t => t.Resource != null && string.Equals(t.ResourceKey, key, StringComparison.OrdinalIgnoreCase));
	}

	private int GetDefaultLayer()
	{
		return Placements.GroupBy(p => p.Layer).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
	}

	private static string ShortName(string key)
	{
		return key.StartsWith(ObjectKeyPrefix, StringComparison.OrdinalIgnoreCase) ? key.Substring(ObjectKeyPrefix.Length) : key;
	}

	#endregion
}
