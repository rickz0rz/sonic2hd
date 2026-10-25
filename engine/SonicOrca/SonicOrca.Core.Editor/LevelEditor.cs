using System;
using System.Collections.Generic;
using System.IO;
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
/// Edits a level with the mouse and the keyboard. The editor pauses the level. A tool edits one part of the level:
/// the objects and rings, or the markers. Each change replaces items in the binding or the map, so a test play after
/// the editor uses the changes. Save writes the changed resources to the editor file of the zone (see
/// <see cref="EditorModFile"/>).
/// </summary>
public sealed class LevelEditor : IDisposable
{
	private const string FontResourceKey = "SONICORCA/FONTS/SEGOEUI";

	internal const double ScreenWidth = 1920.0;

	internal const double ScreenHeight = 1080.0;

	internal const double TopBarHeight = 48.0;

	internal const double StatusBarHeight = 36.0;

	internal const double PanelWidth = 460.0;

	internal const double RowHeight = 30.0;

	internal const double Padding = 10.0;

	private const double TextScale = 0.5;

	/// <summary>The list uses the top part of the side panel, down to this y value. The properties use the rest.</summary>
	private const double ListBottom = 500.0;

	/// <summary>Objects activate in the view and up to this distance around it.</summary>
	private const int LifetimeMargin = 512;

	/// <summary>A drag starts when the mouse moves this number of screen pixels with the button down.</summary>
	private const double DragThreshold = 4.0;

	/// <summary>A message in the status bar shows for this number of frames.</summary>
	private const int MessageFrames = 240;

	private static readonly int[] GridSizes = { 4, 16, 32, 64, 128 };

	private static readonly double[] Zooms = { 1.0, 0.75, 0.5, 0.35, 0.25 };

	internal static readonly Colour PanelColour = new Colour(0.96, 0.07, 0.08, 0.11);

	internal static readonly Colour ButtonColour = new Colour(1.0, 0.18, 0.2, 0.26);

	internal static readonly Colour ButtonHoverColour = new Colour(1.0, 0.27, 0.3, 0.38);

	internal static readonly Colour AccentColour = new Colour(1.0, 0.16, 0.44, 0.82);

	internal static readonly Colour TextColour = Colours.White;

	internal static readonly Colour DimTextColour = new Colour(1.0, 0.62, 0.65, 0.7);

	internal static readonly Colour OutlineColour = new Colour(0.45, 0.4, 0.85, 1.0);

	internal static readonly Colour HoverColour = Colours.White;

	internal static readonly Colour SelectionColour = new Colour(1.0, 1.0, 0.8, 0.1);

	internal static readonly Colour SelectionFillColour = new Colour(0.18, 1.0, 0.8, 0.1);

	internal static readonly Colour ShadowColour = new Colour(0.8, 0.0, 0.0, 0.0);

	private readonly Level _level;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly EditorHistory _history = new EditorHistory();

	private readonly EditorInput _input = new EditorInput();

	private readonly EditorTool[] _tools;

	private ResourceSession _resourceSession;

	private Font _font;

	private EditorTool _tool;

	private int _frames;

	private int _gridIndex = 1;

	private int _zoomIndex;

	private double _listScroll;

	private double _propertyScroll;

	private string _message;

	private int _messageFrames;

	/// <summary>The property row that the user types a value for, or null.</summary>
	private string _editingRow;

	private string _editText = "";

	/// <summary>The first key that the user types replaces the old value.</summary>
	private bool _replaceEditText;

	private DragMode _drag;

	private Vector2 _dragStartMouse;

	private Vector2 _dragStartCamera;

	private enum DragMode
	{
		None,
		Pan,
		Tool,
		Box
	}

	/// <summary>The objects and rings that are selected, by placement ID.</summary>
	internal HashSet<Guid> ObjectSelection { get; } = new HashSet<Guid>();

	/// <summary>The markers that are selected.</summary>
	internal HashSet<LevelMarker> MarkerSelection { get; } = new HashSet<LevelMarker>();

	/// <summary>The binding has changes that are not saved.</summary>
	public bool BindingDirty { get; private set; }

	/// <summary>The markers of the map have changes that are not saved.</summary>
	public bool MarkersDirty { get; private set; }

	/// <summary>There are changes that are not saved.</summary>
	public bool Dirty => BindingDirty || MarkersDirty;

	public EditorPlayRequest PlayRequest { get; private set; }

	/// <summary>The position for <see cref="EditorPlayRequest.FromCursor"/>.</summary>
	public Vector2i PlayPosition { get; private set; }

	internal Level Level => _level;

	internal SonicOrcaGameContext GameContext => _gameContext;

	internal Camera Camera => _level.Camera;

	internal IList<ObjectPlacement> Placements => _level.Binding.ObjectPlacements;

	internal IList<LevelMarker> Markers => _level.Map.Markers;

	internal Vector2 Mouse { get; private set; }

	internal Vector2 MouseWorld { get; private set; }

	internal Vector2 DragStartWorld { get; private set; }

	/// <summary>The mouse moved far enough with the button down to count as a drag.</summary>
	internal bool DragMoved { get; private set; }

	internal int GridSize => GridSizes[_gridIndex];

	internal double Zoom => Zooms[_zoomIndex];

	internal bool IsControlDown => IsKeyDown(KeyboardState.KEY_LCTRL) || IsKeyDown(KeyboardState.KEY_RCTRL) ||
		IsKeyDown(KeyboardState.KEY_LGUI) || IsKeyDown(KeyboardState.KEY_RGUI);

	internal bool IsShiftDown => IsKeyDown(KeyboardState.KEY_LSHIFT) || IsKeyDown(KeyboardState.KEY_RSHIFT);

	internal bool IsMouseOverLevel => IsOverLevel(Mouse);

	private bool HasFont => _font != null;

	public LevelEditor(Level level)
	{
		_level = level;
		_gameContext = level.GameContext;
		_tools = new EditorTool[] { new ObjectsTool(this), new MarkersTool(this) };
		_tool = _tools[0];
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
		foreach (EditorTool tool in _tools)
		{
			tool.Dispose();
		}
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
		foreach (EditorTool tool in _tools)
		{
			tool.Begin();
		}
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
		_input.Read(_gameContext);
		Mouse = _input.Mouse;
		MouseWorld = ToWorld(Mouse);
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
		_tool.Update();
		_level.TileSet.Animate();
		_level.Map.Animate();
		_level.ObjectManager.ResetLifetimeArea();
		_level.ObjectManager.AddLifeArea(((Rectanglei)Camera.Bounds).Inflate(new Vector2i(LifetimeMargin, LifetimeMargin)));
		_level.ObjectManager.UpdateEditor();
	}

	#region Input

	private bool IsKeyDown(int key) => _input.IsKeyDown(key);

	private bool IsKeyPressed(int key) => _input.IsKeyPressed(key);

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
		else if (!control && IsKeyPressed(KeyboardState.KEY_1))
		{
			SelectTool(0);
		}
		else if (!control && IsKeyPressed(KeyboardState.KEY_2))
		{
			SelectTool(1);
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
			_tool.Copy();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_X))
		{
			_tool.Copy();
			_tool.DeleteSelection();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_V))
		{
			_tool.Paste(IsOverLevel(Mouse) ? MouseWorld : Camera.Bounds.Centre);
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_D))
		{
			_tool.Duplicate();
		}
		else if (control && IsKeyPressed(KeyboardState.KEY_A))
		{
			_tool.SelectAllInView();
		}
		else if (IsKeyPressed(KeyboardState.KEY_DELETE) || IsKeyPressed(KeyboardState.KEY_BACKSPACE))
		{
			_tool.DeleteSelection();
		}
		else if (IsKeyPressed(KeyboardState.KEY_ESCAPE))
		{
			if (!_tool.Cancel())
			{
				_tool.ClearSelection();
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
		if (_tool.HasSelection)
		{
			int pressedX = (IsKeyPressed(KeyboardState.KEY_RIGHT) ? 1 : 0) - (IsKeyPressed(KeyboardState.KEY_LEFT) ? 1 : 0);
			int pressedY = (IsKeyPressed(KeyboardState.KEY_DOWN) ? 1 : 0) - (IsKeyPressed(KeyboardState.KEY_UP) ? 1 : 0);
			if (pressedX != 0 || pressedY != 0)
			{
				int step = GridSize * (shift ? 4 : 1);
				_tool.Nudge(new Vector2i(pressedX * step, pressedY * step));
			}
			return;
		}
		int x = (IsKeyDown(KeyboardState.KEY_RIGHT) ? 1 : 0) - (IsKeyDown(KeyboardState.KEY_LEFT) ? 1 : 0);
		int y = (IsKeyDown(KeyboardState.KEY_DOWN) ? 1 : 0) - (IsKeyDown(KeyboardState.KEY_UP) ? 1 : 0);
		if (x != 0 || y != 0)
		{
			double speed = (shift ? 64.0 : 16.0) / Zoom;
			MoveView(x * speed, y * speed);
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
		else if (IsKeyPressed(KeyboardState.KEY_F5) || IsKeyPressed(KeyboardState.KEY_F2))
		{
			// The play keys also work while the user types. The typed value is kept.
			RequestPlay(IsKeyPressed(KeyboardState.KEY_F5) && !IsShiftDown ? EditorPlayRequest.FromCursor : EditorPlayRequest.FromStart);
		}
	}

	private void CommitTextEntry()
	{
		EditorPropertyRow row = _tool.GetPropertyRows().FirstOrDefault(r => r.Label == _editingRow);
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
			if (IsOverTopBar(Mouse))
			{
				if (_input.LeftPressed)
				{
					GetTopBarButtons().FirstOrDefault(b => b.Bounds.Contains(Mouse) && b.Enabled)?.Action();
				}
			}
			else if (IsOverPanel(Mouse))
			{
				OnPanelClick(_input.LeftPressed ? 1 : -1);
			}
			else if (IsOverLevel(Mouse))
			{
				if (_input.LeftPressed && !IsKeyDown(KeyboardState.KEY_SPACE))
				{
					_tool.OnLevelPress();
				}
				else
				{
					BeginDrag(DragMode.Pan);
				}
			}
		}
		else if (_input.MiddlePressed && IsOverLevel(Mouse))
		{
			BeginDrag(DragMode.Pan);
		}
	}

	private void UpdateWheel(double wheel)
	{
		if (IsOverPanel(Mouse))
		{
			if (Mouse.Y < ListBottom)
			{
				_listScroll = Math.Max(0.0, _listScroll - wheel * RowHeight);
			}
			else
			{
				_propertyScroll = Math.Max(0.0, _propertyScroll - wheel * RowHeight);
			}
		}
		else if (IsOverLevel(Mouse))
		{
			if (IsControlDown)
			{
				SetZoom(_zoomIndex - Math.Sign(wheel), MouseWorld, Mouse);
			}
			else
			{
				double distance = wheel * 96.0 / Zoom;
				if (IsShiftDown)
				{
					MoveView(-distance, 0.0);
				}
				else
				{
					MoveView(0.0, -distance);
				}
			}
		}
	}

	/// <summary>The tool moves or resizes its selection while the mouse button is down (<see cref="EditorTool.OnDrag"/>).</summary>
	internal void BeginToolDrag() => BeginDrag(DragMode.Tool);

	/// <summary>The user drags a box. At the end, the tool selects the items in it (<see cref="EditorTool.SelectInBox"/>).</summary>
	internal void BeginBoxSelect() => BeginDrag(DragMode.Box);

	private void BeginDrag(DragMode mode)
	{
		_drag = mode;
		_dragStartMouse = Mouse;
		DragStartWorld = MouseWorld;
		_dragStartCamera = new Vector2(Camera.Bounds.X, Camera.Bounds.Y);
		DragMoved = false;
	}

	private void UpdateDrag()
	{
		if ((Mouse - _dragStartMouse).Length >= DragThreshold)
		{
			DragMoved = true;
		}
		if (_drag == DragMode.Pan)
		{
			Vector2 offset = (Mouse - _dragStartMouse) * (1.0 / Zoom);
			Rectangle bounds = Camera.Bounds;
			Camera.Bounds = new Rectangle(_dragStartCamera.X - offset.X, _dragStartCamera.Y - offset.Y, bounds.Width, bounds.Height);
		}
		else if (_drag == DragMode.Tool && DragMoved)
		{
			_tool.OnDrag();
		}
		bool buttonDown = _input.Left || _input.Right || _input.Middle;
		if (buttonDown && !_input.AnyReleased)
		{
			return;
		}
		DragMode mode = _drag;
		_drag = DragMode.None;
		if (mode == DragMode.Tool)
		{
			_tool.OnDragEnd();
		}
		else if (mode == DragMode.Box && DragMoved)
		{
			_tool.SelectInBox(GetDragBox());
		}
	}

	private Rectangle GetDragBox()
	{
		return Rectangle.FromLTRB(
			Math.Min(DragStartWorld.X, MouseWorld.X), Math.Min(DragStartWorld.Y, MouseWorld.Y),
			Math.Max(DragStartWorld.X, MouseWorld.X), Math.Max(DragStartWorld.Y, MouseWorld.Y));
	}

	#endregion

	#region Commands

	private void SelectTool(int index)
	{
		if (_tool == _tools[index])
		{
			return;
		}
		if (_editingRow != null)
		{
			CommitTextEntry();
		}
		_tool.Cancel();
		_tool = _tools[index];
		_listScroll = 0.0;
		_propertyScroll = 0.0;
		ShowMessage(_tool.Name + ": " + _tool.Hint);
	}

	private void RequestPlay(EditorPlayRequest request)
	{
		if (_editingRow != null)
		{
			CommitTextEntry();
		}
		PlayPosition = IsOverLevel(Mouse) ? Snap(MouseWorld) : (Vector2i)Camera.Bounds.Centre;
		PlayRequest = request;
	}

	public void Save()
	{
		try
		{
			string path = EditorModFile.Save(GetChangedResources(), EditorModFile.GetPath(_gameContext.UserDataDirectory, _level.Binding.Resource.FullKeyPath));
			BindingDirty = false;
			MarkersDirty = false;
			ShowMessage("Saved to " + path);
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ResourceException || ex is InvalidOperationException)
		{
			ShowMessage("Save failed: " + ex.Message);
		}
	}

	/// <summary>
	/// Writes the changes that are not saved to mods/editor/unsaved/ZONE.dat, which the game does not load. The level
	/// calls this method when it unloads with changes that are not saved.
	/// </summary>
	public void SaveBackup()
	{
		if (!Dirty)
		{
			return;
		}
		string zoneFile = EditorModFile.GetPath(_gameContext.UserDataDirectory, _level.Binding.Resource.FullKeyPath);
		string path = Path.Combine(Path.GetDirectoryName(zoneFile), EditorModFile.UnsavedFolderName, Path.GetFileName(zoneFile));
		try
		{
			EditorModFile.Save(GetChangedResources(), path, updateSources: false);
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ResourceException || ex is InvalidOperationException)
		{
			System.Diagnostics.Trace.WriteLine("The editor could not save the backup: " + ex.Message);
		}
	}

	/// <summary>The resources to save: the binding and the map, when they have changes that are not saved.</summary>
	private IReadOnlyList<EditorModFile.Change> GetChangedResources()
	{
		List<EditorModFile.Change> changes = new List<EditorModFile.Change>();
		if (BindingDirty)
		{
			changes.Add(EditorModFile.Change.ForBinding(_level.Binding));
		}
		if (MarkersDirty)
		{
			changes.Add(EditorModFile.Change.ForMap(_level.Map, markers: true));
		}
		return changes;
	}

	public void Undo()
	{
		EditorSnapshot snapshot = _history.Undo(CaptureSnapshot());
		if (snapshot != null)
		{
			Restore(snapshot);
		}
	}

	public void Redo()
	{
		EditorSnapshot snapshot = _history.Redo(CaptureSnapshot());
		if (snapshot != null)
		{
			Restore(snapshot);
		}
	}

	/// <summary>Keeps the state before a change, for undo. Call it before each change.</summary>
	internal void RecordHistory() => _history.Record(CaptureSnapshot());

	private EditorSnapshot CaptureSnapshot()
	{
		return new EditorSnapshot
		{
			Placements = Placements.ToArray(),
			Selection = ObjectSelection.ToArray(),
			Markers = Markers.ToArray(),
			MarkerSelection = MarkerSelection.ToArray()
		};
	}

	private void Restore(EditorSnapshot snapshot)
	{
		if (!snapshot.Placements.SequenceEqual(Placements))
		{
			Placements.Clear();
			foreach (ObjectPlacement placement in snapshot.Placements)
			{
				Placements.Add(placement);
			}
			MarkBindingChanged();
		}
		if (!snapshot.Markers.SequenceEqual(Markers))
		{
			Markers.Clear();
			foreach (LevelMarker marker in snapshot.Markers)
			{
				Markers.Add(marker);
			}
			MarkMarkersChanged();
		}
		ObjectSelection.Clear();
		ObjectSelection.UnionWith(snapshot.Selection);
		MarkerSelection.Clear();
		MarkerSelection.UnionWith(snapshot.MarkerSelection);
		Rebind();
	}

	/// <summary>The placements changed. The objects show the change on the next frame.</summary>
	internal void MarkBindingChanged()
	{
		BindingDirty = true;
		Rebind();
	}

	internal void MarkMarkersChanged()
	{
		MarkersDirty = true;
	}

	/// <summary>Makes the object entries again from the placements, so that the level shows the placements as they are.</summary>
	private void Rebind()
	{
		_level.ObjectManager.Rebind(_level.Binding);
	}

	internal void ShowMessage(string message)
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
		Camera.Bounds = new Rectangle(focusWorld.X - focusScreen.X / Zoom, focusWorld.Y - focusScreen.Y / Zoom, ScreenWidth / Zoom, ScreenHeight / Zoom);
	}

	private void MoveView(double x, double y)
	{
		Rectangle bounds = Camera.Bounds;
		Camera.Bounds = new Rectangle(bounds.X + x, bounds.Y + y, bounds.Width, bounds.Height);
	}

	internal void CentreOn(Vector2 position)
	{
		Rectangle bounds = Camera.Bounds;
		Camera.Bounds = new Rectangle(position.X - bounds.Width / 2.0, position.Y - bounds.Height / 2.0, bounds.Width, bounds.Height);
	}

	/// <summary>Keeps the view in the map, with a margin, so that the user cannot lose the level.</summary>
	private void ClampCamera()
	{
		Rectangle bounds = Camera.Bounds;
		Rectanglei map = _level.Map.Bounds;
		const double margin = 1024.0;
		double x = Math.Clamp(bounds.X, -margin, Math.Max(-margin, map.Width + margin - bounds.Width));
		double y = Math.Clamp(bounds.Y, -margin, Math.Max(-margin, map.Height + margin - bounds.Height));
		Camera.Bounds = new Rectangle(x, y, bounds.Width, bounds.Height);
	}

	private Vector2 ToWorld(Vector2 screen)
	{
		Rectangle bounds = Camera.Bounds;
		return new Vector2(bounds.X + screen.X / Zoom, bounds.Y + screen.Y / Zoom);
	}

	internal Vector2 ToScreen(Vector2 world)
	{
		Rectangle bounds = Camera.Bounds;
		return new Vector2((world.X - bounds.X) * Zoom, (world.Y - bounds.Y) * Zoom);
	}

	internal Rectangle ToScreen(Rectangle world)
	{
		Vector2 topLeft = ToScreen(new Vector2(world.X, world.Y));
		return new Rectangle(topLeft.X, topLeft.Y, world.Width * Zoom, world.Height * Zoom);
	}

	internal Vector2i Snap(Vector2 position)
	{
		int grid = GridSize;
		return new Vector2i((int)Math.Round(position.X / grid) * grid, (int)Math.Round(position.Y / grid) * grid);
	}

	private static bool IsOverTopBar(Vector2 screen) => screen.Y < TopBarHeight;

	private static bool IsOverPanel(Vector2 screen) => !IsOverTopBar(screen) && screen.X >= ScreenWidth - PanelWidth;

	private static bool IsOverStatusBar(Vector2 screen) => screen.Y >= ScreenHeight - StatusBarHeight;

	private static bool IsOverLevel(Vector2 screen) => !IsOverTopBar(screen) && !IsOverPanel(screen) && !IsOverStatusBar(screen);

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
		for (int i = 0; i < _tools.Length; i++)
		{
			int index = i;
			Add($"{_tools[i].Name} ({i + 1})", () => SelectTool(index), active: _tool == _tools[i]);
		}
		x += Padding;
		Add("Play here (F5)", () => RequestPlay(EditorPlayRequest.FromCursor));
		Add("Play from start (F2)", () => RequestPlay(EditorPlayRequest.FromStart));
		Add("Save", Save, active: Dirty);
		Add("Undo", Undo, _history.CanUndo);
		Add("Redo", Redo, _history.CanRedo);
		Add("Delete", _tool.DeleteSelection, _tool.HasSelection);
		Add($"Grid {GridSize}", () => _gridIndex = (_gridIndex + 1) % GridSizes.Length);
		Add($"Zoom {Zoom * 100:0}%", () => SetZoom((_zoomIndex + 1) % Zooms.Length, Camera.Bounds.Centre));
		return buttons;
	}

	private void OnPanelClick(int direction)
	{
		if (Mouse.Y < ListBottom)
		{
			int index = (int)Math.Floor((Mouse.Y - (TopBarHeight + RowHeight) + _listScroll) / RowHeight);
			if (Mouse.Y >= TopBarHeight + RowHeight && index >= 0 && index < _tool.GetListRows().Count)
			{
				_tool.OnListClick(index, direction);
			}
			return;
		}
		List<EditorPropertyRow> rows = _tool.GetPropertyRows();
		int rowIndex = GetPropertyRowIndexAt(Mouse.Y);
		if (rowIndex < 0 || rowIndex >= rows.Count)
		{
			return;
		}
		EditorPropertyRow row = rows[rowIndex];
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
		double top = ListBottom + RowHeight;
		if (y < top)
		{
			return -1;
		}
		return (int)Math.Floor((y - top + _propertyScroll) / RowHeight);
	}

	private bool IsOverPropertyRow(string label)
	{
		if (!IsOverPanel(Mouse) || Mouse.Y < ListBottom)
		{
			return false;
		}
		List<EditorPropertyRow> rows = _tool.GetPropertyRows();
		int index = GetPropertyRowIndexAt(Mouse.Y);
		return index >= 0 && index < rows.Count && rows[index].Label == label;
	}

	#endregion

	#region Drawing

	public void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		// The level renderer leaves the scale of the zoom in the model matrix and in the object renderer, which the
		// font renderer uses
		g.ModelMatrix = Matrix4.Identity;
		renderer.GetObjectRenderer().Scale = new Vector2(1.0, 1.0);
		_tool.DrawOverlay(renderer, g);
		DrawStartPosition(renderer, g);
		if (_drag == DragMode.Box && DragMoved)
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

	/// <summary>While the level plays, reminds the user that the editor has changes that are not saved.</summary>
	public void DrawPlayReminder(Renderer renderer)
	{
		if (!Dirty || !HasFont)
		{
			return;
		}
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.ModelMatrix = Matrix4.Identity;
		const string text = "Editor changes not saved (F2, then Ctrl+S)";
		DrawLabel(renderer, g, text, ScreenWidth - MeasureText(text) - Padding * 3.0, Padding, SelectionColour);
		renderer.DeativateRenderer();
	}

	private void DrawStartPosition(Renderer renderer, I2dRenderer g)
	{
		Vector2 start = ToScreen((Vector2)_level.StartPosition);
		DrawCross(g, start, 24.0, ShadowColour, 7.0);
		DrawCross(g, start, 24.0, Colours.White, 3.0);
		DrawLabel(renderer, g, "START", start.X + 10.0, start.Y - 40.0, TextColour);
	}

	internal static void DrawCross(I2dRenderer g, Vector2 centre, double size, Colour colour, double thickness)
	{
		g.RenderLine(colour, new Vector2(centre.X - size, centre.Y), new Vector2(centre.X + size, centre.Y), thickness);
		g.RenderLine(colour, new Vector2(centre.X, centre.Y - size), new Vector2(centre.X, centre.Y + size), thickness);
	}

	/// <summary>Draws a text on a dark box, for labels in the level.</summary>
	internal void DrawLabel(Renderer renderer, I2dRenderer g, string text, double x, double y, Colour colour)
	{
		if (!HasFont)
		{
			return;
		}
		Rectangle box = new Rectangle(x, y, MeasureText(text) + Padding, RowHeight - 4.0);
		g.RenderQuad(PanelColour, box);
		DrawText(renderer, text, box.X + Padding / 2.0, box.Y + 2.0, colour);
	}

	/// <summary>Shows what is under the mouse, or what a click adds.</summary>
	private void DrawCursorLabel(Renderer renderer, I2dRenderer g)
	{
		if (!IsOverLevel(Mouse) || _drag != DragMode.None)
		{
			return;
		}
		string label = _tool.GetCursorLabel(g);
		if (label != null)
		{
			DrawLabel(renderer, g, label, Mouse.X + 18.0, Mouse.Y + 18.0, TextColour);
		}
	}

	private void DrawTopBar(Renderer renderer, I2dRenderer g)
	{
		g.RenderQuad(PanelColour, new Rectangle(0.0, 0.0, ScreenWidth, TopBarHeight));
		foreach (Button button in GetTopBarButtons())
		{
			bool hover = button.Enabled && button.Bounds.Contains(Mouse);
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

		DrawHeading(renderer, g, _tool.ListHeading, left, TopBarHeight);
		IReadOnlyList<EditorListRow> listRows = _tool.GetListRows();
		double listTop = TopBarHeight + RowHeight;
		_listScroll = Math.Min(_listScroll, Math.Max(0.0, listRows.Count * RowHeight - (ListBottom - listTop)));
		g.ClipRectangle = new Rectangle(left, listTop, PanelWidth, ListBottom - listTop);
		for (int i = 0; i < listRows.Count; i++)
		{
			double y = listTop + i * RowHeight - _listScroll;
			if (y + RowHeight < listTop || y > ListBottom)
			{
				continue;
			}
			EditorListRow listRow = listRows[i];
			Rectangle row = new Rectangle(left, y, PanelWidth, RowHeight);
			if (listRow.Active)
			{
				g.RenderQuad(AccentColour, row);
			}
			else if (row.Contains(Mouse))
			{
				g.RenderQuad(ButtonHoverColour, row);
			}
			double textY = y + (RowHeight - TextHeight) / 2.0;
			DrawText(renderer, listRow.Text, left + Padding, textY, listRow.Dim ? DimTextColour : TextColour);
			if (!string.IsNullOrEmpty(listRow.Detail))
			{
				DrawText(renderer, listRow.Detail, ScreenWidth - Padding - MeasureText(listRow.Detail), textY, DimTextColour);
			}
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);

		DrawHeading(renderer, g, _tool.PropertiesHeading, left, ListBottom);
		List<EditorPropertyRow> rows = _tool.GetPropertyRows();
		double rowsTop = ListBottom + RowHeight;
		_propertyScroll = Math.Min(_propertyScroll, Math.Max(0.0, rows.Count * RowHeight - (bottom - rowsTop)));
		g.ClipRectangle = new Rectangle(left, rowsTop, PanelWidth, bottom - rowsTop);
		for (int i = 0; i < rows.Count; i++)
		{
			double y = rowsTop + i * RowHeight - _propertyScroll;
			if (y + RowHeight < rowsTop || y > bottom)
			{
				continue;
			}
			EditorPropertyRow row = rows[i];
			Rectangle bounds = new Rectangle(left, y, PanelWidth, RowHeight);
			bool editing = row.Label == _editingRow;
			bool changeable = row.Commit != null || row.Step != null;
			if (editing)
			{
				g.RenderQuad(AccentColour, bounds);
			}
			else if (changeable && bounds.Contains(Mouse))
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
		Vector2i position = Snap(MouseWorld);
		string left = IsOverLevel(Mouse)
			? $"X {position.X}  Y {position.Y}   (original {position.X / 4}, {position.Y / 4})   {_tool.Status}"
			: _tool.Status;
		DrawText(renderer, left, Padding, top + (StatusBarHeight - TextHeight) / 2.0, DimTextColour);
		string right = _messageFrames > 0 ? _message : _tool.Hint;
		DrawText(renderer, right, ScreenWidth - Padding - MeasureText(right), top + (StatusBarHeight - TextHeight) / 2.0, _messageFrames > 0 ? TextColour : DimTextColour);
	}

	private string GetZoneName()
	{
		string key = _level.Binding?.Resource?.FullKeyPath ?? "";
		string[] parts = key.Split('/');
		return parts.Length >= 2 ? $"{parts[parts.Length - 2]} editor" : "Level editor";
	}

	private double TextHeight => _font.Height * TextScale;

	internal double MeasureText(string text) => HasFont ? _font.MeasureString(text).Width * TextScale : text.Length * 10.0;

	internal void DrawText(Renderer renderer, string text, double x, double y, Colour colour)
	{
		if (!HasFont)
		{
			return;
		}
		I2dRenderer g = renderer.Get2dRenderer();
		using (g.BeginMatixState())
		{
			g.ModelMatrix = (g.ModelMatrix * Matrix4.CreateScale(TextScale, TextScale)).Translate(x, y);
			renderer.GetFontRenderer().RenderString(text, default, FontAlignment.Left, _font, colour);
		}
	}

	#endregion
}
