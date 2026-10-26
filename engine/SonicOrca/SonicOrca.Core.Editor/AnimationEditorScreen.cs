using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Core.Debugging;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Menu;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>
/// The animation editor ("--animations [KEY]"). The left panel lists the animation groups of the game and the mods.
/// The centre plays the selected animation, and the strip under it shows its frames. The right panel lists the
/// animations of the group, and the properties of the animation and of the selected frame.
/// <para>
/// The editor changes the timing and the order of the frames, and adds and removes frames and animations. In the
/// preview, a drag moves the offset of the frame. The texture view (T) shows the texture of the frame with the
/// rectangles of the frames: a drag moves, resizes or draws the rectangle of the selected frame. The textures section
/// of the properties copies a texture into the folder of the editor, where an image editor can change it; the editor
/// watches these files and shows a changed texture at once. Ctrl+S
/// writes the changed groups into the folder mods/editor/animations of the user data folder, in the layout of
/// "s2hd-data unpack". The game loads this folder after the other mods.
/// </para>
/// </summary>
public sealed class AnimationEditorScreen : Screen
{
	/// <summary>The folder in the editor folder of the mods folder.</summary>
	public const string FolderName = "animations";

	private const double ScreenWidth = 1920.0;

	private const double ScreenHeight = 1080.0;

	/// <summary>The animations use the top part of the right panel, down to this y value. The properties use the rest.</summary>
	private const double ListBottom = 470.0;

	private const double StripHeight = 170.0;

	private const double StripBoxWidth = 110.0;

	private const double StripGap = 8.0;

	private const int MessageFrames = 240;

	/// <summary>The editor looks for changed texture files at this interval, in frames.</summary>
	private const int WatchFrames = 30;

	private static readonly double[] Zooms = { 0.5, 1.0, 2.0, 3.0, 4.0 };

	private static readonly double[] TextureZooms = { 0.25, 0.5, 1.0, 2.0, 4.0, 8.0 };

	/// <summary>The size of the handles of the selected rectangle, and the distance at which the mouse takes an edge.</summary>
	private const double HandleSize = 10.0;

	/// <summary>The opacity of the previous frame in the preview (onion skin).</summary>
	private const double OnionOpacity = 0.3;

	private static readonly Colour RectangleColour = new Colour(0.5, 1.0, 1.0, 1.0);

	private static readonly Colour SelectedRectangleColour = new Colour(1.0, 1.0, 0.8, 0.1);

	private enum View
	{
		/// <summary>The frame at the position of the object, as in the game.</summary>
		Preview,
		/// <summary>The texture of the frame, with the rectangles of the frames of the animation.</summary>
		Texture
	}

	private enum DragMode
	{
		None,
		/// <summary>Moves the offset of the frame, or of all the frames with Ctrl.</summary>
		Offset,
		MoveRectangle,
		/// <summary>Moves the edges of the rectangle in <see cref="_resizeEdges"/>.</summary>
		ResizeRectangle,
		/// <summary>Draws a new rectangle from the point where the drag started.</summary>
		DrawRectangle,
		Pan
	}

	[Flags]
	private enum Edges
	{
		None = 0,
		Left = 1,
		Right = 2,
		Top = 4,
		Bottom = 8
	}

	private static readonly Colour PreviewColour = new Colour(1.0, 0.11, 0.12, 0.15);

	private static readonly Colour GridColour = new Colour(1.0, 0.17, 0.19, 0.23);

	private static readonly Colour FrameOutlineColour = new Colour(0.6, 0.4, 0.85, 1.0);

	/// <summary>The colour of the hit boxes, as the collision view of the level draws them.</summary>
	private static readonly Colour HitboxColour = new Colour(0.9, 1.0, 0.0, 0.0);

	private readonly SonicOrcaGameContext _gameContext;

	private readonly ResourceSession _fontSession;

	private readonly EditorUi _ui = new EditorUi();

	private readonly EditorTextEntry _entry = new EditorTextEntry();

	private readonly EditorInput _input = new EditorInput();

	private readonly string _initialKey;

	/// <summary>The level editor runs the editor (see <see cref="Open"/>): Escape goes back to the level.</summary>
	private readonly bool _hosted;

	/// <summary>The groups that changed since the editor opened, saved or not. The running level takes their changes (see <see cref="ApplyToGame"/>).</summary>
	private readonly HashSet<string> _changed = new HashSet<string>();

	/// <summary>The hit boxes of the object that the level editor opened the editor for, relative to its position.</summary>
	private IReadOnlyList<Rectanglei> _hitboxes = Array.Empty<Rectanglei>();

	/// <summary>The group of the object of <see cref="_hitboxes"/>. The preview of another group draws no hit boxes.</summary>
	private string _hitboxGroupKey;

	/// <summary>The game read the texture files of the folder before this time; a file that changed later is reloaded.</summary>
	private readonly DateTime _startTime = DateTime.UtcNow;

	/// <summary>The time of the last change of each texture file that the editor read or wrote.</summary>
	private readonly Dictionary<string, DateTime> _fileTimes = new Dictionary<string, DateTime>();

	/// <summary>The textures that the editor loads from changed files.</summary>
	private readonly List<Task<(string Key, ITexture Texture, byte[] Data)>> _reloads = new List<Task<(string, ITexture, byte[])>>();

	/// <summary>The textures that the editor made from files. It disposes them when they are replaced.</summary>
	private readonly HashSet<ITexture> _ownedTextures = new HashSet<ITexture>();

	/// <summary>The keys of the textures of the group, in the order of <see cref="AnimationGroupDocument.Textures"/>.</summary>
	private List<string> _textureKeys = new List<string>();

	/// <summary>The keys of all the animation groups, in order.</summary>
	private List<string> _groupKeys = new List<string>();

	/// <summary>The documents that the editor read. A document keeps its changes when the user selects another group.</summary>
	private readonly Dictionary<string, AnimationGroupDocument> _documents = new Dictionary<string, AnimationGroupDocument>();

	/// <summary>The groups with changes that are not saved.</summary>
	private readonly HashSet<string> _dirty = new HashSet<string>();

	private readonly Dictionary<string, Stack<Snapshot>> _undo = new Dictionary<string, Stack<Snapshot>>();

	private readonly Dictionary<string, Stack<Snapshot>> _redo = new Dictionary<string, Stack<Snapshot>>();

	private string _filter = "";

	private string _groupKey;

	private AnimationGroupDocument _document;

	/// <summary>The textures of the group, in the order of <see cref="AnimationGroupDocument.Textures"/>; null for a texture that is not there.</summary>
	private List<ITexture> _textures = new List<ITexture>();

	private ResourceSession _textureSession;

	private Task<(ResourceSession Session, List<ITexture> Textures)> _textureTask;

	private int _animationIndex;

	private int _frameIndex;

	private bool _playing = true;

	private int _playFrame;

	private int _playTicks;

	private int _zoomIndex = 1;

	private View _view = View.Preview;

	private bool _onionSkin = true;

	private int _textureZoomIndex = 2;

	/// <summary>The position of the top left of the texture in the texture view, from the top left of the view.</summary>
	private Vector2 _texturePan = new Vector2(20.0, 20.0);

	private DragMode _drag;

	private Vector2 _dragStart;

	private Vector2 _dragStartPan;

	/// <summary>The values of the frames of the animation when the drag started.</summary>
	private List<AnimationFrameDocument> _dragFrames;

	private Edges _resizeEdges;

	/// <summary>True after the drag changed the document: the undo history has its step.</summary>
	private bool _dragChanged;

	private double _groupScroll;

	private double _animationScroll;

	private double _propertyScroll;

	private double _stripScroll;

	private int _frames;

	private string _message;

	private int _messageFrames;

	/// <summary>True after the first Escape with changes that are not saved: a second Escape leaves.</summary>
	private bool _confirmLeave;

	private sealed record Snapshot(AnimationGroupDocument Document, int Animation, int Frame);

	/// <param name="initialKey">The key of the animation group to open, or null for the first group.</param>
	public AnimationEditorScreen(SonicOrcaGameContext gameContext, string initialKey = null, bool hosted = false)
	{
		_gameContext = gameContext;
		_fontSession = new ResourceSession(gameContext.ResourceTree);
		_initialKey = initialKey;
		_hosted = hosted;
	}

	/// <summary>The user pressed Escape or F5 in the editor of a level (see <see cref="Open"/>).</summary>
	public bool CloseRequested { get; private set; }

	/// <summary>The user pressed F5 in the editor of a level: the level plays after the editor closes.</summary>
	public bool PlayRequested { get; private set; }

	private AnimationDocument Animation => _document != null && _animationIndex < _document.Animations.Count ? _document.Animations[_animationIndex] : null;

	private AnimationFrameDocument Frame
	{
		get
		{
			AnimationDocument animation = Animation;
			return animation != null && _frameIndex < animation.Frames.Count ? animation.Frames[_frameIndex] : null;
		}
	}

	private double Zoom => Zooms[_zoomIndex];

	private double TextureZoom => TextureZooms[_textureZoomIndex];

	private ITexture FrameTexture
	{
		get
		{
			AnimationFrameDocument frame = Frame;
			return frame != null && frame.Texture >= 0 && frame.Texture < _textures.Count ? _textures[frame.Texture] : null;
		}
	}

	private static Rectangle PreviewArea => new Rectangle(EditorUi.PanelWidth, EditorUi.TopBarHeight, ScreenWidth - EditorUi.PanelWidth * 2.0,
		ScreenHeight - EditorUi.StatusBarHeight - StripHeight - EditorUi.TopBarHeight);

	private static Rectangle StripArea => new Rectangle(EditorUi.PanelWidth, ScreenHeight - EditorUi.StatusBarHeight - StripHeight, ScreenWidth - EditorUi.PanelWidth * 2.0, StripHeight);

	/// <summary>The folder where the editor writes the groups.</summary>
	public static string GetFolder(string userDataDirectory) => Path.Combine(EditorModFile.GetFolder(userDataDirectory), FolderName);

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default)
	{
		_fontSession.PushDependency(EditorUi.FontResourceKey);
		await _fontSession.LoadAsync(ct);
		Font font = _gameContext.ResourceTree.GetLoadedResource<Font>(EditorUi.FontResourceKey);
		// Another part of the game can have loaded the font without its texture. This session loaded the texture, but it
		// calls OnLoaded only for the resources that it loaded first, so the font takes its texture here.
		if (font.ShapeTexture == null)
		{
			font.OnLoaded();
		}
		_ui.Font = font;
		_groupKeys = _gameContext.ResourceTree.GetResourceListing()
			.Where(item => item.Value.Identifier == ResourceTypeIdentifier.AnimationGroup)
			.Select(item => item.Key)
			.OrderBy(key => key, StringComparer.Ordinal)
			.ToList();
	}

	public override void Initialise()
	{
		_input.Begin(_gameContext);
		string first = _initialKey != null && _groupKeys.Contains(_initialKey) ? _initialKey : _groupKeys.FirstOrDefault();
		if (first != null)
		{
			SelectGroup(first);
		}
		if (_initialKey != null && first != _initialKey)
		{
			ShowMessage("No animation group " + _initialKey);
		}
	}

	public override void Unload()
	{
		if (_hosted)
		{
			ReleaseGameTextures();
		}
		foreach (ITexture texture in _ownedTextures)
		{
			texture.Dispose();
		}
		_ownedTextures.Clear();
		_textureSession?.Dispose();
		_fontSession.Dispose();
	}

	/// <summary>
	/// Shows a group and one of its animations, for the level editor. The hit boxes are drawn in the preview. The
	/// documents, the changes and the undo history stay from the last time that the editor was open.
	/// </summary>
	public void Open(string groupKey, int animationIndex, IReadOnlyList<Rectanglei> hitboxes)
	{
		_input.Begin(_gameContext);
		CloseRequested = false;
		PlayRequested = false;
		_confirmLeave = false;
		_hitboxes = hitboxes ?? Array.Empty<Rectanglei>();
		_hitboxGroupKey = groupKey;
		if (groupKey != null && _groupKeys.Contains(groupKey))
		{
			if (groupKey != _groupKey)
			{
				_filter = "";
				SelectGroup(groupKey);
			}
			ScrollToGroup();
			SelectAnimation(animationIndex);
		}
		ShowMessage(_hitboxes.Count > 0 ? "Red boxes: the hit boxes of the object. Esc: back to the level. F5: play." : "Esc: back to the level. F5: play.");
	}

	/// <summary>
	/// Gives the changed groups to the game, so that the running level shows them: the loaded groups take the new
	/// animations and textures, and the resources take the new files for the groups that load again.
	/// </summary>
	public void ApplyToGame()
	{
		ResourceTree tree = _gameContext.ResourceTree;
		foreach (string key in _changed)
		{
			AnimationGroupDocument document = _documents[key];
			Resource resource = tree[key]?.Resource;
			// Save gives the resource the saved file; a group with changes that are not saved gives its changes
			if (resource != null && _dirty.Contains(key))
			{
				resource.Source = new MemoryResourceSource(Encoding.UTF8.GetBytes(document.ToXml()));
			}
			if (!tree.TryGetLoadedResource(key, out AnimationGroup group))
			{
				continue;
			}
			List<ITexture> textures = GetGameTextures(key, document, group);
			if (textures.Count > 0 && textures.All(texture => texture != null))
			{
				group.Replace(textures, document.ToAnimations(group, textures.Count));
			}
		}
	}

	/// <summary>The textures for a group of the game: the textures of the editor, else the textures that the group has, else the loaded resources.</summary>
	private List<ITexture> GetGameTextures(string key, AnimationGroupDocument document, AnimationGroup group)
	{
		Resource resource = _gameContext.ResourceTree[key]?.Resource;
		List<ITexture> textures = new List<ITexture>();
		for (int i = 0; i < document.Textures.Count; i++)
		{
			ITexture texture = key == _groupKey && _textureTask == null && i < _textures.Count ? _textures[i] : null;
			texture ??= i < group.Textures.Count ? group.Textures[i] : null;
			string textureKey = resource?.GetAbsolutePath(document.Textures[i]);
			if (texture == null && textureKey != null && _gameContext.ResourceTree.TryGetLoadedResource(textureKey, out ITexture loaded))
			{
				texture = loaded;
			}
			textures.Add(texture ?? group.Textures.FirstOrDefault());
		}
		return textures;
	}

	/// <summary>
	/// The editor disposes the textures that it made from files. The groups of the game that use them take the
	/// textures of the resources again.
	/// </summary>
	private void ReleaseGameTextures()
	{
		foreach (string key in _changed)
		{
			if (!_gameContext.ResourceTree.TryGetLoadedResource(key, out AnimationGroup group) || !group.Textures.Any(_ownedTextures.Contains))
			{
				continue;
			}
			Resource resource = _gameContext.ResourceTree[key]?.Resource;
			IReadOnlyList<string> keys = _documents[key].Textures;
			List<ITexture> textures = new List<ITexture>();
			for (int i = 0; i < group.Textures.Count; i++)
			{
				ITexture texture = group.Textures[i];
				string textureKey = i < keys.Count ? resource?.GetAbsolutePath(keys[i]) : null;
				if (_ownedTextures.Contains(texture))
				{
					texture = textureKey != null && _gameContext.ResourceTree.TryGetLoadedResource(textureKey, out ITexture loaded) ? loaded : null;
				}
				textures.Add(texture);
			}
			ITexture fallback = textures.FirstOrDefault(texture => texture != null);
			if (fallback != null)
			{
				group.Replace(textures.Select(texture => texture ?? fallback), group);
			}
		}
	}

	/// <summary>Scrolls the group list so that the selected group is in the middle.</summary>
	private void ScrollToGroup()
	{
		int index = VisibleGroups.ToList().IndexOf(_groupKey);
		double height = ScreenHeight - EditorUi.StatusBarHeight - GroupListTop;
		_groupScroll = Math.Max(0.0, index * EditorUi.RowHeight - (height - EditorUi.RowHeight) / 2.0);
	}

	#region Groups

	private IEnumerable<string> VisibleGroups => _filter.Length == 0
		? _groupKeys
		: _groupKeys.Where(key => key.Contains(_filter, StringComparison.OrdinalIgnoreCase));

	private void SelectGroup(string key)
	{
		_entry.Cancel();
		_groupKey = key;
		if (!_documents.TryGetValue(key, out AnimationGroupDocument document))
		{
			try
			{
				using Stream stream = _gameContext.ResourceTree[key].Resource.Source.ReadUncompressed();
				document = AnimationGroupDocument.Load(stream);
			}
			catch (Exception ex) when (ex is FormatException or System.Xml.XmlException or IOException)
			{
				ShowMessage($"Cannot read {key}: {ex.Message}");
				document = new AnimationGroupDocument();
			}
			_documents[key] = document;
		}
		_document = document;
		_animationIndex = 0;
		_frameIndex = 0;
		_animationScroll = 0.0;
		RestartPlayback();
		LoadTextures();
	}

	/// <summary>Loads the textures of the group in the background, with their own resource session.</summary>
	private void LoadTextures()
	{
		Resource resource = _gameContext.ResourceTree[_groupKey]?.Resource;
		List<string> keys = _document.Textures.Select(texture => resource?.GetAbsolutePath(texture)).ToList();
		_textureKeys = keys;
		ResourceSession session = new ResourceSession(_gameContext.ResourceTree);
		foreach (string key in keys.Where(k => k != null && _gameContext.ResourceTree[k]?.Resource != null))
		{
			session.PushDependency(key);
		}
		_textureTask = Task.Run(async () =>
		{
			await session.LoadAsync();
			List<ITexture> textures = keys.Select(k => k != null && _gameContext.ResourceTree.TryGetLoadedResource(k, out ITexture texture) ? texture : null).ToList();
			return (session, textures);
		});
	}

	private void UpdateTextureLoad()
	{
		if (_textureTask == null || !_textureTask.IsCompleted)
		{
			return;
		}
		ResourceSession old = _textureSession;
		if (_textureTask.IsFaulted)
		{
			ShowMessage("Cannot load the textures: " + _textureTask.Exception?.GetBaseException().Message);
			_textures = new List<ITexture>();
			_textureSession = null;
		}
		else
		{
			(_textureSession, _textures) = _textureTask.Result;
		}
		_textureTask = null;
		old?.Dispose();
		WatchTextureFiles();
	}

	/// <summary>The folder where the editor writes the files.</summary>
	private string Folder => GetFolder(_gameContext.UserDataDirectory);

	/// <summary>A path in the folder of the editor, from the user data folder, for the messages.</summary>
	private string ShortPath(string path) => Path.GetRelativePath(_gameContext.UserDataDirectory, path).Replace('\\', '/');

	/// <summary>Reloads each texture of the group whose file changed, or was written after the game read the files.</summary>
	private void WatchTextureFiles()
	{
		if (_textureTask != null)
		{
			return;
		}
		foreach (string key in _textureKeys.Where(k => k != null))
		{
			string path = AnimationTextureFiles.GetPath(Folder, _gameContext.ResourceTree, key);
			if (!File.Exists(path))
			{
				continue;
			}
			DateTime time = File.GetLastWriteTimeUtc(path);
			bool known = _fileTimes.TryGetValue(path, out DateTime last);
			if ((known && time != last) || (!known && time > _startTime))
			{
				ReloadTexture(key, path);
			}
			else if (!known)
			{
				_fileTimes[path] = time;
			}
		}
	}

	/// <summary>Reads a texture file and makes a texture from it in the background. A file that is not ready yet is read at the next look.</summary>
	private void ReloadTexture(string key, string path)
	{
		DateTime time = File.GetLastWriteTimeUtc(path);
		byte[] data = AnimationTextureFiles.TryRead(path);
		if (data == null || !AnimationTextureFiles.IsPng(data))
		{
			return;
		}
		_fileTimes[path] = time;
		ResourceTree tree = _gameContext.ResourceTree;
		_reloads.Add(Task.Run(async () =>
		{
			Resource resource = new Resource(key, ResourceTypeIdentifier.TexturePNG, new MemoryResourceSource(data));
			ILoadedResource texture = await ResourceType.FromIdentifier(ResourceTypeIdentifier.TexturePNG)
				.LoadAsync(new ResourceLoadArgs(tree, resource, new MemoryStream(data)));
			return (key, (ITexture)texture, data);
		}));
	}

	/// <summary>Puts the textures that finished loading in place, and gives the game their new data for the later loads.</summary>
	private void UpdateReloads()
	{
		for (int i = _reloads.Count - 1; i >= 0; i--)
		{
			Task<(string Key, ITexture Texture, byte[] Data)> task = _reloads[i];
			if (!task.IsCompleted)
			{
				continue;
			}
			_reloads.RemoveAt(i);
			if (task.IsFaulted)
			{
				ShowMessage("Cannot read the texture: " + task.Exception?.GetBaseException().Message);
				continue;
			}
			(string key, ITexture texture, byte[] data) = task.Result;
			SetResourceData(key, data);
			int index = _textureKeys.IndexOf(key);
			if (index < 0 || index >= _textures.Count)
			{
				texture.Dispose();
				continue;
			}
			if (_textures[index] != null && _ownedTextures.Remove(_textures[index]))
			{
				_textures[index].Dispose();
			}
			_textures[index] = texture;
			_ownedTextures.Add(texture);
			_changed.Add(_groupKey);
			ShowMessage($"Reloaded {key.Split('/')[^1]} ({texture.Width} x {texture.Height})");
		}
	}

	/// <summary>Gives a texture resource of the game new data, or adds the resource.</summary>
	private void SetResourceData(string key, byte[] data)
	{
		Resource resource = _gameContext.ResourceTree[key]?.Resource;
		if (resource != null)
		{
			resource.Source = new MemoryResourceSource(data);
		}
		else
		{
			_gameContext.ResourceTree.SetOrAdd(key, new Resource(key, ResourceTypeIdentifier.TexturePNG, new MemoryResourceSource(data)));
		}
	}

	/// <summary>
	/// Copies a texture into the folder of the editor, when it is not there yet, and shows the file in the file
	/// manager. An image editor can then change it: the editor shows the change when the file is saved.
	/// </summary>
	private void EditTexture(int index)
	{
		string key = index < _textureKeys.Count ? _textureKeys[index] : null;
		Resource resource = key != null ? _gameContext.ResourceTree[key]?.Resource : null;
		if (resource == null)
		{
			ShowMessage("The texture " + (key ?? index.ToString(CultureInfo.InvariantCulture)) + " is not in the game data");
			return;
		}
		string path = AnimationTextureFiles.GetPath(Folder, _gameContext.ResourceTree, key);
		try
		{
			if (!File.Exists(path))
			{
				using Stream source = resource.Source.ReadUncompressed();
				using MemoryStream memory = new MemoryStream();
				source.CopyTo(memory);
				AnimationTextureFiles.Write(path, memory.ToArray());
				_fileTimes[path] = File.GetLastWriteTimeUtc(path);
			}
		}
		catch (IOException ex)
		{
			ShowMessage("Cannot write the texture: " + ex.Message);
			return;
		}
		// Scripted tests do not open windows of the file manager
		bool shown = TestHooks.EditorInput == null && AnimationTextureFiles.ShowInFileManager(path);
		ShowMessage($"Edit {ShortPath(path)}{(shown ? "" : " in the user data folder")}: the editor shows the changes when you save the file");
	}

	/// <summary>Copies a PNG file over a texture of the group, in the folder of the editor.</summary>
	private bool ReplaceTexture(int index, string source)
	{
		byte[] data = ReadPngFile(source);
		string key = index < _textureKeys.Count ? _textureKeys[index] : null;
		if (data == null || key == null)
		{
			return false;
		}
		string path = AnimationTextureFiles.GetPath(Folder, _gameContext.ResourceTree, key);
		AnimationTextureFiles.Write(path, data);
		ReloadTexture(key, path);
		return true;
	}

	/// <summary>Adds a PNG file to the group as a new texture, in the folder of the editor. Its name comes from the file name.</summary>
	private bool AddTexture(string source)
	{
		byte[] data = ReadPngFile(source);
		Resource group = _gameContext.ResourceTree[_groupKey]?.Resource;
		if (data == null || group == null)
		{
			return false;
		}
		string baseName = AnimationTextureFiles.MakeTextureName(source.Trim().Trim('"'));
		string name = baseName;
		for (int n = 2; _document.Textures.Contains("//" + name) || _gameContext.ResourceTree[group.GetAbsolutePath("//" + name)]?.Resource != null; n++)
		{
			name = baseName + "_" + n.ToString(CultureInfo.InvariantCulture);
		}
		string key = group.GetAbsolutePath("//" + name);
		string path = AnimationTextureFiles.GetPath(Folder, _gameContext.ResourceTree, key);
		AnimationTextureFiles.Write(path, data);
		_fileTimes[path] = File.GetLastWriteTimeUtc(path);
		SetResourceData(key, data);
		Change(() => _document.Textures.Add("//" + name));
		LoadTextures();
		ShowMessage($"Added the texture {name} as texture {_document.Textures.Count - 1}: set it on the frames that use it");
		return true;
	}

	private byte[] ReadPngFile(string source)
	{
		string path = source.Trim().Trim('"');
		byte[] data = File.Exists(path) ? AnimationTextureFiles.TryRead(path) : null;
		if (data == null || !AnimationTextureFiles.IsPng(data))
		{
			ShowMessage("Not a PNG file: " + path);
			return null;
		}
		return data;
	}

	#endregion

	#region Update

	public override void Update()
	{
		_input.Read(_gameContext);
		_frames++;
		if (_messageFrames > 0)
		{
			_messageFrames--;
		}
		UpdateTextureLoad();
		UpdateReloads();
		if (_frames % WatchFrames == 0)
		{
			WatchTextureFiles();
		}
		if (_entry.IsActive)
		{
			UpdateTextEntry();
		}
		else
		{
			UpdateKeyboard();
		}
		UpdateMouse();
		UpdatePlayback();
	}

	private bool IsControlDown => _input.IsKeyDown(KeyboardState.KEY_LCTRL) || _input.IsKeyDown(KeyboardState.KEY_RCTRL) ||
		_input.IsKeyDown(KeyboardState.KEY_LGUI) || _input.IsKeyDown(KeyboardState.KEY_RGUI);

	private bool IsShiftDown => _input.IsKeyDown(KeyboardState.KEY_LSHIFT) || _input.IsKeyDown(KeyboardState.KEY_RSHIFT);

	private bool IsKeyPressed(int key) => _input.IsKeyPressed(key);

	private void UpdateTextEntry()
	{
		if (_entry.Row == FilterRow)
		{
			// The list follows the filter while the user types
			if (_entry.Update(_input) || IsKeyPressed(KeyboardState.KEY_ESCAPE))
			{
				_entry.Cancel();
			}
			_filter = _entry.Text;
			_groupScroll = 0.0;
			return;
		}
		if (_entry.Update(_input))
		{
			EditorPropertyRow row = GetPropertyRows().FirstOrDefault(r => r.Label == _entry.Row);
			string text = _entry.Text;
			_entry.Cancel();
			if (row?.Commit != null && !row.Commit(text))
			{
				ShowMessage("Value not accepted: " + text);
			}
		}
		else if (IsKeyPressed(KeyboardState.KEY_ESCAPE))
		{
			_entry.Cancel();
		}
	}

	private void UpdateKeyboard()
	{
		if (_gameContext.Console.IsOpen)
		{
			return;
		}
		if (IsControlDown)
		{
			if (IsKeyPressed(KeyboardState.KEY_S))
			{
				Save();
			}
			else if (IsKeyPressed(KeyboardState.KEY_Z))
			{
				if (IsShiftDown)
				{
					Redo();
				}
				else
				{
					Undo();
				}
			}
			else if (IsKeyPressed(KeyboardState.KEY_Y))
			{
				Redo();
			}
			else if (IsKeyPressed(KeyboardState.KEY_D))
			{
				DuplicateFrame();
			}
			else if (IsKeyPressed(KeyboardState.KEY_LEFT))
			{
				MoveFrame(-1);
			}
			else if (IsKeyPressed(KeyboardState.KEY_RIGHT))
			{
				MoveFrame(1);
			}
			return;
		}
		if (_hosted && (IsKeyPressed(KeyboardState.KEY_ESCAPE) || IsKeyPressed(KeyboardState.KEY_F5)))
		{
			// The changes stay in the editor and in the level, saved or not
			PlayRequested = IsKeyPressed(KeyboardState.KEY_F5);
			CloseRequested = true;
			return;
		}
		if (IsKeyPressed(KeyboardState.KEY_ESCAPE))
		{
			if (_dirty.Count == 0 || _confirmLeave)
			{
				Finish();
				return;
			}
			_confirmLeave = true;
			ShowMessage("Changes not saved. Press Escape again to leave without saving, or Ctrl+S to save.");
			return;
		}
		if (IsKeyPressed(KeyboardState.KEY_SPACE))
		{
			TogglePlay();
		}
		else if (IsAltDown && UpdateNudge())
		{
		}
		else if (IsKeyPressed(KeyboardState.KEY_LEFT))
		{
			StepFrame(-1);
		}
		else if (IsKeyPressed(KeyboardState.KEY_RIGHT))
		{
			StepFrame(1);
		}
		else if (IsKeyPressed(KeyboardState.KEY_UP))
		{
			SelectAnimation(_animationIndex - 1);
		}
		else if (IsKeyPressed(KeyboardState.KEY_DOWN))
		{
			SelectAnimation(_animationIndex + 1);
		}
		else if (IsKeyPressed(KeyboardState.KEY_T))
		{
			ToggleView();
		}
		else if (IsKeyPressed(KeyboardState.KEY_O))
		{
			_onionSkin = !_onionSkin;
		}
		else if (IsKeyPressed(KeyboardState.KEY_INSERT))
		{
			AddFrame();
		}
		else if (IsKeyPressed(KeyboardState.KEY_DELETE) || IsKeyPressed(KeyboardState.KEY_BACKSPACE))
		{
			DeleteFrame();
		}
		else if (IsKeyPressed(KeyboardState.KEY_Z))
		{
			CycleZoom();
		}
	}

	private bool IsAltDown => _input.IsKeyDown(KeyboardState.KEY_LALT) || _input.IsKeyDown(KeyboardState.KEY_RALT);

	/// <summary>
	/// Alt and an arrow key move the offset of the frame (preview) or its rectangle (texture view) by one pixel. With
	/// Shift in the texture view, they change the size of the rectangle.
	/// </summary>
	private bool UpdateNudge()
	{
		int dx = IsKeyPressed(KeyboardState.KEY_LEFT) ? -1 : IsKeyPressed(KeyboardState.KEY_RIGHT) ? 1 : 0;
		int dy = IsKeyPressed(KeyboardState.KEY_UP) ? -1 : IsKeyPressed(KeyboardState.KEY_DOWN) ? 1 : 0;
		AnimationFrameDocument frame = Frame;
		if ((dx == 0 && dy == 0) || frame == null)
		{
			return false;
		}
		_playing = false;
		if (_view == View.Preview)
		{
			Change(() =>
			{
				frame.OffsetX += dx;
				frame.OffsetY += dy;
			});
		}
		else if (IsShiftDown)
		{
			Change(() =>
			{
				frame.Width = Math.Max(1, frame.Width + dx);
				frame.Height = Math.Max(1, frame.Height + dy);
			});
		}
		else
		{
			Change(() =>
			{
				frame.X = Math.Max(0, frame.X + dx);
				frame.Y = Math.Max(0, frame.Y + dy);
			});
		}
		return true;
	}

	private void ToggleView()
	{
		_view = _view == View.Preview ? View.Texture : View.Preview;
		_playing = false;
		if (_view == View.Texture)
		{
			CentreTextureOnFrame();
		}
	}

	private void CycleZoom()
	{
		if (_view == View.Texture)
		{
			_textureZoomIndex = (_textureZoomIndex + 1) % TextureZooms.Length;
			CentreTextureOnFrame();
		}
		else
		{
			_zoomIndex = (_zoomIndex + 1) % Zooms.Length;
		}
	}

	/// <summary>Moves the texture view so that the rectangle of the selected frame is in the centre.</summary>
	private void CentreTextureOnFrame()
	{
		AnimationFrameDocument frame = Frame;
		if (frame == null)
		{
			return;
		}
		Rectangle area = PreviewArea;
		_texturePan = new Vector2(
			Math.Round(area.Width / 2.0 - (frame.X + frame.Width / 2.0) * TextureZoom),
			Math.Round(area.Height / 2.0 - (frame.Y + frame.Height / 2.0) * TextureZoom));
	}

	private void UpdateMouse()
	{
		Vector2 mouse = _input.Mouse;
		if (_drag != DragMode.None)
		{
			UpdateDrag(mouse);
			if (!_input.Left && !_input.Right && !_input.Middle)
			{
				_drag = DragMode.None;
			}
			return;
		}
		if (_input.Wheel != 0.0)
		{
			UpdateWheel(mouse, _input.Wheel);
		}
		if (PreviewArea.Contains(mouse) && (_input.LeftPressed || _input.RightPressed || _input.MiddlePressed))
		{
			if (_entry.IsActive && _entry.Row != FilterRow)
			{
				UpdateTextEntryCommitOnClick();
			}
			BeginDrag(mouse);
			return;
		}
		int direction = _input.LeftPressed ? 1 : _input.RightPressed ? -1 : 0;
		if (direction == 0)
		{
			return;
		}
		if (mouse.Y < EditorUi.TopBarHeight)
		{
			if (direction == 1)
			{
				GetTopBarButtons().FirstOrDefault(b => b.Enabled && b.Bounds.Contains(mouse))?.Action();
			}
			return;
		}
		// A click outside the row that the user types into keeps the typed value
		if (_entry.IsActive && _entry.Row != FilterRow)
		{
			UpdateTextEntryCommitOnClick();
		}
		if (mouse.X < EditorUi.PanelWidth)
		{
			OnGroupPanelClick(mouse);
		}
		else if (mouse.X >= ScreenWidth - EditorUi.PanelWidth)
		{
			OnAnimationPanelClick(mouse, direction);
		}
		else if (StripArea.Contains(mouse))
		{
			OnStripClick(mouse);
		}
	}

	private void UpdateTextEntryCommitOnClick()
	{
		EditorPropertyRow row = GetPropertyRows().FirstOrDefault(r => r.Label == _entry.Row);
		string text = _entry.Text;
		_entry.Cancel();
		if (row?.Commit != null && text != row.Value && !row.Commit(text))
		{
			ShowMessage("Value not accepted: " + text);
		}
	}

	private void UpdateWheel(Vector2 mouse, double wheel)
	{
		double step = -wheel * EditorUi.RowHeight * 3.0;
		if (mouse.X < EditorUi.PanelWidth)
		{
			_groupScroll = Math.Max(0.0, _groupScroll + step);
		}
		else if (mouse.X >= ScreenWidth - EditorUi.PanelWidth)
		{
			if (mouse.Y < ListBottom)
			{
				_animationScroll = Math.Max(0.0, _animationScroll + step);
			}
			else
			{
				_propertyScroll = Math.Max(0.0, _propertyScroll + step);
			}
		}
		else if (StripArea.Contains(mouse))
		{
			_stripScroll = Math.Max(0.0, _stripScroll + step);
		}
		else if (_view == View.Texture)
		{
			// Zoom about the point under the mouse
			Vector2 point = ScreenToTexture(mouse);
			_textureZoomIndex = Math.Clamp(_textureZoomIndex + Math.Sign(wheel), 0, TextureZooms.Length - 1);
			Rectangle area = PreviewArea;
			_texturePan = new Vector2(Math.Round(mouse.X - area.X - point.X * TextureZoom), Math.Round(mouse.Y - area.Y - point.Y * TextureZoom));
		}
		else
		{
			_zoomIndex = Math.Clamp(_zoomIndex + Math.Sign(wheel), 0, Zooms.Length - 1);
		}
	}

	private Vector2 TextureToScreen(double x, double y)
	{
		Rectangle area = PreviewArea;
		return new Vector2(area.X + _texturePan.X + x * TextureZoom, area.Y + _texturePan.Y + y * TextureZoom);
	}

	private Vector2 ScreenToTexture(Vector2 screen)
	{
		Rectangle area = PreviewArea;
		return new Vector2((screen.X - area.X - _texturePan.X) / TextureZoom, (screen.Y - area.Y - _texturePan.Y) / TextureZoom);
	}

	private Rectangle TextureToScreen(AnimationFrameDocument frame)
	{
		Vector2 topLeft = TextureToScreen(frame.X, frame.Y);
		return new Rectangle(topLeft.X, topLeft.Y, frame.Width * TextureZoom, frame.Height * TextureZoom);
	}

	/// <summary>The edges of the rectangle that are near the mouse, for a resize.</summary>
	private static Edges GetEdgesAt(Rectangle rectangle, Vector2 mouse)
	{
		Rectangle near = new Rectangle(rectangle.X - HandleSize, rectangle.Y - HandleSize, rectangle.Width + HandleSize * 2.0, rectangle.Height + HandleSize * 2.0);
		if (!near.Contains(mouse))
		{
			return Edges.None;
		}
		Edges edges = Edges.None;
		if (Math.Abs(mouse.X - rectangle.X) <= HandleSize)
		{
			edges |= Edges.Left;
		}
		else if (Math.Abs(mouse.X - rectangle.Right) <= HandleSize)
		{
			edges |= Edges.Right;
		}
		if (Math.Abs(mouse.Y - rectangle.Y) <= HandleSize)
		{
			edges |= Edges.Top;
		}
		else if (Math.Abs(mouse.Y - rectangle.Bottom) <= HandleSize)
		{
			edges |= Edges.Bottom;
		}
		return edges;
	}

	/// <summary>Starts a drag in the centre: the offset in the preview; the rectangle in the texture view; a pan with the right or the middle button.</summary>
	private void BeginDrag(Vector2 mouse)
	{
		AnimationDocument animation = Animation;
		_dragStart = mouse;
		_dragStartPan = _texturePan;
		_dragChanged = false;
		_dragFrames = animation?.Frames.Select(f => f.Clone()).ToList();
		if (!_input.LeftPressed)
		{
			_drag = _view == View.Texture ? DragMode.Pan : DragMode.None;
			return;
		}
		AnimationFrameDocument frame = Frame;
		if (frame == null)
		{
			return;
		}
		_playing = false;
		if (_view == View.Preview)
		{
			_drag = DragMode.Offset;
			return;
		}
		Rectangle selected = TextureToScreen(frame);
		Edges edges = GetEdgesAt(selected, mouse);
		if (edges != Edges.None)
		{
			_drag = DragMode.ResizeRectangle;
			_resizeEdges = edges;
			return;
		}
		if (selected.Contains(mouse))
		{
			_drag = DragMode.MoveRectangle;
			return;
		}
		// A click in the rectangle of another frame of the texture selects that frame
		for (int i = 0; i < animation.Frames.Count; i++)
		{
			AnimationFrameDocument other = animation.Frames[i];
			if (i != _frameIndex && other.Texture == frame.Texture && TextureToScreen(other).Contains(mouse))
			{
				_frameIndex = i;
				_drag = DragMode.None;
				return;
			}
		}
		_drag = DragMode.DrawRectangle;
	}

	private void UpdateDrag(Vector2 mouse)
	{
		AnimationFrameDocument frame = Frame;
		if (_drag == DragMode.Pan)
		{
			_texturePan = _dragStartPan + (mouse - _dragStart);
			return;
		}
		if (frame == null || _dragFrames == null || _frameIndex >= _dragFrames.Count)
		{
			return;
		}
		AnimationFrameDocument start = _dragFrames[_frameIndex];
		ITexture texture = FrameTexture;
		int maxX = texture?.Width ?? 16384;
		int maxY = texture?.Height ?? 16384;
		// The mouse moves in screen pixels; the values are in pixels of the texture
		double scale = _drag == DragMode.Offset ? Zoom : TextureZoom;
		int dx = (int)Math.Round((mouse.X - _dragStart.X) / scale);
		int dy = (int)Math.Round((mouse.Y - _dragStart.Y) / scale);
		if (!_dragChanged && dx == 0 && dy == 0)
		{
			return;
		}
		if (!_dragChanged)
		{
			// One step of the undo history for the whole drag
			Change(() => { });
			_dragChanged = true;
		}
		switch (_drag)
		{
			case DragMode.Offset:
				AnimationDocument animation = Animation;
				// With Ctrl, the offsets of all the frames move together
				for (int i = 0; i < animation.Frames.Count && i < _dragFrames.Count; i++)
				{
					if (i == _frameIndex || IsControlDown)
					{
						animation.Frames[i].OffsetX = _dragFrames[i].OffsetX + dx;
						animation.Frames[i].OffsetY = _dragFrames[i].OffsetY + dy;
					}
				}
				break;
			case DragMode.MoveRectangle:
				frame.X = Math.Clamp(start.X + dx, 0, Math.Max(0, maxX - start.Width));
				frame.Y = Math.Clamp(start.Y + dy, 0, Math.Max(0, maxY - start.Height));
				break;
			case DragMode.ResizeRectangle:
				int left = start.X;
				int top = start.Y;
				int right = start.X + start.Width;
				int bottom = start.Y + start.Height;
				if (_resizeEdges.HasFlag(Edges.Left))
				{
					left = Math.Clamp(start.X + dx, 0, right - 1);
				}
				if (_resizeEdges.HasFlag(Edges.Right))
				{
					right = Math.Clamp(right + dx, left + 1, maxX);
				}
				if (_resizeEdges.HasFlag(Edges.Top))
				{
					top = Math.Clamp(start.Y + dy, 0, bottom - 1);
				}
				if (_resizeEdges.HasFlag(Edges.Bottom))
				{
					bottom = Math.Clamp(bottom + dy, top + 1, maxY);
				}
				frame.X = left;
				frame.Y = top;
				frame.Width = right - left;
				frame.Height = bottom - top;
				break;
			case DragMode.DrawRectangle:
				Vector2 a = ScreenToTexture(_dragStart);
				Vector2 b = ScreenToTexture(mouse);
				int x0 = Math.Clamp((int)Math.Floor(Math.Min(a.X, b.X)), 0, maxX - 1);
				int y0 = Math.Clamp((int)Math.Floor(Math.Min(a.Y, b.Y)), 0, maxY - 1);
				int x1 = Math.Clamp((int)Math.Ceiling(Math.Max(a.X, b.X)), x0 + 1, maxX);
				int y1 = Math.Clamp((int)Math.Ceiling(Math.Max(a.Y, b.Y)), y0 + 1, maxY);
				frame.X = x0;
				frame.Y = y0;
				frame.Width = x1 - x0;
				frame.Height = y1 - y0;
				break;
		}
	}

	private const string FilterRow = "Filter";

	/// <summary>The rows of the group panel start under the heading and the filter row.</summary>
	private static double GroupListTop => EditorUi.TopBarHeight + EditorUi.RowHeight * 2.0;

	private void OnGroupPanelClick(Vector2 mouse)
	{
		if (mouse.Y < EditorUi.TopBarHeight + EditorUi.RowHeight)
		{
			return;
		}
		if (mouse.Y < GroupListTop)
		{
			_entry.Begin(FilterRow, _filter);
			return;
		}
		int index = (int)Math.Floor((mouse.Y - GroupListTop + _groupScroll) / EditorUi.RowHeight);
		List<string> groups = VisibleGroups.ToList();
		if (index >= 0 && index < groups.Count)
		{
			SelectGroup(groups[index]);
		}
	}

	private void OnAnimationPanelClick(Vector2 mouse, int direction)
	{
		if (mouse.Y < ListBottom)
		{
			int index = (int)Math.Floor((mouse.Y - (EditorUi.TopBarHeight + EditorUi.RowHeight) + _animationScroll) / EditorUi.RowHeight);
			if (mouse.Y >= EditorUi.TopBarHeight + EditorUi.RowHeight && _document != null && index >= 0 && index < _document.Animations.Count)
			{
				SelectAnimation(index);
			}
			return;
		}
		double rowsTop = ListBottom + EditorUi.RowHeight;
		if (mouse.Y < rowsTop)
		{
			return;
		}
		List<EditorPropertyRow> rows = GetPropertyRows();
		int rowIndex = (int)Math.Floor((mouse.Y - rowsTop + _propertyScroll) / EditorUi.RowHeight);
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

	private void OnStripClick(Vector2 mouse)
	{
		AnimationDocument animation = Animation;
		if (animation == null)
		{
			return;
		}
		int index = (int)Math.Floor((mouse.X - StripArea.X - StripGap + _stripScroll) / (StripBoxWidth + StripGap));
		if (index >= 0 && index < animation.Frames.Count)
		{
			_playing = false;
			_frameIndex = index;
		}
	}

	#endregion

	#region Playback

	private void RestartPlayback()
	{
		_playFrame = 0;
		_playTicks = 0;
	}

	private void TogglePlay()
	{
		_playing = !_playing;
		if (_playing)
		{
			_playFrame = _frameIndex;
			_playTicks = 0;
		}
	}

	/// <summary>Plays the animation as the game does: each frame shows for its delay plus one frame, then the loop frame follows the last frame.</summary>
	private void UpdatePlayback()
	{
		AnimationDocument animation = Animation;
		if (!_playing || animation == null || animation.Frames.Count == 0)
		{
			return;
		}
		_playFrame = Math.Min(_playFrame, animation.Frames.Count - 1);
		if (++_playTicks <= animation.Frames[_playFrame].Delay)
		{
			return;
		}
		_playTicks = 0;
		_playFrame++;
		if (_playFrame >= animation.Frames.Count)
		{
			// Without a loop frame the game stays on the last frame or goes to the next animation; the preview starts again
			_playFrame = animation.Loop is int loop && loop >= 0 && loop < animation.Frames.Count ? loop : 0;
		}
		_frameIndex = _playFrame;
	}

	private void StepFrame(int step)
	{
		AnimationDocument animation = Animation;
		if (animation == null || animation.Frames.Count == 0)
		{
			return;
		}
		_playing = false;
		_frameIndex = (_frameIndex + step + animation.Frames.Count) % animation.Frames.Count;
	}

	private void SelectAnimation(int index)
	{
		if (_document == null || _document.Animations.Count == 0)
		{
			return;
		}
		_entry.Cancel();
		_animationIndex = Math.Clamp(index, 0, _document.Animations.Count - 1);
		_frameIndex = 0;
		_stripScroll = 0.0;
		RestartPlayback();
		// Keep the selected animation in the list
		double top = _animationIndex * EditorUi.RowHeight;
		double height = ListBottom - EditorUi.TopBarHeight - EditorUi.RowHeight;
		if (top < _animationScroll)
		{
			_animationScroll = top;
		}
		else if (top + EditorUi.RowHeight > _animationScroll + height)
		{
			_animationScroll = top + EditorUi.RowHeight - height;
		}
	}

	#endregion

	#region Changes

	/// <summary>Runs a change of the document, with a step of the undo history.</summary>
	private void Change(Action action)
	{
		if (_document == null)
		{
			return;
		}
		GetStack(_undo).Push(new Snapshot(_document.Clone(), _animationIndex, _frameIndex));
		GetStack(_redo).Clear();
		action();
		_dirty.Add(_groupKey);
		_changed.Add(_groupKey);
		_confirmLeave = false;
		ClampSelection();
	}

	private Stack<Snapshot> GetStack(Dictionary<string, Stack<Snapshot>> stacks)
	{
		if (!stacks.TryGetValue(_groupKey, out Stack<Snapshot> stack))
		{
			stack = new Stack<Snapshot>();
			stacks[_groupKey] = stack;
		}
		return stack;
	}

	private void ClampSelection()
	{
		_animationIndex = Math.Clamp(_animationIndex, 0, Math.Max(0, _document.Animations.Count - 1));
		int frames = Animation?.Frames.Count ?? 0;
		_frameIndex = Math.Clamp(_frameIndex, 0, Math.Max(0, frames - 1));
		_playFrame = Math.Clamp(_playFrame, 0, Math.Max(0, frames - 1));
	}

	private bool CanUndo => _groupKey != null && GetStack(_undo).Count > 0;

	private bool CanRedo => _groupKey != null && GetStack(_redo).Count > 0;

	private void Undo() => Restore(_undo, _redo, "Undo");

	private void Redo() => Restore(_redo, _undo, "Redo");

	private void Restore(Dictionary<string, Stack<Snapshot>> from, Dictionary<string, Stack<Snapshot>> to, string name)
	{
		if (_groupKey == null || GetStack(from).Count == 0)
		{
			return;
		}
		_entry.Cancel();
		GetStack(to).Push(new Snapshot(_document.Clone(), _animationIndex, _frameIndex));
		Snapshot snapshot = GetStack(from).Pop();
		bool texturesChanged = !snapshot.Document.Textures.SequenceEqual(_document.Textures);
		_document = snapshot.Document;
		_documents[_groupKey] = _document;
		_animationIndex = snapshot.Animation;
		_frameIndex = snapshot.Frame;
		_dirty.Add(_groupKey);
		_changed.Add(_groupKey);
		ClampSelection();
		if (texturesChanged)
		{
			LoadTextures();
		}
		ShowMessage(name);
	}

	private void AddFrame()
	{
		AnimationDocument animation = Animation;
		if (animation == null)
		{
			return;
		}
		// A new frame copies the size and the offset of the selected frame, with the default delay
		AnimationFrameDocument frame = Frame?.Clone() ?? new AnimationFrameDocument();
		Change(() => animation.InsertFrame(animation.Frames.Count == 0 ? 0 : _frameIndex + 1, frame));
		_frameIndex = Math.Min(_frameIndex + 1, animation.Frames.Count - 1);
		_playing = false;
	}

	private void DuplicateFrame()
	{
		AnimationDocument animation = Animation;
		AnimationFrameDocument frame = Frame;
		if (animation == null || frame == null)
		{
			return;
		}
		Change(() => animation.InsertFrame(_frameIndex + 1, frame.Clone()));
		_frameIndex++;
		_playing = false;
	}

	private void DeleteFrame()
	{
		AnimationDocument animation = Animation;
		if (animation == null || Frame == null)
		{
			return;
		}
		Change(() => animation.RemoveFrame(_frameIndex));
		_playing = false;
	}

	private void MoveFrame(int step)
	{
		AnimationDocument animation = Animation;
		if (animation == null || Frame == null)
		{
			return;
		}
		int target = _frameIndex + step;
		if (target < 0 || target >= animation.Frames.Count)
		{
			return;
		}
		Change(() => animation.MoveFrame(_frameIndex, target));
		_frameIndex = target;
		_playing = false;
	}

	private void AddAnimation(bool duplicate)
	{
		if (_document == null)
		{
			return;
		}
		AnimationDocument animation = duplicate && Animation != null ? Animation.Clone() : new AnimationDocument();
		if (!duplicate)
		{
			animation.Name = "new";
			animation.Frames.Add(Frame?.Clone() ?? new AnimationFrameDocument());
		}
		Change(() => _document.AddAnimation(animation));
		SelectAnimation(_document.Animations.Count - 1);
		ShowMessage("The game finds animations by their number, so a new animation goes at the end");
	}

	private void DeleteLastAnimation()
	{
		if (_document == null || _document.Animations.Count == 0)
		{
			return;
		}
		Change(() => _document.RemoveLastAnimation());
		ShowMessage("Deleted the last animation");
	}

	#endregion

	#region Properties

	private List<EditorPropertyRow> GetPropertyRows()
	{
		List<EditorPropertyRow> rows = new List<EditorPropertyRow>();
		AnimationDocument animation = Animation;
		if (animation == null)
		{
			return rows;
		}
		rows.Add(new EditorPropertyRow { Label = $"Animation {_animationIndex}", IsHeading = true });
		rows.Add(new EditorPropertyRow
		{
			Label = "Name",
			Value = animation.Name ?? "",
			Commit = text =>
			{
				Change(() => animation.Name = text);
				return true;
			}
		});
		rows.Add(new EditorPropertyRow
		{
			Label = "Loop frame",
			Value = animation.Loop?.ToString(CultureInfo.InvariantCulture) ?? "none",
			Commit = text => CommitOptional(text, animation.Frames.Count, value => animation.Loop = value)
		});
		rows.Add(new EditorPropertyRow
		{
			Label = "Next animation",
			Value = animation.Next?.ToString(CultureInfo.InvariantCulture) ?? "none",
			Commit = text => CommitOptional(text, _document.Animations.Count, value => animation.Next = value)
		});
		rows.Add(new EditorPropertyRow
		{
			Label = "Delay of all frames",
			Value = animation.Frames.Count > 0 && animation.Frames.All(f => f.Delay == animation.Frames[0].Delay)
				? animation.Frames[0].Delay.ToString(CultureInfo.InvariantCulture)
				: "mixed",
			Commit = text => CommitInt(text, 0, 255, value => animation.Frames.ForEach(f => f.Delay = value))
		});
		int length = animation.Frames.Sum(f => f.Delay + 1);
		rows.Add(new EditorPropertyRow { Label = "Length", Value = $"{animation.Frames.Count} frames, {length} ticks" });
		AnimationFrameDocument frame = Frame;
		if (frame != null)
		{
			AddFrameRows(rows, frame);
		}
		AddTextureRows(rows);
		return rows;
	}

	/// <summary>
	/// The textures of the group. "Edit" copies a texture into the folder of the editor and shows it in the file
	/// manager; "Replace" and "Add texture" take the path of a PNG file.
	/// </summary>
	private void AddTextureRows(List<EditorPropertyRow> rows)
	{
		rows.Add(new EditorPropertyRow { Label = "Textures", IsHeading = true });
		for (int i = 0; i < _document.Textures.Count; i++)
		{
			int index = i;
			string key = i < _textureKeys.Count ? _textureKeys[i] : null;
			ITexture texture = i < _textures.Count ? _textures[i] : null;
			bool inFolder = key != null && File.Exists(AnimationTextureFiles.GetPath(Folder, _gameContext.ResourceTree, key));
			rows.Add(new EditorPropertyRow
			{
				Label = $"Texture {i}",
				Value = $"{_document.Textures[i]}{(texture != null ? $"  {texture.Width} x {texture.Height}" : "  missing")}{(inFolder ? "  (mod)" : "")}"
			});
			rows.Add(new EditorPropertyRow { Label = $"Edit {i}", Value = "copy to the mod folder", Step = _ => EditTexture(index) });
			rows.Add(new EditorPropertyRow { Label = $"Replace {i}", Value = "type the path of a PNG", Commit = text => ReplaceTexture(index, text) });
		}
		rows.Add(new EditorPropertyRow { Label = "Add texture", Value = "type the path of a PNG", Commit = AddTexture });
	}

	private void AddFrameRows(List<EditorPropertyRow> rows, AnimationFrameDocument frame)
	{
		rows.Add(new EditorPropertyRow { Label = $"Frame {_frameIndex}", IsHeading = true });
		string textureName = frame.Texture >= 0 && frame.Texture < _document.Textures.Count ? _document.Textures[frame.Texture] : "missing";
		rows.Add(new EditorPropertyRow
		{
			Label = "Texture",
			Value = $"{frame.Texture} {textureName}",
			Commit = text => CommitInt(text.Split(' ')[0], 0, Math.Max(0, _document.Textures.Count - 1), value => frame.Texture = value)
		});
		rows.Add(IntRow("X", frame.X, 0, 16384, value => frame.X = value));
		rows.Add(IntRow("Y", frame.Y, 0, 16384, value => frame.Y = value));
		rows.Add(IntRow("Width", frame.Width, 0, 16384, value => frame.Width = value));
		rows.Add(IntRow("Height", frame.Height, 0, 16384, value => frame.Height = value));
		rows.Add(IntRow("Offset X", frame.OffsetX, -16384, 16384, value => frame.OffsetX = value));
		rows.Add(IntRow("Offset Y", frame.OffsetY, -16384, 16384, value => frame.OffsetY = value));
		rows.Add(IntRow("Delay", frame.Delay, 0, 255, value => frame.Delay = value));
	}

	private EditorPropertyRow IntRow(string label, int value, int min, int max, Action<int> set)
	{
		return new EditorPropertyRow
		{
			Label = label,
			Value = value.ToString(CultureInfo.InvariantCulture),
			Commit = text => CommitInt(text, min, max, set)
		};
	}

	private bool CommitInt(string text, int min, int max, Action<int> set)
	{
		if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < min || value > max)
		{
			return false;
		}
		Change(() => set(value));
		return true;
	}

	/// <summary>A number below the count, or "none" (or nothing) for no value.</summary>
	private bool CommitOptional(string text, int count, Action<int?> set)
	{
		text = text.Trim();
		if (text.Length == 0 || text.Equals("none", StringComparison.OrdinalIgnoreCase) || text == "-")
		{
			Change(() => set(null));
			return true;
		}
		if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < 0 || value >= count)
		{
			return false;
		}
		Change(() => set(value));
		return true;
	}

	#endregion

	#region Saving

	/// <summary>
	/// Writes each changed group as a file into the animations folder of the editor, and gives the game the new data,
	/// so that a level that loads later in this session has the changes.
	/// </summary>
	public void Save()
	{
		if (_dirty.Count == 0)
		{
			ShowMessage("No changes to save");
			return;
		}
		string folder = GetFolder(_gameContext.UserDataDirectory);
		try
		{
			ResourceTree tree = new ResourceTree();
			List<(string Key, byte[] Data)> saved = new List<(string, byte[])>();
			foreach (string key in _dirty)
			{
				byte[] data = Encoding.UTF8.GetBytes(_documents[key].ToXml());
				tree.SetOrAdd(key, new Resource(key, ResourceTypeIdentifier.AnimationGroup, new MemoryResourceSource(data)));
				saved.Add((key, data));
			}
			ResourceDirectory.Export(tree, folder);
			foreach ((string key, byte[] data) in saved)
			{
				Resource resource = _gameContext.ResourceTree[key]?.Resource;
				if (resource != null)
				{
					resource.Source = new MemoryResourceSource(data);
				}
			}
			ShowMessage($"Saved {saved.Count} animation group{(saved.Count == 1 ? "" : "s")} to mods/{EditorModFile.FolderName}/{FolderName} in the user data folder");
			_dirty.Clear();
			_confirmLeave = false;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			ShowMessage("Cannot save: " + ex.Message);
		}
	}

	#endregion

	#region Drawing

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
		double x = EditorUi.Padding;
		void Add(string text, Action action, bool enabled = true, bool active = false)
		{
			double width = _ui.GetButtonWidth(text);
			buttons.Add(new Button
			{
				Bounds = new Rectangle(x, 6.0, width, EditorUi.TopBarHeight - 12.0),
				Text = text,
				Enabled = enabled,
				Active = active,
				Action = action
			});
			x += width + 6.0;
		}
		bool hasAnimation = Animation != null;
		bool hasFrame = Frame != null;
		if (_hosted)
		{
			Add("Level (Esc)", () => CloseRequested = true);
			x += EditorUi.Padding;
		}
		Add("Save", Save, active: _dirty.Count > 0);
		Add("Undo", Undo, CanUndo);
		Add("Redo", Redo, CanRedo);
		Add(_playing ? "Pause (Space)" : "Play (Space)", TogglePlay, hasAnimation, _playing);
		x += EditorUi.Padding;
		Add("Add frame", AddFrame, hasAnimation);
		Add("Duplicate frame", DuplicateFrame, hasFrame);
		Add("Delete frame", DeleteFrame, hasFrame);
		Add("<", () => MoveFrame(-1), hasFrame && _frameIndex > 0);
		Add(">", () => MoveFrame(1), hasFrame && _frameIndex < Animation.Frames.Count - 1);
		x += EditorUi.Padding;
		Add("Add animation", () => AddAnimation(false), _document != null);
		Add("Duplicate animation", () => AddAnimation(true), hasAnimation);
		Add("Delete last animation", DeleteLastAnimation, _document != null && _document.Animations.Count > 0);
		x += EditorUi.Padding;
		Add(_view == View.Texture ? "Preview (T)" : "Texture (T)", ToggleView, hasFrame, _view == View.Texture);
		Add("Onion skin (O)", () => _onionSkin = !_onionSkin, active: _onionSkin);
		Add($"Zoom {(_view == View.Texture ? TextureZoom : Zoom) * 100:0}%", CycleZoom);
		return buttons;
	}

	public override void Draw(Renderer renderer)
	{
		TestHooks.DrawnLevelTick = TestHooks.EditorCaptureOffset + TestHooks.EditorFrame;
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.ModelMatrix = Matrix4.Identity;
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
		g.RenderQuad(new Colour(1.0, 0.05, 0.06, 0.08), new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight));
		if (_view == View.Texture)
		{
			DrawTextureView(renderer, g);
		}
		else
		{
			DrawPreview(renderer, g);
		}
		DrawStrip(renderer, g);
		if (_ui.HasFont)
		{
			DrawTopBar(renderer, g);
			DrawGroupPanel(renderer, g);
			DrawAnimationPanel(renderer, g);
			DrawStatusBar(renderer, g);
		}
		renderer.DeativateRenderer();
	}

	private Vector2 PreviewOrigin
	{
		get
		{
			Rectangle area = PreviewArea;
			return new Vector2(Math.Round(area.X + area.Width / 2.0), Math.Round(area.Y + area.Height * 0.55));
		}
	}

	private void DrawPreview(Renderer renderer, I2dRenderer g)
	{
		Rectangle area = PreviewArea;
		g.RenderQuad(PreviewColour, area);
		Vector2 origin = PreviewOrigin;
		// A grid of 64 pixels of the game, through the position of the object
		double step = 64.0 * Zoom;
		for (double x = origin.X % step; x < area.Right; x += step)
		{
			if (x > area.X)
			{
				g.RenderLine(GridColour, new Vector2(x, area.Y), new Vector2(x, area.Bottom), 1.0);
			}
		}
		for (double y = origin.Y % step; y < area.Bottom; y += step)
		{
			if (y > area.Y)
			{
				g.RenderLine(GridColour, new Vector2(area.X, y), new Vector2(area.Right, y), 1.0);
			}
		}
		g.ClipRectangle = area;
		AnimationFrameDocument frame = Frame;
		AnimationDocument animation = Animation;
		if (frame != null && _onionSkin && !_playing && animation.Frames.Count > 1)
		{
			// The previous frame, faint, to line up the frames
			AnimationFrameDocument previous = animation.Frames[(_frameIndex - 1 + animation.Frames.Count) % animation.Frames.Count];
			ITexture texture = previous.Texture >= 0 && previous.Texture < _textures.Count ? _textures[previous.Texture] : null;
			if (texture != null)
			{
				g.Colour = new Colour(OnionOpacity, 1.0, 1.0, 1.0);
				g.RenderTexture(texture, new Rectangle(previous.X, previous.Y, previous.Width, previous.Height), GetPreviewRectangle(previous, origin));
				g.Colour = Colours.White;
			}
		}
		if (frame != null)
		{
			Rectangle destination = GetPreviewRectangle(frame, origin);
			DrawFrame(renderer, g, frame, destination);
			g.RenderRectangle(FrameOutlineColour, destination, 1.0);
		}
		foreach (Rectanglei hitbox in _groupKey == _hitboxGroupKey ? _hitboxes : Array.Empty<Rectanglei>())
		{
			g.RenderRectangle(HitboxColour, new Rectangle(origin.X + hitbox.X * Zoom, origin.Y + hitbox.Y * Zoom, hitbox.Width * Zoom, hitbox.Height * Zoom), 2.0);
		}
		EditorUi.DrawCross(g, origin, 16.0, EditorUi.ShadowColour, 5.0);
		EditorUi.DrawCross(g, origin, 16.0, Colours.White, 2.0);
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
		if (_textureTask != null && _ui.HasFont)
		{
			_ui.DrawLabel(renderer, g, "Loading the textures...", area.X + EditorUi.Padding, area.Y + EditorUi.Padding, EditorUi.TextColour);
		}
	}

	/// <summary>The rectangle of the screen where the preview draws a frame: its centre is at the offset from the position of the object.</summary>
	private Rectangle GetPreviewRectangle(AnimationFrameDocument frame, Vector2 origin)
	{
		return new Rectangle(origin.X + (frame.OffsetX - frame.Width / 2.0) * Zoom, origin.Y + (frame.OffsetY - frame.Height / 2.0) * Zoom,
			frame.Width * Zoom, frame.Height * Zoom);
	}

	/// <summary>
	/// The texture of the selected frame, with the rectangles of the frames of the animation that use it. The selected
	/// rectangle has handles at its corners and edges.
	/// </summary>
	private void DrawTextureView(Renderer renderer, I2dRenderer g)
	{
		Rectangle area = PreviewArea;
		g.RenderQuad(PreviewColour, area);
		ITexture texture = FrameTexture;
		AnimationFrameDocument frame = Frame;
		g.ClipRectangle = area;
		if (texture != null && frame != null)
		{
			Vector2 topLeft = TextureToScreen(0, 0);
			Rectangle whole = new Rectangle(topLeft.X, topLeft.Y, texture.Width * TextureZoom, texture.Height * TextureZoom);
			g.RenderQuad(GridColour, whole);
			g.RenderTexture(texture, new Rectangle(0.0, 0.0, texture.Width, texture.Height), whole);
			g.RenderRectangle(EditorUi.OutlineColour, whole, 1.0);
			AnimationDocument animation = Animation;
			for (int i = 0; i < animation.Frames.Count; i++)
			{
				AnimationFrameDocument other = animation.Frames[i];
				if (i != _frameIndex && other.Texture == frame.Texture)
				{
					g.RenderRectangle(RectangleColour, TextureToScreen(other), 1.0);
				}
			}
			Rectangle selected = TextureToScreen(frame);
			g.RenderRectangle(EditorUi.ShadowColour, selected, 4.0);
			g.RenderRectangle(SelectedRectangleColour, selected, 2.0);
			foreach (Vector2 handle in new[]
			{
				new Vector2(selected.X, selected.Y), new Vector2(selected.Right, selected.Y), new Vector2(selected.X, selected.Bottom),
				new Vector2(selected.Right, selected.Bottom), new Vector2(selected.X + selected.Width / 2.0, selected.Y),
				new Vector2(selected.X + selected.Width / 2.0, selected.Bottom), new Vector2(selected.X, selected.Y + selected.Height / 2.0),
				new Vector2(selected.Right, selected.Y + selected.Height / 2.0)
			})
			{
				g.RenderQuad(SelectedRectangleColour, new Rectangle(handle.X - HandleSize / 2.0, handle.Y - HandleSize / 2.0, HandleSize, HandleSize));
			}
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
		if (_ui.HasFont && area.Contains(_input.Mouse) && texture != null)
		{
			Vector2 point = ScreenToTexture(_input.Mouse);
			_ui.DrawLabel(renderer, g, $"{Math.Floor(point.X)}, {Math.Floor(point.Y)}", area.X + EditorUi.Padding, area.Y + EditorUi.Padding, EditorUi.DimTextColour);
		}
	}

	/// <summary>
	/// Draws a frame into a rectangle of the screen. A character (a group with a body and a skin texture) is drawn as
	/// the game draws it, with the skin on the body.
	/// </summary>
	private void DrawFrame(Renderer renderer, I2dRenderer g, AnimationFrameDocument frame, Rectangle destination)
	{
		Rectangle source = new Rectangle(frame.X, frame.Y, frame.Width, frame.Height);
		bool character = _textures.Count == 2 && _document.Textures.Count == 2 && _document.Textures[1].EndsWith("SKIN", StringComparison.OrdinalIgnoreCase);
		if (character && _textures[0] != null && _textures[1] != null)
		{
			renderer.DeativateRenderer();
			ICharacterRenderer characterRenderer = renderer.GetCharacterRenderer();
			// The level shares the character renderer and does not set these values, so they go back after the frame
			Matrix4 modelMatrix = characterRenderer.ModelMatrix;
			Rectangle clipRectangle = characterRenderer.ClipRectangle;
			characterRenderer.ModelMatrix = Matrix4.Identity;
			characterRenderer.ClipRectangle = g.ClipRectangle;
			characterRenderer.RenderTexture(_textures[1], _textures[0], source, destination);
			renderer.DeativateRenderer();
			characterRenderer.ModelMatrix = modelMatrix;
			characterRenderer.ClipRectangle = clipRectangle;
			return;
		}
		ITexture texture = frame.Texture >= 0 && frame.Texture < _textures.Count ? _textures[frame.Texture] : null;
		if (texture != null)
		{
			g.RenderTexture(texture, source, destination);
		}
	}

	private void DrawStrip(Renderer renderer, I2dRenderer g)
	{
		Rectangle area = StripArea;
		g.RenderQuad(EditorUi.PanelColour, area);
		AnimationDocument animation = Animation;
		if (animation == null)
		{
			return;
		}
		double boxHeight = area.Height - EditorUi.RowHeight - StripGap * 2.0;
		// Keep the selected frame in view
		double selectedLeft = _frameIndex * (StripBoxWidth + StripGap);
		if (selectedLeft < _stripScroll)
		{
			_stripScroll = selectedLeft;
		}
		else if (selectedLeft + StripBoxWidth + StripGap * 2.0 > _stripScroll + area.Width)
		{
			_stripScroll = selectedLeft + StripBoxWidth + StripGap * 2.0 - area.Width;
		}
		for (int i = 0; i < animation.Frames.Count; i++)
		{
			double x = area.X + StripGap + i * (StripBoxWidth + StripGap) - _stripScroll;
			if (x + StripBoxWidth < area.X || x > area.Right)
			{
				continue;
			}
			Rectangle box = new Rectangle(x, area.Y + StripGap, StripBoxWidth, boxHeight);
			g.ClipRectangle = new Rectangle(Math.Max(box.X, area.X), box.Y, Math.Min(box.Right, area.Right) - Math.Max(box.X, area.X), box.Height + EditorUi.RowHeight);
			g.RenderQuad(i == _frameIndex ? EditorUi.AccentColour : PreviewColour, box);
			AnimationFrameDocument frame = animation.Frames[i];
			if (frame.Width > 0 && frame.Height > 0)
			{
				double scale = Math.Min(1.0, Math.Min((box.Width - 8.0) / frame.Width, (box.Height - 8.0) / frame.Height));
				double width = frame.Width * scale;
				double height = frame.Height * scale;
				DrawFrame(renderer, g, frame, new Rectangle(box.X + (box.Width - width) / 2.0, box.Y + (box.Height - height) / 2.0, width, height));
			}
			if (_ui.HasFont)
			{
				string label = animation.Loop == i ? $"{i}  loop  {frame.Delay + 1}t" : $"{i}  {frame.Delay + 1}t";
				_ui.DrawText(renderer, label, box.X + 4.0, box.Bottom + 4.0, i == _frameIndex ? EditorUi.TextColour : EditorUi.DimTextColour);
			}
			renderer.DeativateRenderer();
		}
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
	}

	private void DrawTopBar(Renderer renderer, I2dRenderer g)
	{
		g.RenderQuad(EditorUi.PanelColour, new Rectangle(0.0, 0.0, ScreenWidth, EditorUi.TopBarHeight));
		Vector2 mouse = _input.Mouse;
		double buttonsRight = 0.0;
		foreach (Button button in GetTopBarButtons())
		{
			bool hover = button.Enabled && button.Bounds.Contains(mouse);
			_ui.DrawButton(renderer, g, button.Bounds, button.Text, button.Active, hover, button.Enabled);
			buttonsRight = Math.Max(buttonsRight, button.Bounds.Right);
		}
		// The title is the key of the group; a short name when the key does not fit next to the buttons
		int unsaved = _dirty.Count;
		string suffix = unsaved > 0 ? $"  ({unsaved} not saved)" : "";
		string title = (_groupKey ?? "Animation editor") + suffix;
		if (_groupKey != null && _ui.MeasureText(title) > ScreenWidth - EditorUi.Padding * 2.0 - buttonsRight)
		{
			title = ShortName(_groupKey) + suffix;
		}
		if (_ui.MeasureText(title) <= ScreenWidth - EditorUi.Padding * 2.0 - buttonsRight)
		{
			_ui.DrawText(renderer, title, ScreenWidth - EditorUi.Padding - _ui.MeasureText(title), (EditorUi.TopBarHeight - _ui.TextHeight) / 2.0,
				unsaved > 0 ? EditorUi.SelectionColour : EditorUi.DimTextColour);
		}
	}

	private void DrawGroupPanel(Renderer renderer, I2dRenderer g)
	{
		double bottom = ScreenHeight - EditorUi.StatusBarHeight;
		g.RenderQuad(EditorUi.PanelColour, new Rectangle(0.0, EditorUi.TopBarHeight, EditorUi.PanelWidth, bottom - EditorUi.TopBarHeight));
		_ui.DrawHeading(renderer, g, "Animation groups", 0.0, EditorUi.TopBarHeight);
		EditorPropertyRow filterRow = new EditorPropertyRow
		{
			Label = FilterRow,
			Value = _filter.Length > 0 ? _filter : "(type to find)",
			Commit = _ => true
		};
		_ui.DrawPropertyRow(renderer, g, filterRow, new Rectangle(0.0, EditorUi.TopBarHeight + EditorUi.RowHeight, EditorUi.PanelWidth, EditorUi.RowHeight), _input.Mouse, _entry, _frames);
		List<string> groups = VisibleGroups.ToList();
		double top = GroupListTop;
		_groupScroll = Math.Min(_groupScroll, Math.Max(0.0, groups.Count * EditorUi.RowHeight - (bottom - top)));
		g.ClipRectangle = new Rectangle(0.0, top, EditorUi.PanelWidth, bottom - top);
		for (int i = 0; i < groups.Count; i++)
		{
			double y = top + i * EditorUi.RowHeight - _groupScroll;
			if (y + EditorUi.RowHeight < top || y > bottom)
			{
				continue;
			}
			string key = groups[i];
			EditorListRow row = new EditorListRow
			{
				Text = ShortName(key),
				Detail = _dirty.Contains(key) ? "not saved" : null,
				Active = key == _groupKey
			};
			_ui.DrawListRow(renderer, g, row, new Rectangle(0.0, y, EditorUi.PanelWidth, EditorUi.RowHeight), _input.Mouse);
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
	}

	/// <summary>The key without "SONICORCA/" and "/ANIGROUP", for example OBJECTS/SONIC.</summary>
	private static string ShortName(string key)
	{
		string name = key.StartsWith("SONICORCA/", StringComparison.Ordinal) ? key.Substring("SONICORCA/".Length) : key;
		return name.EndsWith("/ANIGROUP", StringComparison.Ordinal) ? name.Substring(0, name.Length - "/ANIGROUP".Length) : name;
	}

	private void DrawAnimationPanel(Renderer renderer, I2dRenderer g)
	{
		double left = ScreenWidth - EditorUi.PanelWidth;
		double bottom = ScreenHeight - EditorUi.StatusBarHeight;
		g.RenderQuad(EditorUi.PanelColour, new Rectangle(left, EditorUi.TopBarHeight, EditorUi.PanelWidth, bottom - EditorUi.TopBarHeight));
		_ui.DrawHeading(renderer, g, "Animations", left, EditorUi.TopBarHeight);
		double listTop = EditorUi.TopBarHeight + EditorUi.RowHeight;
		int count = _document?.Animations.Count ?? 0;
		_animationScroll = Math.Min(_animationScroll, Math.Max(0.0, count * EditorUi.RowHeight - (ListBottom - listTop)));
		g.ClipRectangle = new Rectangle(left, listTop, EditorUi.PanelWidth, ListBottom - listTop);
		for (int i = 0; i < count; i++)
		{
			double y = listTop + i * EditorUi.RowHeight - _animationScroll;
			if (y + EditorUi.RowHeight < listTop || y > ListBottom)
			{
				continue;
			}
			AnimationDocument animation = _document.Animations[i];
			EditorListRow row = new EditorListRow
			{
				Text = $"{i}  {animation.Name ?? ""}",
				Detail = $"{animation.Frames.Count} frames",
				Active = i == _animationIndex,
				Dim = animation.Frames.Count == 0
			};
			_ui.DrawListRow(renderer, g, row, new Rectangle(left, y, EditorUi.PanelWidth, EditorUi.RowHeight), _input.Mouse);
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
		_ui.DrawHeading(renderer, g, "Properties", left, ListBottom);
		List<EditorPropertyRow> rows = GetPropertyRows();
		double rowsTop = ListBottom + EditorUi.RowHeight;
		_propertyScroll = Math.Min(_propertyScroll, Math.Max(0.0, rows.Count * EditorUi.RowHeight - (bottom - rowsTop)));
		g.ClipRectangle = new Rectangle(left, rowsTop, EditorUi.PanelWidth, bottom - rowsTop);
		for (int i = 0; i < rows.Count; i++)
		{
			double y = rowsTop + i * EditorUi.RowHeight - _propertyScroll;
			if (y + EditorUi.RowHeight < rowsTop || y > bottom)
			{
				continue;
			}
			_ui.DrawPropertyRow(renderer, g, rows[i], new Rectangle(left, y, EditorUi.PanelWidth, EditorUi.RowHeight), _input.Mouse, _entry, _frames);
		}
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
	}

	private void DrawStatusBar(Renderer renderer, I2dRenderer g)
	{
		double top = ScreenHeight - EditorUi.StatusBarHeight;
		g.RenderQuad(EditorUi.PanelColour, new Rectangle(0.0, top, ScreenWidth, EditorUi.StatusBarHeight));
		AnimationDocument animation = Animation;
		string left = animation == null
			? $"{_groupKeys.Count} animation groups"
			: $"Frame {_frameIndex + 1} of {animation.Frames.Count}   Animation {_animationIndex} of {_document.Animations.Count}";
		double textY = top + (EditorUi.StatusBarHeight - _ui.TextHeight) / 2.0;
		_ui.DrawText(renderer, left, EditorUi.Padding, textY, EditorUi.DimTextColour);
		string hint = _view == View.Texture
			? "Drag: move, resize or draw the rectangle   Right drag: pan   Wheel: zoom   Alt+arrows: move   Alt+Shift+arrows: size   T: preview"
			: "Drag: offset (Ctrl: all frames)   Alt+arrows: offset   Space play   Left/Right frame   Up/Down animation   T: texture   O: onion skin   Ctrl+S save";
		string right = _messageFrames > 0 ? _message : hint;
		_ui.DrawText(renderer, right, ScreenWidth - EditorUi.Padding - _ui.MeasureText(right), textY, _messageFrames > 0 ? EditorUi.TextColour : EditorUi.DimTextColour);
	}

	private void ShowMessage(string message)
	{
		_message = message;
		_messageFrames = MessageFrames;
	}

	#endregion
}
