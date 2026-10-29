using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Original;
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
	private const string FontResourceKey = EditorUi.FontResourceKey;

	internal const double ScreenWidth = 1920.0;

	internal const double ScreenHeight = 1080.0;

	internal const double TopBarHeight = EditorUi.TopBarHeight;

	internal const double StatusBarHeight = EditorUi.StatusBarHeight;

	internal const double PanelWidth = EditorUi.PanelWidth;

	internal const double RowHeight = EditorUi.RowHeight;

	internal const double Padding = EditorUi.Padding;

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

	internal static readonly Colour PanelColour = EditorUi.PanelColour;

	internal static readonly Colour ButtonColour = EditorUi.ButtonColour;

	internal static readonly Colour ButtonHoverColour = EditorUi.ButtonHoverColour;

	internal static readonly Colour AccentColour = EditorUi.AccentColour;

	internal static readonly Colour TextColour = EditorUi.TextColour;

	internal static readonly Colour DimTextColour = EditorUi.DimTextColour;

	internal static readonly Colour OutlineColour = EditorUi.OutlineColour;

	internal static readonly Colour HoverColour = EditorUi.HoverColour;

	internal static readonly Colour SelectionColour = EditorUi.SelectionColour;

	internal static readonly Colour SelectionFillColour = EditorUi.SelectionFillColour;

	internal static readonly Colour ShadowColour = EditorUi.ShadowColour;

	private readonly Level _level;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly EditorHistory _history = new EditorHistory();

	private readonly EditorInput _input = new EditorInput();

	private readonly EditorTool[] _tools;

	private ResourceSession _resourceSession;

	/// <summary>The look of the editor, with its font.</summary>
	private readonly EditorUi _ui = new EditorUi();

	/// <summary>The value that the user types into a property row.</summary>
	private readonly EditorTextEntry _entry = new EditorTextEntry();

	private EditorTool _tool;

	private int _frames;

	private int _gridIndex = 1;

	private int _zoomIndex;

	/// <summary>The centre of the view before it went to a layer that shows alone, or null.</summary>
	private Vector2? _viewBeforeLayerAlone;

	private double _listScroll;

	private double _propertyScroll;

	private string _message;

	private int _messageFrames;

	/// <summary>The animation editor of the level. It keeps its changes and its undo history when it closes.</summary>
	private AnimationEditorScreen _animationEditor;

	/// <summary>The animation editor loads its font and the list of the groups.</summary>
	private Task _animationEditorLoad;

	private bool _animationEditorOpen;

	/// <summary>The group, the animation and the hit boxes that the animation editor shows when it opens.</summary>
	private (string Key, int Index, Rectanglei[] Hitboxes) _animationTarget;




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

	/// <summary>The collision lines that are selected.</summary>
	internal HashSet<CollisionVector> VectorSelection { get; } = new HashSet<CollisionVector>();

	/// <summary>
	/// The collision data of the original for the front layer, or null when the zone has none (see
	/// <see cref="TileCollision"/>). With it, the collision follows the tiles.
	/// </summary>
	internal TileCollision TileCollision { get; private set; }

	/// <summary>The binding has changes that are not saved.</summary>
	public bool BindingDirty { get; private set; }

	/// <summary>The markers of the map have changes that are not saved.</summary>
	public bool MarkersDirty { get; private set; }

	/// <summary>The tiles of the map have changes that are not saved.</summary>
	public bool TilesDirty { get; private set; }

	/// <summary>The collision lines of the map have changes that are not saved.</summary>
	public bool CollisionDirty { get; private set; }

	/// <summary>The solidity of the cells (<see cref="TileCollision"/>) has changes that are not saved.</summary>
	public bool SolidityDirty { get; private set; }

	/// <summary>There are changes that are not saved.</summary>
	public bool Dirty => BindingDirty || MarkersDirty || TilesDirty || CollisionDirty || SolidityDirty;

	public EditorPlayRequest PlayRequest { get; private set; }

	/// <summary>The position for <see cref="EditorPlayRequest.FromCursor"/>.</summary>
	public Vector2i PlayPosition { get; private set; }

	internal Level Level => _level;

	internal SonicOrcaGameContext GameContext => _gameContext;

	internal Camera Camera => _level.Camera;

	internal Renderer Renderer => _gameContext.Renderer;

	internal IList<ObjectPlacement> Placements => _level.Binding.ObjectPlacements;

	internal IList<LevelMarker> Markers => _level.Map.Markers;

	internal IList<CollisionVector> Vectors => _level.Map.CollisionVectors;

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

	private bool HasFont => _ui.HasFont;

	public LevelEditor(Level level)
	{
		_level = level;
		_gameContext = level.GameContext;
		_tools = new EditorTool[] { new ObjectsTool(this), new MarkersTool(this), new TilesTool(this), new CollisionTool(this) };
		_tool = _tools[0];
	}

	public async Task LoadAsync(CancellationToken ct = default)
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependency(FontResourceKey);
		await _resourceSession.LoadAsync(ct);
		_ui.Font = resourceTree.GetLoadedResource<Font>(FontResourceKey);
	}

	public void Dispose()
	{
		if (_animationEditor != null && _animationEditorLoad == null)
		{
			_animationEditor.Unload();
		}
		_animationEditor = null;
		foreach (EditorTool tool in _tools)
		{
			tool.Dispose();
		}
		_resourceSession?.Dispose();
		_resourceSession = null;
		_ui.Font = null;
	}

	/// <summary>Opens the editor on the level as it is now. The view stays where the camera is.</summary>
	public void Begin()
	{
		PlayRequest = EditorPlayRequest.None;
		_input.Begin(_gameContext);
		_frames = 0;
		_drag = DragMode.None;
		_entry.Cancel();
		// The editor changes the map as it is in the file, without the layout changes of the play
		LayoutChanges.Reset(_level.Map);
		TileCollision ??= LoadTileCollision();
		_viewBeforeLayerAlone = null;
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
		// The tiles tool paints at level positions. Thus a background layer shows at these positions while the tool paints on it.
		if (!EditorLayerView.Apply(_level.Map.Layers, _tool is TilesTool tiles ? tiles.LayerIndex : -1))
		{
			RestoreViewBeforeLayerAlone();
		}
		if (_messageFrames > 0)
		{
			_messageFrames--;
		}
		if (_animationEditorLoad != null || _animationEditorOpen)
		{
			UpdateAnimationEditor();
			return;
		}
		_input.Read(_gameContext);
		Mouse = _input.Mouse;
		MouseWorld = ToWorld(Mouse);
		if (!_gameContext.Console.IsOpen)
		{
			if (_entry.IsActive)
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
		UpdateTrace();
		_level.TileSet.Animate();
		_level.Map.Animate();
		_level.ObjectManager.ResetLifetimeArea();
		_level.ObjectManager.AddLifeArea(((Rectanglei)Camera.Bounds).Inflate(new Vector2i(LifetimeMargin, LifetimeMargin)));
		_level.ObjectManager.UpdateEditor();
	}

	#region Input

	private bool IsKeyDown(int key) => _input.IsKeyDown(key);

	private bool IsKeyPressed(int key) => _input.IsKeyPressed(key);

	/// <summary>A key for a command of a tool. It is false while the user types a value or the console is open.</summary>
	internal bool IsCommandKeyPressed(int key) => !_entry.IsActive && !_gameContext.Console.IsOpen && !IsControlDown && IsKeyPressed(key);

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
		else if (!control && IsKeyPressed(KeyboardState.KEY_3))
		{
			SelectTool(2);
		}
		else if (!control && IsKeyPressed(KeyboardState.KEY_4))
		{
			SelectTool(3);
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
		else if (IsKeyPressed(KeyboardState.KEY_N))
		{
			OpenAnimationEditor();
		}
		else if (IsKeyPressed(KeyboardState.KEY_H))
		{
			ToggleHitboxes();
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
		if (_entry.Update(_input))
		{
			CommitTextEntry();
		}
		else if (IsKeyPressed(KeyboardState.KEY_ESCAPE))
		{
			_entry.Cancel();
		}
		else if (IsKeyPressed(KeyboardState.KEY_F5) || IsKeyPressed(KeyboardState.KEY_F2))
		{
			// The play keys also work while the user types. The typed value is kept.
			RequestPlay(IsKeyPressed(KeyboardState.KEY_F5) && !IsShiftDown ? EditorPlayRequest.FromCursor : EditorPlayRequest.FromStart);
		}
	}

	private void CommitTextEntry()
	{
		EditorPropertyRow row = _tool.GetPropertyRows().FirstOrDefault(r => r.Label == _entry.Row);
		string text = _entry.Text;
		_entry.Cancel();
		if (row?.Commit != null && !row.Commit(text))
		{
			ShowMessage("Value not accepted: " + text);
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
			if (_entry.IsActive && !IsOverPropertyRow(_entry.Row))
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
			else if (_tool.HasCustomProperties)
			{
				_tool.OnPropertiesWheel(wheel);
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
		if (_entry.IsActive)
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
		CompleteTraces();
		if (_entry.IsActive)
		{
			CommitTextEntry();
		}
		EditorLayerView.Apply(_level.Map.Layers, -1);
		// The mouse is not at a level position while a background layer shows alone
		bool restored = RestoreViewBeforeLayerAlone();
		PlayPosition = IsOverLevel(Mouse) && !restored ? Snap(MouseWorld) : (Vector2i)Camera.Bounds.Centre;
		PlayRequest = request;
	}

	/// <summary>
	/// Goes to the area of a layer that shows alone, when the area is not in view. The view before is kept, so that
	/// the view goes back when all layers show again.
	/// </summary>
	internal void ShowLayerAlone(Rectangle area)
	{
		Rectangle view = Camera.Bounds;
		if (view.Right > area.Left && view.Left < area.Right && view.Bottom > area.Top && view.Top < area.Bottom)
		{
			return;
		}
		_viewBeforeLayerAlone ??= view.Centre;
		CentreOn(new Vector2(view.Width / 2.0, view.Height / 2.0));
	}

	/// <returns>True when the view went back.</returns>
	private bool RestoreViewBeforeLayerAlone()
	{
		if (_viewBeforeLayerAlone is not Vector2 centre)
		{
			return false;
		}
		_viewBeforeLayerAlone = null;
		CentreOn(centre);
		return true;
	}

	#region Animation editor

	/// <summary>
	/// Opens the animation editor for the selected object, or for the protagonist when no object is selected. The
	/// editor shows the animation that the object plays now, with the hit boxes of the object.
	/// </summary>
	private void OpenAnimationEditor()
	{
		ActiveObject target = _level.ObjectManager.ActiveObjects
			.FirstOrDefault(o => o.Entry != null && o.Entry.Active == o && ObjectSelection.Contains(o.Entry.Uid))
			?? _level.Player?.Protagonist as ActiveObject;
		AnimationInstance animation = target != null ? FindMainAnimation(target) : null;
		if (target != null && animation == null)
		{
			ShowMessage("The selected object has no animation that the editor can find.");
			return;
		}
		_animationTarget = (animation?.AnimationGroup.Resource.FullKeyPath, animation?.Index ?? 0,
			target?.Hitboxes.ToArray() ?? Array.Empty<Rectanglei>());
		CompleteTraces();
		_drag = DragMode.None;
		if (_animationEditor == null)
		{
			_animationEditor = new AnimationEditorScreen(_gameContext, _animationTarget.Key, hosted: true);
			_animationEditorLoad = _animationEditor.LoadAsync(new SonicOrca.Menu.ScreenLoadingProgress(_animationEditor));
			return;
		}
		ShowAnimationEditor();
	}

	private void ShowAnimationEditor()
	{
		_animationEditorOpen = true;
		_animationEditor.Open(_animationTarget.Key, _animationTarget.Index, _animationTarget.Hitboxes);
	}

	private void UpdateAnimationEditor()
	{
		if (_animationEditorLoad != null)
		{
			if (!_animationEditorLoad.IsCompleted)
			{
				return;
			}
			Task load = _animationEditorLoad;
			_animationEditorLoad = null;
			if (load.IsFaulted)
			{
				_animationEditor = null;
				ShowMessage("The animation editor did not open: " + load.Exception?.GetBaseException().Message);
				return;
			}
			_animationEditor.Initialise();
			ShowAnimationEditor();
		}
		_animationEditor.Update();
		if (!_animationEditor.CloseRequested)
		{
			return;
		}
		_animationEditorOpen = false;
		_animationEditor.ApplyToGame();
		_input.Begin(_gameContext);
		if (_animationEditor.PlayRequested)
		{
			RequestPlay(EditorPlayRequest.FromCursor);
		}
		else
		{
			ShowMessage("The level shows the animation changes. N: animation editor. F5: play.");
		}
	}

	/// <summary>
	/// The animation of an object that the editor shows: of the animation instances in the fields of the object, the
	/// one with the most animations. For a character, that is the body and not the dust or the barrier.
	/// </summary>
	internal static AnimationInstance FindMainAnimation(object target)
	{
		List<AnimationInstance> found = new List<AnimationInstance>();
		for (Type type = target.GetType(); type != null && type != typeof(object); type = type.BaseType)
		{
			foreach (System.Reflection.FieldInfo field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
			{
				if (field.FieldType == typeof(AnimationInstance) && field.GetValue(target) is AnimationInstance instance && instance.AnimationGroup?.Resource != null)
				{
					found.Add(instance);
				}
			}
		}
		return found.OrderByDescending(instance => instance.AnimationGroup.Count).FirstOrDefault();
	}

	/// <summary>Shows or hides the hit boxes of the objects, in the editor and in the play.</summary>
	private void ToggleHitboxes()
	{
		LayerViewOptions options = _level.LayerViewOptions;
		options.ShowObjectCollision = !options.ShowObjectCollision;
		ShowMessage(options.ShowObjectCollision ? "Hit boxes on" : "Hit boxes off");
	}

	#endregion

	public void Save()
	{
		try
		{
			CompleteTraces();
			string path = EditorModFile.Save(GetChangedResources(), EditorModFile.GetPath(_gameContext.UserDataDirectory, _level.Binding.Resource.FullKeyPath));
			BindingDirty = false;
			MarkersDirty = false;
			TilesDirty = false;
			CollisionDirty = false;
			SolidityDirty = false;
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
		CompleteTraces();
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
		if (MarkersDirty || TilesDirty || CollisionDirty)
		{
			changes.Add(EditorModFile.Change.ForMap(_level.Map, tiles: TilesDirty, collision: CollisionDirty, markers: MarkersDirty));
		}
		if (SolidityDirty && TileCollision != null && GetTileCollisionResource() is Resource resource)
		{
			changes.Add(new EditorModFile.Change(resource, TileCollision.ToXml()));
		}
		return changes;
	}

	public void Undo()
	{
		EditorSnapshot snapshot = _history.Undo(CaptureSnapshot());
		if (snapshot != null)
		{
			Restore(snapshot, undo: true);
		}
	}

	public void Redo()
	{
		EditorSnapshot snapshot = _history.Redo(CaptureSnapshot());
		if (snapshot != null)
		{
			Restore(snapshot, undo: false);
		}
	}

	/// <summary>Keeps the state before a change, for undo. Call it before each change.</summary>
	internal void RecordHistory() => _history.Record(CaptureSnapshot());

	/// <summary>Keeps a state from before a change that is complete now, for example a stroke of tiles with its tile edits.</summary>
	internal void RecordHistory(EditorSnapshot before) => _history.Record(before);

	internal EditorSnapshot CaptureSnapshot()
	{
		return new EditorSnapshot
		{
			Placements = Placements.ToArray(),
			Selection = ObjectSelection.ToArray(),
			Markers = Markers.ToArray(),
			MarkerSelection = MarkerSelection.ToArray(),
			Vectors = Vectors.ToArray(),
			VectorSelection = VectorSelection.ToArray()
		};
	}

	private void Restore(EditorSnapshot snapshot, bool undo)
	{
		// Undo puts back the old tiles of the step. Redo puts back the new tiles.
		IEnumerable<TileEdit> edits = undo ? snapshot.TileEdits.Reverse() : snapshot.TileEdits;
		foreach (TileEdit edit in edits)
		{
			SetTile(edit.Layer, edit.Column, edit.Row, undo ? edit.OldTile : edit.NewTile, undo ? edit.OldSolidity : edit.NewSolidity);
		}
		if (snapshot.TileEdits.Length > 0)
		{
			TilesDirty = true;
			// The lines of the snapshot can be from before a trace that did not finish, so trace the cells again
			int layer = TileCollision?.Layer ?? -1;
			foreach (TileEdit edit in snapshot.TileEdits.Where(e => e.Layer == layer))
			{
				RequestTrace(new Rectanglei(edit.Column, edit.Row, 1, 1));
			}
		}
		if (!snapshot.Vectors.SequenceEqual(Vectors))
		{
			Vectors.Clear();
			foreach (CollisionVector vector in snapshot.Vectors)
			{
				Vectors.Add(vector);
			}
			CollisionDirty = true;
		}
		VectorSelection.Clear();
		VectorSelection.UnionWith(snapshot.VectorSelection);
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

	internal void MarkTilesChanged() => TilesDirty = true;

	internal void MarkCollisionChanged() => CollisionDirty = true;

	/// <summary>Sets a tile, and the solidity of the cell when the layer is the front layer of the tile collision.</summary>
	internal void SetTile(int layerIndex, int column, int row, int tile, byte solidity)
	{
		LevelLayer layer = _level.Map.Layers[layerIndex];
		if (column < 0 || row < 0 || column >= layer.Columns || row >= layer.Rows)
		{
			return;
		}
		layer.Tiles[column, row] = tile;
		if (TileCollision != null && layerIndex == TileCollision.Layer)
		{
			_tileGeneration++;
		}
		if (TileCollision != null && layerIndex == TileCollision.Layer && TileCollision.GetSolidity(column, row) != solidity)
		{
			TileCollision.SetSolidity(column, row, solidity);
			SolidityDirty = true;
		}
	}

	/// <summary>The front cells that changed since the last trace, or null.</summary>
	private Rectanglei? _pendingTraceCells;

	private Task<List<(Vector2i A, Vector2i B, uint Paths)>> _traceTask;

	private Rectanglei _traceTaskCells;

	/// <summary>Changes on each change of the front tiles, so that a trace of old tiles is not used.</summary>
	private int _tileGeneration;

	private int _traceTaskGeneration;

	/// <summary>
	/// The collision of the front cells in an area must follow the tiles again. A trace runs in the background, and
	/// <see cref="Update"/> uses the result when the tiles did not change in the meantime.
	/// </summary>
	internal void RequestTrace(Rectanglei cells)
	{
		if (TileCollision == null)
		{
			return;
		}
		_pendingTraceCells = _pendingTraceCells is Rectanglei pending ? Union(pending, cells) : cells;
	}

	private static Rectanglei Union(Rectanglei a, Rectanglei b)
	{
		int left = Math.Min(a.X, b.X);
		int top = Math.Min(a.Y, b.Y);
		return new Rectanglei(left, top, Math.Max(a.X + a.Width, b.X + b.Width) - left, Math.Max(a.Y + a.Height, b.Y + b.Height) - top);
	}

	private void UpdateTrace()
	{
		if (_traceTask != null && _traceTask.IsCompleted)
		{
			Task<List<(Vector2i A, Vector2i B, uint Paths)>> task = _traceTask;
			_traceTask = null;
			if (task.IsFaulted)
			{
				ShowMessage("The collision trace failed: " + task.Exception?.InnerException?.Message);
			}
			else if (_traceTaskGeneration == _tileGeneration)
			{
				ApplyTrace(task.Result, _traceTaskCells);
			}
			else
			{
				RequestTrace(_traceTaskCells);
			}
		}
		if (_traceTask == null && _pendingTraceCells is Rectanglei cells)
		{
			_pendingTraceCells = null;
			_traceTaskCells = cells;
			_traceTaskGeneration = _tileGeneration;
			// The trace reads copies, so that the user can paint while it runs
			TileCollision tileCollision = TileCollision.Clone();
			LevelLayer front = CopyLayer(_level.Map.Layers[TileCollision.Layer]);
			_traceTask = Task.Run(() => tileCollision.Trace(front));
		}
	}

	/// <summary>Waits for the traces that did not finish, for example before a save or a test play.</summary>
	internal void CompleteTraces()
	{
		if (_traceTask != null)
		{
			_traceTask.Wait();
			Task<List<(Vector2i A, Vector2i B, uint Paths)>> task = _traceTask;
			_traceTask = null;
			if (!task.IsFaulted && _traceTaskGeneration == _tileGeneration)
			{
				ApplyTrace(task.Result, _traceTaskCells);
			}
			else
			{
				RequestTrace(_traceTaskCells);
			}
		}
		if (_pendingTraceCells is Rectanglei cells)
		{
			_pendingTraceCells = null;
			ApplyTrace(TileCollision.Trace(_level.Map.Layers[TileCollision.Layer]), cells);
		}
	}

	private static LevelLayer CopyLayer(LevelLayer layer)
	{
		LevelLayer copy = new LevelLayer(layer.Map);
		copy.Resize(layer.Columns, layer.Rows);
		Array.Copy(layer.Tiles, copy.Tiles, layer.Tiles.Length);
		return copy;
	}

	/// <summary>Uses a trace of the whole map for the lines that cross an area of cells (see <see cref="CollisionDiff"/>).</summary>
	private void ApplyTrace(List<(Vector2i A, Vector2i B, uint Paths)> lines, Rectanglei cells)
	{
		(List<CollisionVector> removed, List<(Vector2i A, Vector2i B, uint Paths)> added) = CollisionDiff.Get(Vectors, lines, cells);
		if (added.Count == 0 && removed.Count == 0)
		{
			return;
		}
		foreach (CollisionVector vector in removed)
		{
			Vectors.Remove(vector);
			VectorSelection.Remove(vector);
		}
		foreach ((Vector2i a, Vector2i b, uint paths) in added)
		{
			Vectors.Add(new CollisionVector(a, b, paths));
		}
		CollisionDirty = true;
	}

	/// <summary>Traces all the lines of paths 1 and 2 again from the tiles, and replaces the lines that are different.</summary>
	/// <returns><c>true</c> if a line changed.</returns>
	internal bool RetraceAll()
	{
		if (TileCollision == null)
		{
			return false;
		}
		CompleteTraces();
		List<CollisionVector> before = Vectors.ToList();
		LevelLayer front = _level.Map.Layers[TileCollision.Layer];
		ApplyTrace(TileCollision.Trace(front), new Rectanglei(-1, -1, front.Columns + 2, front.Rows + 2));
		return !before.SequenceEqual(Vectors);
	}

	private Resource GetTileCollisionResource()
	{
		string mapKey = _level.Map.Resource?.FullKeyPath;
		if (mapKey == null || mapKey.LastIndexOf('/') < 0)
		{
			return null;
		}
		return _gameContext.ResourceTree[mapKey.Substring(0, mapKey.LastIndexOf('/') + 1) + TileCollision.ResourceName]?.Resource;
	}

	private TileCollision LoadTileCollision()
	{
		Resource resource = GetTileCollisionResource();
		if (resource == null)
		{
			return null;
		}
		try
		{
			using Stream stream = resource.Source.ReadUncompressed();
			TileCollision tileCollision = TileCollision.FromXml(stream);
			return tileCollision.Layer < _level.Map.Layers.Count ? tileCollision : null;
		}
		catch (Exception ex) when (ex is IOException || ex is FormatException || ex is System.Xml.XmlException || ex is InvalidDataException || ex is ArgumentException)
		{
			System.Diagnostics.Trace.WriteLine("The tile collision did not load: " + ex.Message);
			return null;
		}
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
		Add("Animations (N)", OpenAnimationEditor);
		Add("Hit boxes (H)", ToggleHitboxes, active: _level.LayerViewOptions.ShowObjectCollision);
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
		if (_tool.HasCustomProperties)
		{
			_tool.OnPropertiesClick(Mouse, PropertiesArea, direction);
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
			_entry.Begin(row.Label, row.Value);
		}
	}

	/// <summary>The area of the properties panel under its heading.</summary>
	private static Rectangle PropertiesArea => new Rectangle(ScreenWidth - PanelWidth, ListBottom + RowHeight, PanelWidth, ScreenHeight - StatusBarHeight - ListBottom - RowHeight);

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
		if (_animationEditorOpen)
		{
			_animationEditor.Draw(renderer);
			return;
		}
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

	internal static void DrawCross(I2dRenderer g, Vector2 centre, double size, Colour colour, double thickness) => EditorUi.DrawCross(g, centre, size, colour, thickness);

	/// <summary>Draws a text on a dark box, for labels in the level.</summary>
	internal void DrawLabel(Renderer renderer, I2dRenderer g, string text, double x, double y, Colour colour) => _ui.DrawLabel(renderer, g, text, x, y, colour);

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
			_ui.DrawButton(renderer, g, button.Bounds, button.Text, button.Active, hover, button.Enabled);
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
			_ui.DrawListRow(renderer, g, listRows[i], new Rectangle(left, y, PanelWidth, RowHeight), Mouse);
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);

		DrawHeading(renderer, g, _tool.PropertiesHeading, left, ListBottom);
		if (_tool.HasCustomProperties)
		{
			g.ClipRectangle = PropertiesArea;
			_tool.DrawProperties(renderer, g, PropertiesArea);
			renderer.DeativateRenderer();
			g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
			return;
		}
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
			_ui.DrawPropertyRow(renderer, g, rows[i], new Rectangle(left, y, PanelWidth, RowHeight), Mouse, _entry, _frames);
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
	}

	private void DrawHeading(Renderer renderer, I2dRenderer g, string text, double left, double top) => _ui.DrawHeading(renderer, g, text, left, top);

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

	private double TextHeight => _ui.TextHeight;

	internal double MeasureText(string text) => _ui.MeasureText(text);

	internal void DrawText(Renderer renderer, string text, double x, double y, Colour colour) => _ui.DrawText(renderer, text, x, y, colour);

	#endregion
}
