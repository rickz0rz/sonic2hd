using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Selects, moves, adds and deletes the objects and rings of a level, and changes their properties. The list has all
/// the object types of the game. A type that the level did not load loads when the user clicks it. The first row of
/// the list makes a new Lua object (see <see cref="NewObject"/>).
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

	/// <summary>The properties panel shows the form of a new object.</summary>
	private bool _creating;

	private string _newName = "";

	private string _newKey = "";

	private NewObjectKind _newKind = NewObjectKind.Scenery;

	/// <summary>The path of the image of the new object, or null for a placeholder image.</summary>
	private string _newImagePath;

	public ObjectsTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Objects";

	public override string Hint => _creating
		? "Type a name, choose a kind and an image, then click Create. Esc: close the form."
		: _placeKey != null
		? $"Click in the level to add {ShortName(_placeKey)}. Hold Shift to add more. Esc to stop."
		: "Drag: move or select.  Right drag, Space drag or wheel: scroll.  Ctrl+wheel: zoom.  Del: delete.  Ctrl+Z: undo.";

	public override string Status => $"Objects {Placements.Count}   Selected {Selection.Count}";

	public override string ListHeading => "Object types (click, then click in the level)";

	public override string PropertiesHeading => _creating ? "New object (click a value to change it)" : Selection.Count == 0 ? "Properties (select an object)" : "Properties (click a value to change it)";

	public override bool HasSelection => Selection.Count > 0;

	private IList<ObjectPlacement> Placements => Editor.Placements;

	private HashSet<Guid> Selection => Editor.ObjectSelection;

	private Level Level => Editor.Level;

	public override void Begin()
	{
		_placeKey = null;
		_moving = false;
		GiveUniqueIds();
		ReadTypeKeys();
	}

	private void ReadTypeKeys()
	{
		_allTypeKeys = Editor.GameContext.ResourceTree.GetResourceListing()
			.Where(x => (x.Value.Identifier == ResourceTypeIdentifier.Object || x.Value.Identifier == ResourceTypeIdentifier.LuaObject) && x.Key.StartsWith(ObjectKeyPrefix, StringComparison.OrdinalIgnoreCase))
			// An item gives an ability to a character, so the list does not show abilities, also before they load
			.Where(x => x.Value.Identifier != ResourceTypeIdentifier.LuaObject || !SonicOrca.Core.Scripting.LuaObjectType.IsAbilityScript(x.Value))
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
			Editor.ShowMessage($"{(key == null ? "The type of the property" : ShortName(key))} did not load: {task.Exception?.InnerException?.Message}");
			return;
		}
		Level.ObjectManager.RegisterAndStartNewObjectTypes();
		if (key == null)
		{
			// A type that a property value names, not a type to place
			Editor.ShowMessage("Loaded the object types of the property.");
			return;
		}
		ObjectType loaded = FindType(key);
		if (loaded == null)
		{
			Editor.ShowMessage($"{ShortName(key)} is not an object type that the editor can add.");
			return;
		}
		if (!IsPlaceable(loaded))
		{
			Editor.ShowMessage(loaded is SonicOrca.Core.Scripting.LuaObjectType { IsAbility: true }
				? $"{ShortName(key)} is an ability. Add the item that gives it, for example LUAFEATHER for LUADOUBLEJUMP."
				: $"{ShortName(key)} is not an object type that the editor can add.");
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

	/// <summary>The first row makes a new object. The other rows are the object types.</summary>
	private const int NewRowCount = 1;

	/// <summary>
	/// The types that the user cannot add: characters, animals, particles, and Lua abilities. An item gives an ability
	/// to a character.
	/// </summary>
	private static bool IsPlaceable(ObjectType type)
	{
		return type.Classification != ObjectClassification.Character && type.Classification != ObjectClassification.Animal &&
			type.Classification != ObjectClassification.Particle && !(type is SonicOrca.Core.Scripting.LuaObjectType { IsAbility: true });
	}

	private string[] GetListKeys()
	{
		return _allTypeKeys.Where(key => FindType(key) is not ObjectType type || IsPlaceable(type)).ToArray();
	}

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		Dictionary<string, int> counts = Placements.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
		EditorListRow newRow = new EditorListRow { Text = "+ New object", Active = _creating };
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
		}).Prepend(newRow).ToArray();
	}

	public override void OnListClick(int index, int direction)
	{
		if (index < NewRowCount)
		{
			_creating = true;
			_placeKey = null;
			Editor.ShowMessage(Hint);
			return;
		}
		string[] keys = GetListKeys();
		index -= NewRowCount;
		if (index < 0 || index >= keys.Length || _loadTask != null)
		{
			return;
		}
		_creating = false;
		string key = keys[index];
		if (string.Equals(key, _placeKey, StringComparison.OrdinalIgnoreCase))
		{
			_placeKey = null;
			return;
		}
		ChooseType(key);
	}

	/// <summary>Starts to add objects of a type. A type that is not loaded loads first.</summary>
	private void ChooseType(string key)
	{
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
		_creating = false;
		if (_placeKey != null)
		{
			PlaceObject(_placeKey, Editor.Snap(Editor.MouseWorld));
			if (!Editor.IsShiftDown)
			{
				_placeKey = null;
			}
			return;
		}
		if (_menu != null)
		{
			// A click in the menu selects its object. A click outside closes the menu.
			int row = GetMenuRowAt(Editor.Mouse);
			if (row >= 0)
			{
				Selection.Clear();
				Selection.Add(_menu[row].Entry.Uid);
			}
			_menu = null;
			return;
		}
		if (TryBeginPathDrag())
		{
			return;
		}
		_pathPoint = null;
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
		ActiveObject[] hits = HitTestAll(Editor.MouseWorld);
		_menuPress = null;
		if (hits.Length > 1 && !Editor.IsShiftDown)
		{
			// Objects overlap here. A selected one can still move, and a click without a move opens the menu (see OnDragEnd).
			ActiveObject selected = hits.FirstOrDefault(o => Selection.Contains(o.Entry.Uid));
			if (selected == null)
			{
				OpenMenu(hits);
				return;
			}
			uid = selected.Entry.Uid;
			_menuPress = Editor.MouseWorld;
		}
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
		if (_pathDrag != null)
		{
			DragPathPoint();
			return;
		}
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
		if (_pathDrag != null)
		{
			EndPathDrag();
			return;
		}
		_moving = false;
		if (_moveDelta == new Vector2i(0, 0))
		{
			if (_menuPress is Vector2 press && !Editor.DragMoved)
			{
				OpenMenu(HitTestAll(press));
			}
			_menuPress = null;
			return;
		}
		_menuPress = null;
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
		if (_menu != null)
		{
			_menu = null;
			return true;
		}
		if (_pathPoint != null)
		{
			_pathPoint = null;
			return true;
		}
		if (_creating)
		{
			_creating = false;
			return true;
		}
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
		LoadReferencedTypes(newPlacement);
		return true;
	}

	/// <summary>
	/// Loads the object types that the values of a placement name, for example the contents of an item block. The
	/// level loads them when it starts, so the editor loads them too.
	/// </summary>
	private void LoadReferencedTypes(ObjectPlacement placement)
	{
		string[] keys = LevelBindingResourceType.GetReferencedObjectTypes(new[] { placement }, Editor.GameContext.ResourceTree)
			.Where(key => FindType(key) == null).ToArray();
		if (keys.Length == 0 || _loadTask != null)
		{
			return;
		}
		_typeSession ??= new ResourceSession(Editor.GameContext.ResourceTree);
		foreach (string key in keys)
		{
			_typeSession.PushDependency(key);
		}
		_loadingKey = null;
		_loadTask = _typeSession.LoadAsync();
		Editor.ShowMessage($"Loading {string.Join(", ", keys.Select(ShortName))}...");
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
		if (_creating)
		{
			return GetNewObjectRows();
		}
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
		if (GetScriptPath(type) is string scriptPath)
		{
			rows.Add(new EditorPropertyRow
			{
				Label = "Script",
				Value = OperatingSystem.IsBrowser() ? Path.GetFileName(Path.GetDirectoryName(scriptPath)) : "click to show the file",
				Step = _ => ShowFile(scriptPath)
			});
		}
		rows.Add(new EditorPropertyRow { Label = "Properties", Value = "", IsHeading = true });
		foreach (EditorProperty property in EditorProperty.For(type))
		{
			object value = property.GetValue(placement);
			EditorPropertyRow row = new EditorPropertyRow { Label = property.Name, Value = EditorProperty.Format(value) };
			if (property.Path != null)
			{
				// The level shows the points of a path. The row keeps the text, so that the user can also type it.
				int count = EditorPath.Parse(value as string).Count;
				row.Tooltip = $"{count} points. Drag a point in the level to move it. Click a + to add a point." +
					(property.Path.Description != null ? " " + property.Path.Description : "");
			}
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
		AddPathPointRows(rows, placement);
		return rows;
	}

	/// <summary>The path of the script of a Lua type in a mod folder, or null.</summary>
	private static string GetScriptPath(ObjectType type)
	{
		return type.Resource?.Identifier == ResourceTypeIdentifier.LuaObject && type.Resource.Source is FileResourceSource file && file.Offset == 0
			? file.Path
			: null;
	}

	private void ShowFile(string path)
	{
		if (!AnimationTextureFiles.ShowInFileManager(path))
		{
			Editor.ShowMessage(path);
		}
	}

	#endregion

	#region New object

	/// <summary>
	/// The mod folder of a new object: the folder of the open level when it is a mod folder, so a level and its
	/// objects stay together. Otherwise the folder <see cref="NewObject.DefaultModName"/>.
	/// </summary>
	private string GetNewObjectFolder()
	{
		string mods = Path.Combine(Editor.GameContext.UserDataDirectory, "mods");
		string[] parts = Editor.Level.Binding.Resource.FullKeyPath.Split('/');
		string mnemonic = parts.Length >= 2 ? parts[parts.Length - 2].ToLowerInvariant() : null;
		if (mnemonic != null && Directory.Exists(Path.Combine(mods, mnemonic, "SONICORCA", "LEVELS")))
		{
			return Path.Combine(mods, mnemonic);
		}
		return Path.Combine(mods, NewObject.DefaultModName);
	}

	/// <summary>The form of a new object: the name, the key, the kind, the image, the folder and Create.</summary>
	private List<EditorPropertyRow> GetNewObjectRows()
	{
		NewObjectKind[] kinds = Enum.GetValues<NewObjectKind>();
		return new List<EditorPropertyRow>
		{
			new EditorPropertyRow
			{
				Label = "Name",
				Value = _newName.Length > 0 ? _newName : "click to type",
				Commit = text =>
				{
					// The key follows the name until the user types a key
					if (_newKey == NewObject.MakeKey(_newName))
					{
						_newKey = NewObject.MakeKey(text);
					}
					_newName = text.Trim();
					return true;
				}
			},
			new EditorPropertyRow
			{
				Label = "Key",
				Value = _newKey.Length > 0 ? ObjectKeyPrefix + _newKey : "capital letters and digits",
				Commit = text =>
				{
					string key = text.Trim().ToUpperInvariant();
					if (key.StartsWith(ObjectKeyPrefix, StringComparison.Ordinal))
					{
						key = key.Substring(ObjectKeyPrefix.Length);
					}
					if (NewObject.CheckKey(key) is string problem)
					{
						Editor.ShowMessage(problem);
						return false;
					}
					_newKey = key;
					return true;
				}
			},
			new EditorPropertyRow
			{
				Label = "Kind",
				Value = NewObject.GetKindName(_newKind),
				Step = direction => _newKind = kinds[((Array.IndexOf(kinds, _newKind) + direction) % kinds.Length + kinds.Length) % kinds.Length]
			},
			new EditorPropertyRow
			{
				Label = "Image",
				Value = _newImagePath ?? "placeholder (type a PNG path)",
				Commit = text =>
				{
					string path = text.Trim().Trim('"');
					if (path.Length == 0)
					{
						_newImagePath = null;
						return true;
					}
					if (NewObject.GetPngSize(File.Exists(path) ? AnimationTextureFiles.TryRead(path) : null) == null)
					{
						Editor.ShowMessage("Not a PNG file: " + path);
						return false;
					}
					_newImagePath = path;
					return true;
				}
			},
			new EditorPropertyRow { Label = "Folder", Value = "mods/" + Path.GetFileName(GetNewObjectFolder()) },
			new EditorPropertyRow { Label = "Create", Value = "click to make and add it", Step = _ => CreateObject() }
		};
	}

	/// <summary>Makes the files of the new object, loads its type, and starts to add objects of it.</summary>
	private void CreateObject()
	{
		if (_loadTask != null)
		{
			return;
		}
		string folder = GetNewObjectFolder();
		try
		{
			byte[] image = _newImagePath == null ? null : AnimationTextureFiles.TryRead(_newImagePath);
			if (_newImagePath != null && image == null)
			{
				throw new InvalidOperationException("The image did not load: " + _newImagePath);
			}
			NewObject.Create(Editor.GameContext.ResourceTree, folder, _newKey, _newName, _newKind, image);
		}
		catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException)
		{
			Editor.ShowMessage(ex.Message);
			return;
		}
		string key = ObjectKeyPrefix + _newKey;
		string ability = _newKind == NewObjectKind.PowerUp ? NewObject.GetAbilityKey(_newKey) : null;
		_creating = false;
		_newName = "";
		_newKey = "";
		_newImagePath = null;
		ReadTypeKeys();
		ChooseType(key);
		Editor.ShowMessage(ability == null
			? $"Made {ShortName(key)} in mods/{Path.GetFileName(folder)}. Click in the level to add it."
			: $"Made {ShortName(key)} and its ability {ability} in mods/{Path.GetFileName(folder)}. Click in the level to add {ShortName(key)}.");
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
		DrawPaths(renderer, g);
		DrawMenu(renderer, g);
	}

	public override string GetCursorLabel(I2dRenderer g)
	{
		if (_placeKey != null)
		{
			Vector2 position = Editor.ToScreen((Vector2)Editor.Snap(Editor.MouseWorld));
			LevelEditor.DrawCross(g, position, 16.0, LevelEditor.SelectionColour, 2.0);
			return "+ " + ShortName(_placeKey);
		}
		if (GetPathHandleAt(Editor.MouseWorld) is PathHandle handle)
		{
			return handle.Insert ? $"{handle.Property.Name}: click to add a point" : DescribePoint(handle.Property, handle.Points, handle.Index);
		}
		if (_menu != null)
		{
			return null;
		}
		ActiveObject[] hits = HitTestAll(Editor.MouseWorld);
		return hits.Length switch
		{
			0 => null,
			1 => ShortName(hits[0].Type.ResourceKey),
			// Objects can overlap, for example a spin tube and its cap. A click shows a menu of them.
			_ => $"{hits.Length} objects here: click to choose"
		};
	}

	#endregion

	#region Menu of overlapping objects

	/// <summary>The objects of the open menu, or null. The menu shows when a click hits objects that overlap.</summary>
	private ActiveObject[] _menu;

	/// <summary>The top left of the menu on the screen.</summary>
	private Vector2 _menuPosition;

	/// <summary>The press position of a click on a selected object where objects overlap, or null. Without a move, the click opens the menu.</summary>
	private Vector2? _menuPress;

	private void OpenMenu(ActiveObject[] objects)
	{
		_menu = objects;
		_menuPosition = Editor.Mouse + new Vector2(8.0, 8.0);
	}

	private string GetMenuText(ActiveObject activeObject)
	{
		string name = Placements.FirstOrDefault(p => p.Uid == activeObject.Entry.Uid)?.Name;
		return ShortName(activeObject.Type.ResourceKey) + (string.IsNullOrEmpty(name) ? "" : ": " + name);
	}

	/// <summary>The area of the menu, kept inside the view of the level.</summary>
	private Rectangle GetMenuArea()
	{
		double width = _menu.Max(o => Editor.MeasureText(GetMenuText(o))) + LevelEditor.Padding * 2.0;
		double height = _menu.Length * LevelEditor.RowHeight;
		double x = Math.Min(_menuPosition.X, LevelEditor.ScreenWidth - LevelEditor.PanelWidth - width);
		double y = Math.Min(_menuPosition.Y, LevelEditor.ScreenHeight - LevelEditor.StatusBarHeight - height);
		return new Rectangle(Math.Max(0.0, x), Math.Max(LevelEditor.TopBarHeight, y), width, height);
	}

	private int GetMenuRowAt(Vector2 screen)
	{
		Rectangle area = GetMenuArea();
		return area.Contains(screen) ? Math.Min(_menu.Length - 1, (int)((screen.Y - area.Y) / LevelEditor.RowHeight)) : -1;
	}

	private void DrawMenu(Renderer renderer, I2dRenderer g)
	{
		if (_menu == null)
		{
			return;
		}
		Rectangle area = GetMenuArea();
		int hovered = GetMenuRowAt(Editor.Mouse);
		// The object of the hovered row shows its box in the level, under the menu
		if (hovered >= 0)
		{
			g.RenderRectangle(LevelEditor.HoverColour, Editor.ToScreen(GetBounds(_menu[hovered])), 3.0);
		}
		g.RenderQuad(LevelEditor.ShadowColour, area.OffsetBy(new Vector2(4.0, 4.0)));
		g.RenderQuad(LevelEditor.PanelColour, area);
		for (int i = 0; i < _menu.Length; i++)
		{
			Rectangle row = new Rectangle(area.X, area.Y + i * LevelEditor.RowHeight, area.Width, LevelEditor.RowHeight);
			bool selected = Selection.Contains(_menu[i].Entry.Uid);
			if (i == hovered || selected)
			{
				g.RenderQuad(i == hovered ? LevelEditor.ButtonHoverColour : LevelEditor.AccentColour, row);
			}
			Editor.DrawText(renderer, GetMenuText(_menu[i]), row.X + LevelEditor.Padding, row.Y + 3.0, LevelEditor.TextColour);
		}
		g.RenderRectangle(LevelEditor.OutlineColour, area, 1.0);
	}

	#endregion

	#region Paths

	/// <summary>The colours of the paths of an object, in the order of its properties.</summary>
	private static readonly Colour[] PathColours =
	{
		new Colour(1.0, 1.0, 0.6, 0.1),
		new Colour(1.0, 0.3, 0.85, 1.0),
		new Colour(1.0, 0.9, 0.4, 1.0),
		new Colour(1.0, 0.4, 1.0, 0.5),
	};

	/// <summary>The distance in screen pixels within which the mouse takes a point.</summary>
	private const double HandleRadius = 9.0;

	/// <summary>A point of a path, or the + in the middle of a segment, which adds a point after <see cref="Index"/>.</summary>
	private sealed record PathHandle(EditorProperty Property, List<EditorPathPoint> Points, int Index, bool Insert);

	/// <summary>The selected point: the path property and the index of the point. The rows of the point show in the properties.</summary>
	private (string Property, int Index)? _pathPoint;

	/// <summary>The points of the path while the user drags one of them, or null.</summary>
	private List<EditorPathPoint> _pathDrag;

	private EditorProperty _pathDragProperty;

	private int _pathDragIndex;

	private bool _pathDragInserted;

	/// <summary>The one selected placement, or null.</summary>
	private ObjectPlacement GetSelectedPlacement()
	{
		ObjectPlacement[] selected = Placements.Where(p => Selection.Contains(p.Uid)).Take(2).ToArray();
		return selected.Length == 1 ? selected[0] : null;
	}

	/// <summary>The path properties of the one selected object, with their points. The points of a dragged path are the dragged points.</summary>
	private IEnumerable<(EditorProperty Property, List<EditorPathPoint> Points)> GetSelectedPaths(out ObjectPlacement placement)
	{
		placement = GetSelectedPlacement();
		ObjectType type = placement == null ? null : FindType(placement.Key);
		if (type == null)
		{
			return Array.Empty<(EditorProperty, List<EditorPathPoint>)>();
		}
		ObjectPlacement selected = placement;
		return EditorProperty.For(type).Where(p => p.Path != null)
			.Select(p => (p, p == _pathDragProperty && _pathDrag != null ? _pathDrag : EditorPath.Parse(p.GetValue(selected) as string)))
			.ToArray();
	}

	/// <summary>The point or the + of the selected object's paths under a level position, or null.</summary>
	private PathHandle GetPathHandleAt(Vector2 world)
	{
		if (_placeKey != null)
		{
			return null;
		}
		double radius = HandleRadius / Editor.Zoom;
		PathHandle best = null;
		double bestDistance = radius;
		// Paths can share points, for example the start. The path of the selected point comes first, then the order of the properties.
		string selectedPath = _pathPoint?.Property;
		foreach ((EditorProperty property, List<EditorPathPoint> points) in GetSelectedPaths(out ObjectPlacement placement).OrderBy(p => p.Property.Name == selectedPath ? 0 : 1))
		{
			Vector2 origin = (Vector2)placement.Position;
			for (int i = 0; i < points.Count; i++)
			{
				double distance = (origin + (Vector2)points[i].Position - world).Length;
				if (distance < bestDistance || (best == null && distance <= bestDistance))
				{
					best = new PathHandle(property, points, i, false);
					bestDistance = distance;
				}
				if (i + 1 < points.Count && IsLongSegment(points[i], points[i + 1]))
				{
					double middle = (origin + GetMiddle(points[i], points[i + 1]) - world).Length;
					if (middle < bestDistance)
					{
						best = new PathHandle(property, points, i, true);
						bestDistance = middle;
					}
				}
			}
		}
		return best;
	}

	private static Vector2 GetMiddle(EditorPathPoint a, EditorPathPoint b) => ((Vector2)a.Position + (Vector2)b.Position) / 2.0;

	/// <summary>A segment shows its + when it is long enough on the screen to keep the + away from the points.</summary>
	private bool IsLongSegment(EditorPathPoint a, EditorPathPoint b) => ((Vector2)(b.Position - a.Position)).Length * Editor.Zoom >= HandleRadius * 5.0;

	/// <summary>Starts to drag a point of a path, or adds a point at a + and drags it. Returns false when no handle is under the mouse.</summary>
	private bool TryBeginPathDrag()
	{
		if (GetPathHandleAt(Editor.MouseWorld) is not PathHandle handle)
		{
			return false;
		}
		_pathDrag = new List<EditorPathPoint>(handle.Points);
		_pathDragProperty = handle.Property;
		_pathDragIndex = handle.Index;
		_pathDragInserted = handle.Insert;
		if (handle.Insert)
		{
			_pathDragIndex = handle.Index + 1;
			Vector2 middle = GetMiddle(handle.Points[handle.Index], handle.Points[handle.Index + 1]);
			_pathDrag.Insert(_pathDragIndex, new EditorPathPoint(new Vector2i((int)Math.Round(middle.X), (int)Math.Round(middle.Y)), false));
		}
		_pathPoint = (handle.Property.Name, _pathDragIndex);
		Editor.BeginToolDrag();
		return true;
	}

	private void DragPathPoint()
	{
		ObjectPlacement placement = GetSelectedPlacement();
		if (placement == null || !Editor.DragMoved)
		{
			return;
		}
		Vector2i position = Editor.Snap(Editor.MouseWorld) - placement.Position;
		_pathDrag[_pathDragIndex] = _pathDrag[_pathDragIndex] with { Position = position };
	}

	private void EndPathDrag()
	{
		List<EditorPathPoint> points = _pathDrag;
		EditorProperty property = _pathDragProperty;
		_pathDrag = null;
		_pathDragProperty = null;
		ObjectPlacement placement = GetSelectedPlacement();
		if (placement != null && (Editor.DragMoved || _pathDragInserted))
		{
			ReplaceWith(placement, property.SetValue(placement, EditorPath.Format(points)));
		}
	}

	/// <summary>Changes the points of a path of the selected object.</summary>
	private bool ChangePath(ObjectPlacement placement, EditorProperty property, Action<List<EditorPathPoint>> change)
	{
		List<EditorPathPoint> points = EditorPath.Parse(property.GetValue(placement) as string);
		change(points);
		return ReplaceWith(placement, property.SetValue(placement, EditorPath.Format(points)));
	}

	private static string DescribePoint(EditorProperty property, List<EditorPathPoint> points, int index)
	{
		string place = index == 0 ? " (start)" : index == points.Count - 1 ? " (end)" : "";
		string mark = points[index].Marked ? $", {property.Path.MarkName ?? "marked"}" : "";
		return $"{property.Name} point {index}{place}{mark}";
	}

	/// <summary>The rows of the selected point of a path: its position, its mark, and a row that removes it.</summary>
	private void AddPathPointRows(List<EditorPropertyRow> rows, ObjectPlacement placement)
	{
		if (_pathPoint is not (string name, int index))
		{
			return;
		}
		EditorProperty property = EditorProperty.For(FindType(placement.Key)).FirstOrDefault(p => p.Name == name && p.Path != null);
		List<EditorPathPoint> points = property == null ? null : EditorPath.Parse(property.GetValue(placement) as string);
		if (points == null || index >= points.Count)
		{
			_pathPoint = null;
			return;
		}
		EditorPathPoint point = points[index];
		rows.Add(new EditorPropertyRow { Label = "Point", Value = DescribePoint(property, points, index), IsHeading = true });
		rows.Add(new EditorPropertyRow
		{
			Label = "Point X, Y",
			Value = $"{point.Position.X}, {point.Position.Y}",
			Tooltip = "The position of the point from the object.",
			Commit = text =>
			{
				string[] xy = text.Split(',');
				return xy.Length == 2 && int.TryParse(xy[0].Trim(), out int x) && int.TryParse(xy[1].Trim(), out int y) &&
					ChangePath(placement, property, p => p[index] = p[index] with { Position = new Vector2i(x, y) });
			}
		});
		if (property.Path.MarkName != null)
		{
			rows.Add(new EditorPropertyRow
			{
				Label = char.ToUpperInvariant(property.Path.MarkName[0]) + property.Path.MarkName.Substring(1),
				Value = point.Marked ? "on" : "off",
				Tooltip = $"The object plays the {property.Path.MarkName} when a character reaches the point.",
				Step = _ => ChangePath(placement, property, p => p[index] = p[index] with { Marked = !p[index].Marked })
			});
		}
		rows.Add(new EditorPropertyRow
		{
			Label = "Remove point",
			Value = points.Count > 2 ? "click to remove it" : "a path needs 2 points",
			Step = points.Count > 2 ? _ =>
			{
				if (ChangePath(placement, property, p => p.RemoveAt(index)))
				{
					_pathPoint = null;
				}
			} : null
		});
	}

	/// <summary>Draws the paths of the selected object on top of the level, with their points and the + of each segment.</summary>
	private void DrawPaths(Renderer renderer, I2dRenderer g)
	{
		int pathIndex = 0;
		foreach ((EditorProperty property, List<EditorPathPoint> points) in GetSelectedPaths(out ObjectPlacement placement))
		{
			Colour colour = PathColours[pathIndex++ % PathColours.Length];
			if (points.Count == 0)
			{
				continue;
			}
			Vector2 origin = (Vector2)placement.Position;
			Vector2 ToScreen(int i) => Editor.ToScreen(origin + (Vector2)points[i].Position);
			for (int i = 0; i + 1 < points.Count; i++)
			{
				g.RenderLine(LevelEditor.ShadowColour, ToScreen(i), ToScreen(i + 1), 5.0);
				g.RenderLine(colour, ToScreen(i), ToScreen(i + 1), 2.0);
				if (IsLongSegment(points[i], points[i + 1]))
				{
					Vector2 middle = Editor.ToScreen(origin + GetMiddle(points[i], points[i + 1]));
					LevelEditor.DrawCross(g, middle, 5.0, colour, 2.0);
				}
			}
			for (int i = 0; i < points.Count; i++)
			{
				Vector2 p = ToScreen(i);
				bool selected = _pathPoint is (string name, int index) && name == property.Name && index == i;
				double size = selected ? 12.0 : 8.0;
				Rectangle box = new Rectangle(p.X - size / 2.0, p.Y - size / 2.0, size, size);
				g.RenderQuad(LevelEditor.ShadowColour, box.Inflate(new Vector2(2.0, 2.0)));
				g.RenderQuad(points[i].Marked ? Colours.White : colour, box);
				if (selected)
				{
					g.RenderRectangle(Colours.White, box.Inflate(new Vector2(3.0, 3.0)), 2.0);
				}
			}
			Vector2 start = ToScreen(0);
			Vector2 end = ToScreen(points.Count - 1);
			// The labels of the paths stack, because paths often share their start
			double offset = (pathIndex - 1) * LevelEditor.RowHeight;
			Editor.DrawLabel(renderer, g, $"{property.Name} start", start.X + 10.0, start.Y - 34.0 - offset, colour);
			Editor.DrawLabel(renderer, g, $"{property.Name} end", end.X + 10.0, end.Y + 6.0 + offset, colour);
		}
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
	private ActiveObject HitTest(Vector2 world) => HitTestAll(world).FirstOrDefault();

	/// <summary>All the objects under a position, the smallest first.</summary>
	private ActiveObject[] HitTestAll(Vector2 world)
	{
		return GetSelectableObjects()
			.Where(o => GetBounds(o).Contains(world))
			.OrderBy(o => GetBounds(o).Width * GetBounds(o).Height)
			.ToArray();
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
