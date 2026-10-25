using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>The way that the player leaves the level editor.</summary>
public enum EditorPlayRequest
{
	None,
	/// <summary>Start the level again at the start position.</summary>
	FromStart,
	/// <summary>Start the level again with the protagonist at <see cref="LevelEditor.PlayPosition"/>.</summary>
	FromCursor
}

/// <summary>
/// Edits the objects and rings of a level with the mouse and the keyboard. The editor pauses the level and shows all
/// the placements of the binding. Each change replaces placements in the binding and makes the object entries
/// again, so a test play after the editor uses the changes. Save writes the binding to the editor file of the zone
/// (see <see cref="EditorModFile"/>).
/// </summary>
public sealed class LevelEditor : IDisposable
{
	private const string FontResourceKey = "SONICORCA/FONTS/SEGOEUI";

	private const double ScreenWidth = 1920.0;

	private const double ScreenHeight = 1080.0;

	private const double TopBarHeight = 48.0;

	private const double StatusBarHeight = 36.0;

	private const double PanelWidth = 460.0;

	private const double RowHeight = 30.0;

	private const double TextScale = 0.5;

	private const double Padding = 10.0;

	/// <summary>The palette uses the top part of the side panel, down to this y value. The properties use the rest.</summary>
	private const double PaletteBottom = 500.0;

	/// <summary>Objects activate in the view and up to this distance around it.</summary>
	private const int LifetimeMargin = 512;

	/// <summary>A drag starts when the mouse moves this number of screen pixels with the button down.</summary>
	private const double DragThreshold = 4.0;

	/// <summary>A message in the status bar shows for this number of frames.</summary>
	private const int MessageFrames = 240;

	private static readonly int[] GridSizes = { 4, 16, 32, 64, 128 };

	private static readonly double[] Zooms = { 1.0, 0.75, 0.5, 0.35, 0.25 };

	private static readonly Colour PanelColour = new Colour(0.96, 0.07, 0.08, 0.11);

	private static readonly Colour MarkerOutlineColour = new Colour(0.8, 0.0, 0.0, 0.0);

	private static readonly Colour ButtonColour = new Colour(1.0, 0.18, 0.2, 0.26);

	private static readonly Colour ButtonHoverColour = new Colour(1.0, 0.27, 0.3, 0.38);

	private static readonly Colour AccentColour = new Colour(1.0, 0.16, 0.44, 0.82);

	private static readonly Colour TextColour = Colours.White;

	private static readonly Colour DimTextColour = new Colour(1.0, 0.62, 0.65, 0.7);

	private static readonly Colour ObjectOutlineColour = new Colour(0.45, 0.4, 0.85, 1.0);

	private static readonly Colour HoverOutlineColour = Colours.White;

	private static readonly Colour SelectionColour = new Colour(1.0, 1.0, 0.8, 0.1);

	private static readonly Colour SelectionFillColour = new Colour(0.18, 1.0, 0.8, 0.1);

	/// <summary>The clipboard is shared by the levels, so the user can copy objects from one zone to another.</summary>
	private static readonly List<ObjectPlacement> Clipboard = new List<ObjectPlacement>();

	private readonly Level _level;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly EditorHistory _history = new EditorHistory();

	private readonly HashSet<Guid> _selection = new HashSet<Guid>();

	private readonly EditorInput _input = new EditorInput();

	private ResourceSession _resourceSession;

	private Font _font;

	private int _frames;

	private int _gridIndex = 1;

	private int _zoomIndex;

	private string _placeKey;

	private double _paletteScroll;

	private double _propertyScroll;

	private string _message;

	private int _messageFrames;

	/// <summary>The property row that the user types a value for, or null.</summary>
	private string _editingRow;

	private string _editText = "";

	/// <summary>The first key that the user types replaces the old value.</summary>
	private bool _replaceEditText;

	private Vector2 _mouse;

	private Vector2 _mouseWorld;

	private DragMode _drag;

	private Vector2 _dragStartMouse;

	private Vector2 _dragStartWorld;

	private Vector2 _dragStartCamera;

	private bool _dragMoved;

	private Vector2i _moveDelta;

	private Guid _moveAnchor;

	private readonly Dictionary<Guid, Vector2i> _moveStartPositions = new Dictionary<Guid, Vector2i>();

	private enum DragMode
	{
		None,
		Pan,
		Move,
		Box
	}

	/// <summary>There are changes that are not saved.</summary>
	public bool Dirty { get; private set; }

	public EditorPlayRequest PlayRequest { get; private set; }

	/// <summary>The position for <see cref="EditorPlayRequest.FromCursor"/>.</summary>
	public Vector2i PlayPosition { get; private set; }

	public IReadOnlyCollection<Guid> Selection => _selection;

	private Camera Camera => _level.Camera;

	private IList<ObjectPlacement> Placements => _level.Binding.ObjectPlacements;

	private int GridSize => GridSizes[_gridIndex];

	private double Zoom => Zooms[_zoomIndex];

	private bool HasFont => _font != null;

	public LevelEditor(Level level)
	{
		_level = level;
		_gameContext = level.GameContext;
	}

	public async Task LoadAsync(CancellationToken ct = default)
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependency(FontResourceKey);
		await _resourceSession.LoadAsync(ct);
		_font = resourceTree.GetLoadedResource<Font>(FontResourceKey);
	}

	public void Dispose()
	{
		_resourceSession?.Dispose();
		_resourceSession = null;
		_font = null;
	}

	/// <summary>Opens the editor on the level as it is now. The view stays where the camera is.</summary>
	public void Begin()
	{
		PlayRequest = EditorPlayRequest.None;
		_input.Begin(_gameContext);
		_frames = 0;
		_drag = DragMode.None;
		_editingRow = null;
		_placeKey = null;
		GiveUniqueIds();
		SetZoom(_zoomIndex, Camera.Bounds.Centre);
		Rebind();
		ShowMessage("Editor. F5: play here. F2: play from the start.");
	}

	public void Update()
	{
		_frames++;
		if (_messageFrames > 0)
		{
			_messageFrames--;
		}
		ReadMouse();
		if (!_gameContext.Console.IsOpen)
		{
			if (_editingRow != null)
			{
				UpdateTextEntry();
			}
			else
			{
				UpdateKeyboard();
			}
		}
		UpdateMouse();
		ClampCamera();
		_level.TileSet.Animate();
		_level.Map.Animate();
		_level.ObjectManager.ResetLifetimeArea();
		_level.ObjectManager.AddLifeArea(((Rectanglei)Camera.Bounds).Inflate(new Vector2i(LifetimeMargin, LifetimeMargin)));
		_level.ObjectManager.UpdateEditor();
	}

	#region Input

	private void ReadMouse()
	{
		_input.Read(_gameContext);
		_mouse = _input.Mouse;
		_mouseWorld = ToWorld(_mouse);
	}

	private bool IsKeyDown(int key) => _input.IsKeyDown(key);

	private bool IsKeyPressed(int key) => _input.IsKeyPressed(key);

	private bool IsControlDown => IsKeyDown(KeyboardState.KEY_LCTRL) || IsKeyDown(KeyboardState.KEY_RCTRL) ||
		IsKeyDown(KeyboardState.KEY_LGUI) || IsKeyDown(KeyboardState.KEY_RGUI);

	private bool IsShiftDown => IsKeyDown(KeyboardState.KEY_LSHIFT) || IsKeyDown(KeyboardState.KEY_RSHIFT);

	private void UpdateKeyboard()
	{
		bool control = IsControlDown;
		bool shift = IsShiftDown;
		if (IsKeyPressed(KeyboardState.KEY_F5))
		{
			RequestPlay(shift ? EditorPlayRequest.FromStart : EditorPlayRequest.FromCursor);
		}
		else if (IsKeyPressed(KeyboardState.KEY_F2))
		{
			RequestPlay(EditorPlayRequest.FromStart);
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_S))
		{
			Save();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_Z))
		{
			if (shift)
			{
				Redo();
			}
			else
			{
				Undo();
			}
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_Y))
		{
			Redo();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_C))
		{
			Copy();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_X))
		{
			Copy();
			DeleteSelection();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_V))
		{
			Paste(IsOverLevel(_mouse) ? _mouseWorld : Camera.Bounds.Centre);
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_D))
		{
			Duplicate();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_A))
		{
			SelectAllInView();
		}
		else if (IsKeyPressed(KeyboardState.KEY_DELETE) || IsKeyPressed(KeyboardState.KEY_BACKSPACE))
		{
			DeleteSelection();
		}
		else if (IsKeyPressed(KeyboardState.KEY_ESCAPE))
		{
			if (_placeKey != null)
			{
				_placeKey = null;
			}
			else
			{
				_selection.Clear();
			}
		}
		else if (IsKeyPressed(KeyboardState.KEY_G))
		{
			_gridIndex = (_gridIndex + (shift ? GridSizes.Length - 1 : 1)) % GridSizes.Length;
			ShowMessage($"Grid: {GridSize} ({GridSize / 4} px in the original)");
		}
		else if (IsKeyPressed(KeyboardState.KEY_MINUS))
		{
			SetZoom(_zoomIndex + 1, Camera.Bounds.Centre);
		}
		else if (IsKeyPressed(KeyboardState.KEY_EQUALS))
		{
			SetZoom(_zoomIndex - 1, Camera.Bounds.Centre);
		}
		else if (IsKeyPressed(KeyboardState.KEY_HOME))
		{
			CentreOn(_level.StartPosition);
		}
		UpdateArrowKeys(shift);
	}

	/// <summary>The arrow keys move the selection by the grid size. Without a selection, they move the view.</summary>
	private void UpdateArrowKeys(bool shift)
	{
		int x = (IsKeyDown(KeyboardState.KEY_RIGHT) ? 1 : 0) - (IsKeyDown(KeyboardState.KEY_LEFT) ? 1 : 0);
		int y = (IsKeyDown(KeyboardState.KEY_DOWN) ? 1 : 0) - (IsKeyDown(KeyboardState.KEY_UP) ? 1 : 0);
		if (_selection.Count > 0)
		{
			int pressedX = (IsKeyPressed(KeyboardState.KEY_RIGHT) ? 1 : 0) - (IsKeyPressed(KeyboardState.KEY_LEFT) ? 1 : 0);
			int pressedY = (IsKeyPressed(KeyboardState.KEY_DOWN) ? 1 : 0) - (IsKeyPressed(KeyboardState.KEY_UP) ? 1 : 0);
			if (pressedX != 0 || pressedY != 0)
			{
				int step = GridSize * (shift ? 4 : 1);
				MoveSelection(new Vector2i(pressedX * step, pressedY * step));
			}
		}
		else if (x != 0 || y != 0)
		{
			double speed = (shift ? 64.0 : 16.0) / Zoom;
			Rectangle bounds = Camera.Bounds;
			Camera.Bounds = new Rectangle(bounds.X + x * speed, bounds.Y + y * speed, bounds.Width, bounds.Height);
		}
	}

	private void UpdateTextEntry()
	{
		string text = _input.TextInput;
		if (!string.IsNullOrEmpty(text))
		{
			_editText = _replaceEditText ? text : _editText + text;
			_replaceEditText = false;
		}
		if (IsKeyPressed(KeyboardState.KEY_BACKSPACE) && _editText.Length > 0)
		{
			_editText = _replaceEditText ? "" : _editText.Substring(0, _editText.Length - 1);
			_replaceEditText = false;
		}
		if (IsKeyPressed(KeyboardState.KEY_RETURN) || IsKeyPressed(KeyboardState.KEY_KP_ENTER))
		{
			CommitTextEntry();
		}
		else if (IsKeyPressed(KeyboardState.KEY_ESCAPE))
		{
			_editingRow = null;
		}
	}

	private void CommitTextEntry()
	{
		PropertyRow row = GetPropertyRows().FirstOrDefault(r => r.Label == _editingRow);
		_editingRow = null;
		if (row?.Commit != null && !row.Commit(_editText))
		{
			ShowMessage("Value not accepted: " + _editText);
		}
	}

	private void UpdateMouse()
	{
		if (_drag != DragMode.None)
		{
			UpdateDrag();
			return;
		}
		if (_input.Wheel != 0.0)
		{
			UpdateWheel(_input.Wheel);
		}
		if (_input.LeftPressed || _input.RightPressed)
		{
			if (_editingRow != null && !IsOverPropertyRow(_editingRow))
			{
				CommitTextEntry();
			}
			if (IsOverTopBar(_mouse))
			{
				if (_input.LeftPressed)
				{
					GetTopBarButtons().FirstOrDefault(b => b.Bounds.Contains(_mouse) && b.Enabled)?.Action();
				}
			}
			else if (IsOverPanel(_mouse))
			{
				OnPanelClick(_input.LeftPressed ? 1 : -1);
			}
			else if (IsOverLevel(_mouse))
			{
				OnLevelPress(_input.LeftPressed);
			}
		}
		else if (_input.MiddlePressed && IsOverLevel(_mouse))
		{
			BeginDrag(DragMode.Pan);
		}
	}

	private void UpdateWheel(double wheel)
	{
		if (IsOverPanel(_mouse))
		{
			if (_mouse.Y < PaletteBottom)
			{
				_paletteScroll = Math.Max(0.0, _paletteScroll - wheel * RowHeight);
			}
			else
			{
				_propertyScroll = Math.Max(0.0, _propertyScroll - wheel * RowHeight);
			}
		}
		else if (IsOverLevel(_mouse))
		{
			if (IsControlDown)
			{
				SetZoom(_zoomIndex - Math.Sign(wheel), _mouseWorld, _mouse);
			}
			else
			{
				double distance = wheel * 96.0 / Zoom;
				Rectangle bounds = Camera.Bounds;
				Camera.Bounds = IsShiftDown
					? new Rectangle(bounds.X - distance, bounds.Y, bounds.Width, bounds.Height)
					: new Rectangle(bounds.X, bounds.Y - distance, bounds.Width, bounds.Height);
			}
		}
	}

	private void OnLevelPress(bool left)
	{
		if (!left || IsKeyDown(KeyboardState.KEY_SPACE))
		{
			BeginDrag(DragMode.Pan);
			return;
		}
		if (_placeKey != null)
		{
			PlaceObject(_placeKey, Snap(_mouseWorld));
			if (!IsShiftDown)
			{
				_placeKey = null;
			}
			return;
		}
		ActiveObject hit = HitTest(_mouseWorld);
		if (hit == null)
		{
			if (!IsShiftDown)
			{
				_selection.Clear();
			}
			BeginDrag(DragMode.Box);
			return;
		}
		Guid uid = hit.Entry.Uid;
		if (IsShiftDown && _selection.Contains(uid))
		{
			_selection.Remove(uid);
			return;
		}
		if (!_selection.Contains(uid))
		{
			if (!IsShiftDown)
			{
				_selection.Clear();
			}
			_selection.Add(uid);
		}
		_moveAnchor = uid;
		BeginDrag(DragMode.Move);
	}

	private void BeginDrag(DragMode mode)
	{
		_drag = mode;
		_dragStartMouse = _mouse;
		_dragStartWorld = _mouseWorld;
		_dragStartCamera = new Vector2(Camera.Bounds.X, Camera.Bounds.Y);
		_dragMoved = false;
		_moveDelta = new Vector2i(0, 0);
		_moveStartPositions.Clear();
		if (mode == DragMode.Move)
		{
			foreach (ObjectPlacement placement in Placements.Where(p => _selection.Contains(p.Uid)))
			{
				_moveStartPositions[placement.Uid] = placement.Position;
			}
		}
	}

	private void UpdateDrag()
	{
		if ((_mouse - _dragStartMouse).Length >= DragThreshold)
		{
			_dragMoved = true;
		}
		bool buttonDown = _input.Left || _input.Right || _input.Middle;
		switch (_drag)
		{
			case DragMode.Pan:
			{
				Vector2 offset = (_mouse - _dragStartMouse) * (1.0 / Zoom);
				Rectangle bounds = Camera.Bounds;
				Camera.Bounds = new Rectangle(_dragStartCamera.X - offset.X, _dragStartCamera.Y - offset.Y, bounds.Width, bounds.Height);
				break;
			}
			case DragMode.Move:
				if (_dragMoved && _moveStartPositions.TryGetValue(_moveAnchor, out Vector2i anchorStart))
				{
					Vector2 target = (Vector2)anchorStart + (_mouseWorld - _dragStartWorld);
					_moveDelta = Snap(target) - anchorStart;
					MoveActiveObjects(_moveDelta);
				}
				break;
		}
		if (buttonDown && !_input.AnyReleased)
		{
			return;
		}
		DragMode mode = _drag;
		_drag = DragMode.None;
		if (mode == DragMode.Move && _moveDelta != new Vector2i(0, 0))
		{
			_history.Record(Placements, _selection);
			ReplacePlacements(p => _moveStartPositions.TryGetValue(p.Uid, out Vector2i start) ? p.WithPosition(start + _moveDelta) : p);
		}
		else if (mode == DragMode.Box && _dragMoved)
		{
			Rectangle box = GetDragBox();
			foreach (ActiveObject activeObject in GetSelectableObjects().Where(o => GetBounds(o).IntersectsWith(box)))
			{
				_selection.Add(activeObject.Entry.Uid);
			}
		}
	}

	/// <summary>Moves the active objects of the selection while the user drags them. The placements change at the end of the drag.</summary>
	private void MoveActiveObjects(Vector2i delta)
	{
		foreach (ActiveObject activeObject in GetSelectableObjects())
		{
			if (_moveStartPositions.TryGetValue(activeObject.Entry.Uid, out Vector2i start))
			{
				activeObject.Position = start + delta;
			}
		}
	}

	private Rectangle GetDragBox()
	{
		return Rectangle.FromLTRB(
			Math.Min(_dragStartWorld.X, _mouseWorld.X), Math.Min(_dragStartWorld.Y, _mouseWorld.Y),
			Math.Max(_dragStartWorld.X, _mouseWorld.X), Math.Max(_dragStartWorld.Y, _mouseWorld.Y));
	}

	#endregion

	#region Commands

	private void RequestPlay(EditorPlayRequest request)
	{
		if (_editingRow != null)
		{
			CommitTextEntry();
		}
		PlayPosition = IsOverLevel(_mouse) ? Snap(_mouseWorld) : (Vector2i)Camera.Bounds.Centre;
		PlayRequest = request;
	}

	public void Save()
	{
		try
		{
			string path = EditorModFile.Save(_level.Binding, _gameContext.UserDataDirectory);
			Dirty = false;
			ShowMessage("Saved to " + path);
		}
		catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is ResourceException)
		{
			ShowMessage("Save failed: " + ex.Message);
		}
	}

	public void Undo()
	{
		if (_history.Undo(Placements, _selection))
		{
			AfterHistoryChange();
		}
	}

	public void Redo()
	{
		if (_history.Redo(Placements, _selection))
		{
			AfterHistoryChange();
		}
	}

	private void AfterHistoryChange()
	{
		Dirty = true;
		_selection.IntersectWith(Placements.Select(p => p.Uid));
		Rebind();
	}

	/// <summary>Adds an object of a type at a position, on the layer that most objects use.</summary>
	public ObjectPlacement PlaceObject(string key, Vector2i position)
	{
		int count = Placements.Count(p => p.Key == key);
		ObjectType type = FindType(key);
		string name = $"{type?.Name ?? ShortName(key)} {count}";
		ObjectPlacement placement = new ObjectPlacement(key, Guid.NewGuid(), name, GetDefaultLayer(), position, new { });
		_history.Record(Placements, _selection);
		Placements.Add(placement);
		_selection.Clear();
		_selection.Add(placement.Uid);
		Dirty = true;
		Rebind();
		return placement;
	}

	public void DeleteSelection()
	{
		if (_selection.Count == 0)
		{
			return;
		}
		_history.Record(Placements, _selection);
		foreach (ObjectPlacement placement in Placements.Where(p => _selection.Contains(p.Uid)).ToArray())
		{
			Placements.Remove(placement);
		}
		_selection.Clear();
		Dirty = true;
		Rebind();
	}

	public void MoveSelection(Vector2i delta)
	{
		if (_selection.Count == 0)
		{
			return;
		}
		_history.Record(Placements, _selection);
		ReplacePlacements(p => _selection.Contains(p.Uid) ? p.WithPosition(p.Position + delta) : p);
	}

	private void Copy()
	{
		if (_selection.Count == 0)
		{
			return;
		}
		Clipboard.Clear();
		Clipboard.AddRange(Placements.Where(p => _selection.Contains(p.Uid)));
		ShowMessage($"Copied {Clipboard.Count} objects.");
	}

	private void Duplicate()
	{
		if (_selection.Count == 0)
		{
			return;
		}
		ObjectPlacement[] selected = Placements.Where(p => _selection.Contains(p.Uid)).ToArray();
		AddCopies(selected, new Vector2i(GridSize * 2, GridSize * 2));
	}

	/// <summary>Adds copies of the clipboard objects, with the centre of the group at a position.</summary>
	private void Paste(Vector2 position)
	{
		ObjectPlacement[] available = Clipboard.Where(p => FindType(p.Key) != null).ToArray();
		if (available.Length == 0)
		{
			if (Clipboard.Count > 0)
			{
				ShowMessage("This level cannot use the object types of the clipboard.");
			}
			return;
		}
		Vector2 centre = new Vector2(available.Average(p => (double)p.Position.X), available.Average(p => (double)p.Position.Y));
		AddCopies(available, Snap(position) - Snap(centre));
	}

	/// <summary>
	/// Adds copies of placements with new IDs. A mapping to an object that is also copied refers to the copy of that
	/// object.
	/// </summary>
	private void AddCopies(IReadOnlyList<ObjectPlacement> sources, Vector2i offset)
	{
		Dictionary<Guid, Guid> newIds = sources.ToDictionary(p => p.Uid, _ => Guid.NewGuid());
		_history.Record(Placements, _selection);
		_selection.Clear();
		foreach (ObjectPlacement source in sources)
		{
			ObjectPlacement copy = source.With(newIds[source.Uid], source.Position + offset, source.Layer, source.Behaviour);
			copy.Mappings = source.Mappings
				.Select(m => Guid.TryParse(Convert.ToString(m.Value), out Guid target) && newIds.TryGetValue(target, out Guid newTarget)
					? new KeyValuePair<string, object>(m.Key, newTarget.ToString())
					: m)
				.ToList();
			Placements.Add(copy);
			_selection.Add(copy.Uid);
		}
		Dirty = true;
		Rebind();
	}

	private void SelectAllInView()
	{
		Rectangle view = Camera.Bounds;
		foreach (ActiveObject activeObject in GetSelectableObjects().Where(o => view.IntersectsWith(GetBounds(o))))
		{
			_selection.Add(activeObject.Entry.Uid);
		}
	}

	private void ReplacePlacements(Func<ObjectPlacement, ObjectPlacement> change)
	{
		for (int i = 0; i < Placements.Count; i++)
		{
			Placements[i] = change(Placements[i]);
		}
		Dirty = true;
		Rebind();
	}

	private void ReplacePlacement(ObjectPlacement oldPlacement, ObjectPlacement newPlacement)
	{
		int index = Placements.IndexOf(oldPlacement);
		if (index < 0)
		{
			return;
		}
		_history.Record(Placements, _selection);
		Placements[index] = newPlacement;
		Dirty = true;
		Rebind();
	}

	/// <summary>Makes the object entries again from the placements, so that the level shows the placements as they are.</summary>
	private void Rebind()
	{
		_level.ObjectManager.Rebind(_level.Binding);
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

	private void ShowMessage(string message)
	{
		_message = message;
		_messageFrames = MessageFrames;
	}

	#endregion

	#region View

	private void SetZoom(int index, Vector2 focusWorld) => SetZoom(index, focusWorld, new Vector2(ScreenWidth / 2.0, ScreenHeight / 2.0));

	/// <summary>Changes the zoom, and keeps the world position under a point of the screen.</summary>
	private void SetZoom(int index, Vector2 focusWorld, Vector2 focusScreen)
	{
		_zoomIndex = Math.Clamp(index, 0, Zooms.Length - 1);
		double width = ScreenWidth / Zoom;
		double height = ScreenHeight / Zoom;
		Camera.Bounds = new Rectangle(focusWorld.X - focusScreen.X / Zoom, focusWorld.Y - focusScreen.Y / Zoom, width, height);
	}

	private void CentreOn(Vector2i position)
	{
		Rectangle bounds = Camera.Bounds;
		Camera.Bounds = new Rectangle(position.X - bounds.Width / 2.0, position.Y - bounds.Height / 2.0, bounds.Width, bounds.Height);
	}

	/// <summary>Keeps the view in the map, with a margin, so that the user cannot lose the level.</summary>
	private void ClampCamera()
	{
		Rectangle bounds = Camera.Bounds;
		Vector2i size = GetMapSize();
		double margin = 1024.0;
		double x = Math.Clamp(bounds.X, -margin, Math.Max(-margin, size.X + margin - bounds.Width));
		double y = Math.Clamp(bounds.Y, -margin, Math.Max(-margin, size.Y + margin - bounds.Height));
		Camera.Bounds = new Rectangle(x, y, bounds.Width, bounds.Height);
	}

	private Vector2i GetMapSize()
	{
		int width = 0;
		int height = 0;
		foreach (LevelLayer layer in _level.Map.Layers)
		{
			width = Math.Max(width, layer.Columns * 64);
			height = Math.Max(height, layer.Rows * 64);
		}
		return new Vector2i(width, height);
	}

	private Vector2 ToWorld(Vector2 screen)
	{
		Rectangle bounds = Camera.Bounds;
		return new Vector2(bounds.X + screen.X / Zoom, bounds.Y + screen.Y / Zoom);
	}

	private Vector2 ToScreen(Vector2 world)
	{
		Rectangle bounds = Camera.Bounds;
		return new Vector2((world.X - bounds.X) * Zoom, (world.Y - bounds.Y) * Zoom);
	}

	private Rectangle ToScreen(Rectangle world)
	{
		Vector2 topLeft = ToScreen(new Vector2(world.X, world.Y));
		return new Rectangle(topLeft.X, topLeft.Y, world.Width * Zoom, world.Height * Zoom);
	}

	private Vector2i Snap(Vector2 position)
	{
		int grid = GridSize;
		return new Vector2i((int)Math.Round(position.X / grid) * grid, (int)Math.Round(position.Y / grid) * grid);
	}

	private static bool IsOverTopBar(Vector2 screen) => screen.Y < TopBarHeight;

	private static bool IsOverPanel(Vector2 screen) => !IsOverTopBar(screen) && screen.X >= ScreenWidth - PanelWidth;

	private static bool IsOverStatusBar(Vector2 screen) => screen.Y >= ScreenHeight - StatusBarHeight;

	private static bool IsOverLevel(Vector2 screen) => !IsOverTopBar(screen) && !IsOverPanel(screen) && !IsOverStatusBar(screen);

	#endregion

	#region Objects

	/// <summary>The active objects that a placement made. Sub-objects, characters and particles are not selectable.</summary>
	private IEnumerable<ActiveObject> GetSelectableObjects()
	{
		return _level.ObjectManager.ActiveObjects.Where(o => !o.IsSubObject && o.Entry != null && o.Entry.Active == o);
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
		return _level.ObjectManager.RegisteredTypes.FirstOrDefault(t => t.Resource != null && string.Equals(t.ResourceKey, key, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>The object types that the user can place: the loaded types that are not characters, animals or particles.</summary>
	private IReadOnlyList<ObjectType> GetPaletteTypes()
	{
		return _level.ObjectManager.RegisteredTypes
			.Where(t => t.Resource != null && t.Classification != ObjectClassification.Character &&
				t.Classification != ObjectClassification.Animal && t.Classification != ObjectClassification.Particle)
			.OrderBy(t => ShortName(t.ResourceKey), StringComparer.Ordinal)
			.ToArray();
	}

	private int GetDefaultLayer()
	{
		return Placements.GroupBy(p => p.Layer).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
	}

	private static string ShortName(string key)
	{
		const string prefix = "SONICORCA/OBJECTS/";
		return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? key.Substring(prefix.Length) : key;
	}

	#endregion

	#region Panels

	private sealed class Button
	{
		public Rectangle Bounds;

		public string Text;

		public bool Enabled = true;

		public bool Active;

		public Action Action;
	}

	/// <summary>A row of the properties panel. A row with <see cref="Commit"/> takes a typed value. A row with <see cref="Step"/> changes on a click.</summary>
	private sealed class PropertyRow
	{
		public string Label;

		public string Value;

		public Func<string, bool> Commit;

		public Action<int> Step;

		public bool IsHeading;
	}

	private List<Button> GetTopBarButtons()
	{
		List<Button> buttons = new List<Button>();
		double x = Padding;
		void Add(string text, Action action, bool enabled = true, bool active = false)
		{
			double width = MeasureText(text) + Padding * 2.0;
			buttons.Add(new Button
			{
				Bounds = new Rectangle(x, 6.0, width, TopBarHeight - 12.0),
				Text = text,
				Enabled = enabled,
				Active = active,
				Action = action
			});
			x += width + 6.0;
		}
		Add("Play here (F5)", () => RequestPlay(EditorPlayRequest.FromCursor));
		Add("Play from start (F2)", () => RequestPlay(EditorPlayRequest.FromStart));
		Add("Save (Ctrl+S)", Save, active: Dirty);
		Add("Undo", Undo, _history.CanUndo);
		Add("Redo", Redo, _history.CanRedo);
		Add("Copy", Copy, _selection.Count > 0);
		Add("Paste", () => Paste(Camera.Bounds.Centre), Clipboard.Count > 0);
		Add("Delete", DeleteSelection, _selection.Count > 0);
		Add($"Grid {GridSize} (G)", () => _gridIndex = (_gridIndex + 1) % GridSizes.Length);
		Add($"Zoom {Zoom * 100:0}%", () => SetZoom((_zoomIndex + 1) % Zooms.Length, Camera.Bounds.Centre));
		return buttons;
	}

	private void OnPanelClick(int direction)
	{
		if (_mouse.Y < PaletteBottom)
		{
			IReadOnlyList<ObjectType> types = GetPaletteTypes();
			int index = (int)Math.Floor((_mouse.Y - (TopBarHeight + RowHeight) + _paletteScroll) / RowHeight);
			if (_mouse.Y >= TopBarHeight + RowHeight && index >= 0 && index < types.Count)
			{
				string key = types[index].ResourceKey;
				_placeKey = _placeKey == key ? null : key;
				if (_placeKey != null)
				{
					ShowMessage($"Click in the level to place {ShortName(key)}. Hold Shift to place more. Esc to stop.");
				}
			}
			return;
		}
		List<PropertyRow> rows = GetPropertyRows();
		int rowIndex = GetPropertyRowIndexAt(_mouse.Y);
		if (rowIndex < 0 || rowIndex >= rows.Count)
		{
			return;
		}
		PropertyRow row = rows[rowIndex];
		if (row.Step != null)
		{
			row.Step(direction);
		}
		else if (row.Commit != null)
		{
			_editingRow = row.Label;
			_editText = row.Value;
			_replaceEditText = true;
		}
	}

	private int GetPropertyRowIndexAt(double y)
	{
		double top = PaletteBottom + RowHeight;
		if (y < top)
		{
			return -1;
		}
		return (int)Math.Floor((y - top + _propertyScroll) / RowHeight);
	}

	private bool IsOverPropertyRow(string label)
	{
		if (!IsOverPanel(_mouse) || _mouse.Y < PaletteBottom)
		{
			return false;
		}
		List<PropertyRow> rows = GetPropertyRows();
		int index = GetPropertyRowIndexAt(_mouse.Y);
		return index >= 0 && index < rows.Count && rows[index].Label == label;
	}

	/// <summary>The rows of the properties panel for the selection.</summary>
	private List<PropertyRow> GetPropertyRows()
	{
		List<PropertyRow> rows = new List<PropertyRow>();
		ObjectPlacement[] selected = Placements.Where(p => _selection.Contains(p.Uid)).ToArray();
		if (selected.Length != 1)
		{
			if (selected.Length > 1)
			{
				rows.Add(new PropertyRow { Label = "Selected", Value = $"{selected.Length} objects", IsHeading = true });
				foreach (IGrouping<string, ObjectPlacement> group in selected.GroupBy(p => p.Key).OrderBy(g => g.Key))
				{
					rows.Add(new PropertyRow { Label = ShortName(group.Key), Value = group.Count().ToString() });
				}
			}
			return rows;
		}
		ObjectPlacement placement = selected[0];
		rows.Add(new PropertyRow { Label = "Type", Value = ShortName(placement.Key), IsHeading = true });
		rows.Add(new PropertyRow
		{
			Label = "Name",
			Value = placement.Name ?? "",
			Commit = text => ReplaceWith(placement, placement.With(placement.Uid, placement.Position, placement.Layer, placement.Behaviour, text))
		});
		rows.Add(new PropertyRow
		{
			Label = "X",
			Value = placement.Position.X.ToString(),
			Commit = text => int.TryParse(text, out int x) && ReplaceWith(placement, placement.WithPosition(new Vector2i(x, placement.Position.Y)))
		});
		rows.Add(new PropertyRow
		{
			Label = "Y",
			Value = placement.Position.Y.ToString(),
			Commit = text => int.TryParse(text, out int y) && ReplaceWith(placement, placement.WithPosition(new Vector2i(placement.Position.X, y)))
		});
		IList<LevelLayer> layers = _level.Map.Layers;
		rows.Add(new PropertyRow
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
		rows.Add(new PropertyRow { Label = "Properties", Value = "", IsHeading = true });
		foreach (EditorProperty property in EditorProperty.For(type))
		{
			object value = property.GetValue(placement);
			PropertyRow row = new PropertyRow { Label = property.Name, Value = EditorProperty.Format(value) };
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

	private bool ReplaceWith(ObjectPlacement oldPlacement, ObjectPlacement newPlacement)
	{
		ReplacePlacement(oldPlacement, newPlacement);
		return true;
	}

	#endregion

	#region Drawing

	public void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		// The level renderer leaves the scale of the zoom in the model matrix
		g.ModelMatrix = Matrix4.Identity;
		DrawObjectOverlays(g);
		DrawStartPosition(renderer, g);
		if (_drag == DragMode.Box && _dragMoved)
		{
			Rectangle box = ToScreen(GetDragBox());
			g.RenderQuad(SelectionFillColour, box);
			g.RenderRectangle(SelectionColour, box, 2.0);
		}
		if (HasFont)
		{
			DrawCursorLabel(renderer, g);
			DrawTopBar(renderer, g);
			DrawPanel(renderer, g);
			DrawStatusBar(renderer, g);
		}
		renderer.DeativateRenderer();
	}

	private void DrawObjectOverlays(I2dRenderer g)
	{
		ActiveObject hover = _drag == DragMode.None && IsOverLevel(_mouse) && _placeKey == null ? HitTest(_mouseWorld) : null;
		foreach (ActiveObject activeObject in GetSelectableObjects())
		{
			Rectangle bounds = ToScreen(GetBounds(activeObject));
			if (_selection.Contains(activeObject.Entry.Uid))
			{
				g.RenderQuad(SelectionFillColour, bounds);
				g.RenderRectangle(SelectionColour, bounds, 3.0);
			}
			else if (activeObject == hover)
			{
				g.RenderRectangle(HoverOutlineColour, bounds, 2.0);
			}
			else
			{
				g.RenderRectangle(ObjectOutlineColour, bounds, 1.0);
			}
		}
	}

	private void DrawStartPosition(Renderer renderer, I2dRenderer g)
	{
		Vector2 start = ToScreen((Vector2)_level.StartPosition);
		DrawCross(g, start, 24.0, MarkerOutlineColour, 7.0);
		DrawCross(g, start, 24.0, Colours.White, 3.0);
		if (HasFont)
		{
			const string label = "START";
			Rectangle box = new Rectangle(start.X + 10.0, start.Y - 40.0, MeasureText(label) + Padding, RowHeight - 4.0);
			g.RenderQuad(PanelColour, box);
			DrawText(renderer, label, box.X + Padding / 2.0, box.Y + 2.0, TextColour);
		}
	}

	private static void DrawCross(I2dRenderer g, Vector2 centre, double size, Colour colour, double thickness)
	{
		g.RenderLine(colour, new Vector2(centre.X - size, centre.Y), new Vector2(centre.X + size, centre.Y), thickness);
		g.RenderLine(colour, new Vector2(centre.X, centre.Y - size), new Vector2(centre.X, centre.Y + size), thickness);
	}

	/// <summary>Shows the type of the object under the mouse, or the type that a click places.</summary>
	private void DrawCursorLabel(Renderer renderer, I2dRenderer g)
	{
		if (!IsOverLevel(_mouse) || _drag != DragMode.None)
		{
			return;
		}
		string label;
		if (_placeKey != null)
		{
			Vector2 position = ToScreen((Vector2)Snap(_mouseWorld));
			g.RenderLine(SelectionColour, new Vector2(position.X - 16.0, position.Y), new Vector2(position.X + 16.0, position.Y), 2.0);
			g.RenderLine(SelectionColour, new Vector2(position.X, position.Y - 16.0), new Vector2(position.X, position.Y + 16.0), 2.0);
			label = "+ " + ShortName(_placeKey);
		}
		else
		{
			ActiveObject hover = HitTest(_mouseWorld);
			if (hover == null)
			{
				return;
			}
			label = ShortName(hover.Type.ResourceKey);
		}
		double width = MeasureText(label) + Padding;
		Rectangle box = new Rectangle(_mouse.X + 18.0, _mouse.Y + 18.0, width, RowHeight - 4.0);
		g.RenderQuad(PanelColour, box);
		DrawText(renderer, label, box.X + Padding / 2.0, box.Y + 2.0, TextColour);
	}

	private void DrawTopBar(Renderer renderer, I2dRenderer g)
	{
		g.RenderQuad(PanelColour, new Rectangle(0.0, 0.0, ScreenWidth, TopBarHeight));
		foreach (Button button in GetTopBarButtons())
		{
			bool hover = button.Enabled && button.Bounds.Contains(_mouse);
			g.RenderQuad(button.Active ? AccentColour : (hover ? ButtonHoverColour : ButtonColour), button.Bounds);
			DrawText(renderer, button.Text, button.Bounds.X + Padding, button.Bounds.Y + (button.Bounds.Height - TextHeight) / 2.0, button.Enabled ? TextColour : DimTextColour);
		}
		string title = GetZoneName() + (Dirty ? "  (not saved)" : "");
		DrawText(renderer, title, ScreenWidth - Padding - MeasureText(title), (TopBarHeight - TextHeight) / 2.0, Dirty ? SelectionColour : DimTextColour);
	}

	private void DrawPanel(Renderer renderer, I2dRenderer g)
	{
		double left = ScreenWidth - PanelWidth;
		double bottom = ScreenHeight - StatusBarHeight;
		g.RenderQuad(PanelColour, new Rectangle(left, TopBarHeight, PanelWidth, bottom - TopBarHeight));

		// The palette
		DrawHeading(renderer, g, "Objects (click, then click in the level)", left, TopBarHeight);
		IReadOnlyList<ObjectType> types = GetPaletteTypes();
		Dictionary<string, int> counts = Placements.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
		double listTop = TopBarHeight + RowHeight;
		_paletteScroll = Math.Min(_paletteScroll, Math.Max(0.0, types.Count * RowHeight - (PaletteBottom - listTop)));
		g.ClipRectangle = new Rectangle(left, listTop, PanelWidth, PaletteBottom - listTop);
		for (int i = 0; i < types.Count; i++)
		{
			double y = listTop + i * RowHeight - _paletteScroll;
			if (y + RowHeight < listTop || y > PaletteBottom)
			{
				continue;
			}
			Rectangle row = new Rectangle(left, y, PanelWidth, RowHeight);
			string key = types[i].ResourceKey;
			if (string.Equals(key, _placeKey, StringComparison.OrdinalIgnoreCase))
			{
				g.RenderQuad(AccentColour, row);
			}
			else if (row.Contains(_mouse))
			{
				g.RenderQuad(ButtonHoverColour, row);
			}
			DrawText(renderer, ShortName(key), left + Padding, y + (RowHeight - TextHeight) / 2.0, TextColour);
			if (counts.TryGetValue(key, out int count))
			{
				string countText = count.ToString();
				DrawText(renderer, countText, ScreenWidth - Padding - MeasureText(countText), y + (RowHeight - TextHeight) / 2.0, DimTextColour);
			}
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);

		// The properties of the selection
		DrawHeading(renderer, g, _selection.Count == 0 ? "Properties (select an object)" : "Properties (click a value to change it)", left, PaletteBottom);
		List<PropertyRow> rows = GetPropertyRows();
		double rowsTop = PaletteBottom + RowHeight;
		_propertyScroll = Math.Min(_propertyScroll, Math.Max(0.0, rows.Count * RowHeight - (bottom - rowsTop)));
		g.ClipRectangle = new Rectangle(left, rowsTop, PanelWidth, bottom - rowsTop);
		for (int i = 0; i < rows.Count; i++)
		{
			double y = rowsTop + i * RowHeight - _propertyScroll;
			if (y + RowHeight < rowsTop || y > bottom)
			{
				continue;
			}
			PropertyRow row = rows[i];
			Rectangle bounds = new Rectangle(left, y, PanelWidth, RowHeight);
			bool editing = row.Label == _editingRow;
			bool changeable = row.Commit != null || row.Step != null;
			if (editing)
			{
				g.RenderQuad(AccentColour, bounds);
			}
			else if (changeable && bounds.Contains(_mouse))
			{
				g.RenderQuad(ButtonHoverColour, bounds);
			}
			double textY = y + (RowHeight - TextHeight) / 2.0;
			DrawText(renderer, row.Label, left + Padding, textY, row.IsHeading ? SelectionColour : DimTextColour);
			string value = editing ? _editText + ((_frames / 30) % 2 == 0 ? "|" : "") : row.Value;
			DrawText(renderer, value, left + PanelWidth * 0.45, textY, changeable || row.IsHeading ? TextColour : DimTextColour);
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
	}

	private void DrawHeading(Renderer renderer, I2dRenderer g, string text, double left, double top)
	{
		g.RenderQuad(ButtonColour, new Rectangle(left, top, PanelWidth, RowHeight));
		DrawText(renderer, text, left + Padding, top + (RowHeight - TextHeight) / 2.0, TextColour);
	}

	private void DrawStatusBar(Renderer renderer, I2dRenderer g)
	{
		double top = ScreenHeight - StatusBarHeight;
		g.RenderQuad(PanelColour, new Rectangle(0.0, top, ScreenWidth, StatusBarHeight));
		Vector2i position = Snap(_mouseWorld);
		string left = IsOverLevel(_mouse)
			? $"X {position.X}  Y {position.Y}   (original {position.X / 4}, {position.Y / 4})   Objects {Placements.Count}   Selected {_selection.Count}"
			: $"Objects {Placements.Count}   Selected {_selection.Count}";
		DrawText(renderer, left, Padding, top + (StatusBarHeight - TextHeight) / 2.0, DimTextColour);
		string right = _messageFrames > 0 ? _message : "Drag: move or select.  Right drag, Space drag or wheel: scroll.  Ctrl+wheel: zoom.  Del: delete.  Ctrl+Z: undo.";
		DrawText(renderer, right, ScreenWidth - Padding - MeasureText(right), top + (StatusBarHeight - TextHeight) / 2.0, _messageFrames > 0 ? TextColour : DimTextColour);
	}

	private string GetZoneName()
	{
		string key = _level.Binding?.Resource?.FullKeyPath ?? "";
		string[] parts = key.Split('/');
		return parts.Length >= 2 ? $"{parts[parts.Length - 2]} editor" : "Level editor";
	}

	private double TextHeight => _font.Height * TextScale;

	private double MeasureText(string text) => HasFont ? _font.MeasureString(text).Width * TextScale : text.Length * 10.0;

	private void DrawText(Renderer renderer, string text, double x, double y, Colour colour)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		using (g.BeginMatixState())
		{
			g.ModelMatrix = (g.ModelMatrix * Matrix4.CreateScale(TextScale, TextScale)).Translate(x, y);
			renderer.GetFontRenderer().RenderString(text, default, FontAlignment.Left, _font, colour);
		}
	}

	#endregion
}
