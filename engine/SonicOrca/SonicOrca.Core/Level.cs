#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Audio;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Debugging;
using SonicOrca.Core.Editor;
using SonicOrca.Core.Lighting;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Menu;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// A playable level. It owns the map, the objects, the camera, the HUDs and the level state, and it runs the level loop.
/// </summary>
public class Level : IDisposable
{
	// 10 minutes at 60 ticks each second.
	private const int TimeBeforeDeath = 36000;

	private const int FadeDuration = 60;

	/// <summary>The largest distance from the feet of a character at the start down to the floor that it starts on.</summary>
	private const int StartFloorDistance = 32;

	private const double ScreenWidth = ScreenSize.Width;

	private const double ScreenHeight = ScreenSize.Height;

	private const double CameraMaxVelocity = 64.0;

	private const double CameraVelocityRecovery = 0.5;

	// An object stays alive when it is in this distance of a character or of the camera.
	private const int ObjectLifeRadius = 1024;

	private const double EarthquakeMaxShake = 16.0;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly Player _player;

	private Random _random = TestHooks.CreateRandom();

	/// <summary>
	/// The seed of the random numbers while a recording plays or records, so that a recorded play gives the same
	/// result when it plays back.
	/// </summary>
	private const int RecordedPlaySeed = 0;

	private readonly FadeTransition _fadeTransition = new FadeTransition(FadeDuration);

	/// <summary>True while the level fades to white, for the special stage, not to black.</summary>
	private bool _fadeToWhite;

	private readonly ObjectManager _objectManager;

	private readonly CollisionTable _collisionTable;

	private readonly ParticleManager _particleManager;

	private readonly WaterManager _waterManager;

	private readonly SoundManager _soundManager;

	private readonly LightingManager _lightingManager;

	private readonly Dictionary<string, LevelMarker> _markers = new Dictionary<string, LevelMarker>();

	private readonly DebugContext _debugContext;

	private readonly LevelEditor _editor;

	private readonly LevelHud _hud;

	private readonly LevelCompleteHud _completeHud;

	private readonly GameOverHud _gameOverHud;

	private ILevelTitleCard _titleCard;

	private readonly Camera _camera;

	/// <summary>The ticks in which the zoom goes to the default zoom of the next act after a seamless change.</summary>
	private const int SeamlessZoomTicks = 60;

	private readonly LayerViewOptions _layerViewOptions = new LayerViewOptions();

	private readonly List<Tuple<int, Action>> _intervals = new List<Tuple<int, Action>>();

	private GhostCharacterInstance _ghostCharacter;

	private PlayRecorder _playRecorder;

	/// <summary>The input of player 2, the sidekick, from a recording (S2HD_TEST_PLAYER2_PLAYBACK), for tests.</summary>
	private PlayRecorder _player2Recorder = new PlayRecorder();

	private readonly CharacterInputState _player2Input = new CharacterInputState();

	private Rectanglei _targetBounds;

	private int _boundsScrollSpeed;

	private bool _cameraLockedSeamless;

	private LevelState _fadeOutToState;

	private readonly CommonResources _commonResources;

	private readonly IFadeTransitionRenderer _fadeTransitionRenderer;

	private readonly ILevelRenderer _levelRenderer;

	private readonly IReadOnlyList<string> CharacterTypeResourceKeys = new string[4] { null, "SONICORCA/OBJECTS/SONIC", "SONICORCA/OBJECTS/TAILS", "SONICORCA/OBJECTS/KNUCKLES" };

	private const double EarthquakeFadeAmount = 1.0 / 90.0;

	private bool _earthquakeIsCurrentlyActive;

	private double _earthquakeFade;

	private SampleInstance _earthquakeSampleInstance;

	public SonicOrcaGameContext GameContext => _gameContext;

	public Player Player => _player;

	/// <summary>Player 2 in versus mode, else null.</summary>
	private Player _player2;

	/// <summary>The players: player 1, and player 2 in versus mode.</summary>
	public IReadOnlyList<Player> Players => _player2 == null ? new[] { _player } : new[] { _player, _player2 };

	/// <summary>
	/// True in versus mode (see <see cref="LevelPrepareSettings.Versus"/>). The objects near the start start before
	/// the characters, so this does not wait for player 2.
	/// </summary>
	public bool Versus => PrepareSettings?.Versus == true;

	/// <summary>The camera of player 2 in versus mode, else null. <see cref="Camera"/> is the camera of player 1.</summary>
	private Camera _camera2;

	/// <summary>The cameras: the camera of player 1, and of player 2 in versus mode.</summary>
	public IReadOnlyList<Camera> Cameras => _camera2 == null ? new[] { _camera } : new[] { _camera, _camera2 };

	/// <summary>The rules of versus mode, or null outside versus mode and before the players start.</summary>
	public VersusRules VersusRules { get; private set; }

	public Area Area { get; private set; }

	public LevelPrepareSettings PrepareSettings { get; set; }

	/// <summary>
	/// The physics mode of the level: the mode of the prepare settings (a demo always plays in Original mode), the test
	/// hook S2HD_PHYSICS, or the game settings.
	/// </summary>
	public PhysicsMode Physics => PrepareSettings?.Physics ?? TestHooks.Physics ?? _gameContext.Physics;

	/// <summary>True when the level plays as the original Sonic 2 (<see cref="PhysicsMode.Original"/>).</summary>
	public bool OriginalPhysics => Physics == PhysicsMode.Original;

	public string Name { get; set; }

	public bool ShowAsZone { get; set; }

	public bool ShowAsAct { get; set; }

	public int CurrentAct { get; set; }

	public LevelScheme Scheme { get; set; }

	public IReadOnlyCollection<string> AnimalResourceKeys { get; set; }

	public string LevelMusic { get; set; }

	public Vector2i StartPosition { get; set; }

	/// <summary>
	/// The name of the marker that gave <see cref="StartPosition"/>, or null. The level editor shows the start at this
	/// marker, so that the start follows the marker when the user moves it.
	/// </summary>
	public string StartMarkerName { get; private set; }

	public int StartLayerIndex { get; set; }

	public Rectanglei Bounds { get; set; }

	public Rectanglei SeamlessNextBounds { get; set; }

	public bool SeamlessAct { get; set; }

	/// <summary>
	/// True from the first update of a boss until the level starts again, as Current_Boss_ID in the original. In
	/// Original physics the characters cannot leave the screen on the right during a boss fight.
	/// </summary>
	public bool BossFight { get; set; }

	public double NightMode { get; set; }

	/// <summary>
	/// The colour behind all the layers. The original game shows the backdrop colour of the palette where the
	/// background has no pattern. Transparent (the default) shows black.
	/// </summary>
	public Colour BackdropColour { get; set; }

	public bool FinishOnCompleteLevel { get; set; }

	public TileSet TileSet { get; set; }

	public LevelBinding Binding { get; set; }

	public LevelMap Map { get; set; }

	internal PlayRecorder PlayRecorder => _playRecorder;

	public LevelState State { get; set; }

	/// <summary>The level editor. It loads only when the game starts with the editor turned on.</summary>
	public LevelEditor Editor => _editor;

	public LevelStateFlags StateFlags { get; set; }

	public int Ticks { get; set; }

	public int Time { get; set; }

	public Camera Camera => _camera;

	/// <summary>The zoom of the level. Each camera shows the level at this zoom, and the HUD keeps its size.</summary>
	public LevelZoom Zoom { get; } = new LevelZoom();

	/// <summary>
	/// The zoom at the start of the level, from the attribute <c>zoom</c> of the level. A restart at a star post uses the
	/// zoom of the star post.
	/// </summary>
	public double DefaultZoom { get; set; } = 1.0;

	public bool IsScrollingBounds => _boundsScrollSpeed != 0;

	public LayerViewOptions LayerViewOptions => _layerViewOptions;

	public Random Random => _random;

	public ObjectManager ObjectManager => _objectManager;

	public CollisionTable CollisionTable => _collisionTable;

	public ParticleManager ParticleManager => _particleManager;

	public WaterManager WaterManager => _waterManager;

	public SoundManager SoundManager => _soundManager;

	public ILightingManager LightingManager => _lightingManager;

	public DebugContext DebugContext => _debugContext;

	public bool LandscapeAnimating { get; set; }

	public bool ObjectsAnimating { get; set; }

	public bool ShowCharacterInfo { get; set; }

	public bool ShowSidekickIntelligence { get; set; }

	public bool ClassicDebugMode { get; set; }

	public bool ShowHUD { get; set; }

	public int RingsCollected { get; set; }

	public int RingsPerfectTarget { get; set; }

	public CommonResources CommonResources => _commonResources;

	public ILevelRenderer LevelRenderer => _levelRenderer;

	public bool EarthquakeActive { get; set; }

	public Level(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_fadeTransitionRenderer = gameContext.Renderer.CreateFadeTransitionRenderer();
		_levelRenderer = gameContext.CreateLevelRenderer(this);
		_player = new Player(this);
		_collisionTable = new CollisionTable(this);
		_objectManager = new ObjectManager(this);
		_particleManager = new ParticleManager(this);
		_waterManager = new WaterManager(this);
		_soundManager = new SoundManager(this);
		_lightingManager = new LightingManager();
		_debugContext = new DebugContext(this);
		_editor = new LevelEditor(this);
		_hud = new LevelHud(this);
		_completeHud = new LevelCompleteHud(this);
		_gameOverHud = new GameOverHud(this);
		_camera = new Camera(this);
		_commonResources = new CommonResources(_gameContext);
		_playRecorder = new PlayRecorder();
		TileSet = new TileSet();
		Map = new LevelMap();
		LandscapeAnimating = true;
		ObjectsAnimating = true;
		ShowHUD = true;
		AnimalResourceKeys = new string[0];
	}

	public void Dispose()
	{
		if (_earthquakeSampleInstance != null)
		{
			_earthquakeSampleInstance.Dispose();
		}
		Unload();
		_playRecorder.Dispose();
		_player2Recorder.Dispose();
		_commonResources.Dispose();
		_particleManager.Dispose();
		_gameOverHud.Dispose();
		_completeHud.Dispose();
		_hud.Dispose();
		_debugContext.Dispose();
		_editor.Dispose();
		_fadeTransitionRenderer.Dispose();
	}

	public async Task LoadCommonAsync(CancellationToken ct = default(CancellationToken))
	{
		Trace.WriteLine("Loading common level components");
		await _debugContext.LoadAsync(ct);
		if (_gameContext.EditorEnabled)
		{
			await _editor.LoadAsync(ct);
		}
		await _hud.LoadAsync(ct);
		await _completeHud.LoadAsync(ct);
		await _gameOverHud.LoadAsync(ct);
		await _particleManager.LoadAsync(ct);
		await _levelRenderer.LoadAsync(ct);
		await _commonResources.LoadEntriesAsync(ct);
	}

	public async Task LoadAsync(Area area, CancellationToken ct = default(CancellationToken))
	{
		Trace.WriteLine("Loading level " + Name);
		Area = area;
		await _commonResources.LoadSchemeAsync(Scheme, ct);
		_titleCard = new Sonic2LevelTitleCard(this);
		await _titleCard.LoadAsync(_gameContext.ResourceTree, ct);
		_soundManager.SetJingle(JingleType.Life, _commonResources.GetResourcePath("1upjingle"));
		_soundManager.SetJingle(JingleType.Invincibility, _commonResources.GetResourcePath("invincibilityjingle"));
		_soundManager.SetJingle(JingleType.Drowning, _commonResources.GetResourcePath("drowningjingle"));
	}

	public void LoadMap(LevelMap map)
	{
		Trace.WriteLine("Loading level map");
		Map = map;
		Map.Level = this;
		_objectManager.Setup(Map);
		_collisionTable.Initialise(Map);
		_markers.Clear();
		foreach (LevelMarker marker in map.Markers)
		{
			if (!string.IsNullOrEmpty(marker.Name))
			{
				_markers[marker.Name.ToLower()] = marker;
			}
		}
		_levelRenderer.Initialise();
		ObjectPaletteCycles = LoadObjectPaletteCycles(map);
		Trace.WriteLine("Map loading complete");
	}

	/// <summary>The palette cycles of the sprites of the objects of the zone, from the import, or null.</summary>
	public ObjectPaletteCycles ObjectPaletteCycles { get; private set; }

	private ColourSwap[] _objectColourSwaps;

	private int _objectColourSwapsTick = -1;

	/// <summary>The colour swaps of the sprites of an object type at the current tick, or null when the type has none.</summary>
	public IReadOnlyList<ColourSwap> GetObjectColourSwaps(string objectKey)
	{
		if (ObjectPaletteCycles == null || !ObjectPaletteCycles.Uses(objectKey))
		{
			return null;
		}
		if (_objectColourSwapsTick != Ticks)
		{
			_objectColourSwaps = ObjectPaletteCycles.GetSwaps(Ticks);
			_objectColourSwapsTick = Ticks;
		}
		return _objectColourSwaps;
	}

	/// <summary>Reads the resource PALETTECYCLES next to the map. Only the import of a zone writes it.</summary>
	private ObjectPaletteCycles LoadObjectPaletteCycles(LevelMap map)
	{
		_objectColourSwapsTick = -1;
		Resource resource = map.Resource == null ? null : _gameContext.ResourceTree[map.Resource.GetAbsolutePath("//" + ObjectPaletteCycles.ResourceName)]?.Resource;
		if (resource == null)
		{
			return null;
		}
		try
		{
			using Stream stream = resource.Source.ReadUncompressed();
			return ObjectPaletteCycles.FromXml(stream);
		}
		catch (Exception ex) when (ex is IOException || ex is FormatException || ex is System.Xml.XmlException)
		{
			Trace.WriteLine("The palette cycles of the objects did not load: " + ex.Message);
			return null;
		}
	}

	public void LoadBinding(LevelBinding binding)
	{
		Trace.WriteLine("Loading level binding");
		Binding = binding;
		Binding.Level = this;
		_objectManager.Bind(Binding);
		Trace.WriteLine("Map binding complete");
	}

	public void UnloadCommon()
	{
		Trace.WriteLine("Unloading common level components");
		_debugContext.Dispose();
		_editor.Dispose();
		_hud.Dispose();
		_completeHud.Dispose();
		_gameOverHud.Dispose();
		_particleManager.Dispose();
		_levelRenderer.Dispose();
		_commonResources.Dispose();
	}

	public void Unload()
	{
		Trace.WriteLine("Unloading level " + Name);
		Stop();
		if (_titleCard != null)
		{
			_titleCard.Dispose();
		}
	}

	public void Start()
	{
		Trace.WriteLine("Starting level " + Name);
		_waterManager.Load();
		_fadeTransition.Clear();
		_fadeToWhite = false;
		_fadeTransition.Duration = FadeDuration;
		StateFlags = 0;
		Ticks = 0;
		BossFight = false;
		_intervals.Clear();
		InitialiseEarthquake();
		FinishOnCompleteLevel = true;
		NightMode = PrepareSettings.NightMode;
		if (!PrepareSettings.Seamless)
		{
			_objectManager.Start();
		}
		if (State == LevelState.Editing)
		{
			return;
		}
		_player.ResetScoreChain();
		_player.ResetRings();
		if (TestHooks.StartRings is int testRings && _player.StarpostIndex == -1)
		{
			_player.CurrentRings = testRings;
		}
		if (TestHooks.Emeralds is int testEmeralds)
		{
			_player.Emeralds = testEmeralds;
		}
		_player.RemovePowerups();
		StartZoom();
		if (PrepareSettings.Seamless)
		{
			_player.StarpostIndex = -1;
			_player.Protagonist.IsWinning = false;
			if (_player.Sidekick != null)
			{
				_player.Sidekick.IsWinning = false;
			}
			StateFlags |= LevelStateFlags.AllowCharacterControl;
			StateFlags |= LevelStateFlags.UpdateTime;
			_cameraLockedSeamless = true;
		}
		else
		{
			SetupCharacters();
			_cameraLockedSeamless = false;
		}
		StartTitleCard(PrepareSettings.Seamless);
		// A zone without music in the resources plays without music (see AreaResourceType)
		if (!string.IsNullOrEmpty(LevelMusic) && _gameContext.ResourceTree[LevelMusic]?.Resource != null)
		{
			_soundManager.PlayMusic(LevelMusic);
		}
		Time = ((_player.StarpostIndex != -1) ? GetStarpostStartTime() : 0);
		_hud.ShowMiliseconds = PrepareSettings.TimeTrial;
		ClassicDebugMode = PrepareSettings.Debugging;
		Controller.IsDebug = PrepareSettings.Debugging;
		if (!PrepareSettings.Seamless)
		{
			if (!string.IsNullOrEmpty(PrepareSettings.RecordedPlayReadPath))
			{
				_playRecorder.BeginPlaying(PrepareSettings.RecordedPlayReadPath);
			}
			else if (PrepareSettings.RecordedPlayReadData != null)
			{
				_playRecorder.BeginPlaying(new MemoryStream(PrepareSettings.RecordedPlayReadData));
			}
			else if (!string.IsNullOrEmpty(PrepareSettings.RecordedPlayGhostReadPath))
			{
				_playRecorder.BeginPlaying(PrepareSettings.RecordedPlayGhostReadPath);
			}
			// A recording with a header starts with the rings, the emeralds and the lives of the recorded play, so
			// that a play with emeralds (Super Sonic) plays back in the same way. A ghost does not change the player.
			if (_playRecorder.PlayingHeader is PlayRecorder.Header header && string.IsNullOrEmpty(PrepareSettings.RecordedPlayGhostReadPath))
			{
				_player.CurrentRings = header.Rings;
				_player.Emeralds = header.Emeralds;
				_player.Lives = header.Lives;
			}
			if (TestHooks.Player2Playback is string player2Path && !_player2Recorder.Playing)
			{
				_player2Recorder.BeginPlaying(player2Path);
			}
			if (!string.IsNullOrEmpty(PrepareSettings.RecordedPlayWritePath))
			{
				_playRecorder.BeginRecording(PrepareSettings.RecordedPlayWritePath,
					new PlayRecorder.Header { Rings = _player.CurrentRings, Emeralds = _player.Emeralds, Lives = _player.Lives });
			}
			if (_playRecorder.Playing || _playRecorder.Recording)
			{
				_random = TestHooks.CreateRandom(RecordedPlaySeed);
			}
		}
		State = LevelState.Playing;
		RingsCollected = 0;
		if (TileSet != null)
		{
			TileSet.UseAlternateAnimations = false;
		}
		Area.OnStart();
		Area.StartScript(this);
		SonicOrca.Core.Debugging.StoryTest.OnStart(this);
	}

	public void Stop()
	{
		Trace.WriteLine("Stopping level " + Name);
		if (_playRecorder != null)
		{
			_playRecorder.EndPlaying();
			_player2Recorder.EndPlaying();
			_playRecorder.EndRecording();
		}
		StopEarthquake();
		_soundManager.StopAll();
		_objectManager.Stop();
	}

	/// <summary>
	/// Sets the zoom at the start of the level: the zoom of the star post at a restart there, or else the default
	/// zoom of the level. The next act of a seamless change goes to its default zoom in one second.
	/// </summary>
	private void StartZoom()
	{
		if (PrepareSettings.Seamless)
		{
			Zoom.ZoomTo(DefaultZoom, SeamlessZoomTicks);
		}
		else
		{
			Zoom.Set(_player.StarpostIndex != -1 && _player.StarpostZoom is double zoom ? zoom : DefaultZoom);
			Trace.WriteLine($"Start zoom {Zoom.Current:0.###}");
		}
		ApplyZoom();
	}

	/// <summary>Gives each camera the zoom of this tick.</summary>
	private void ApplyZoom()
	{
		_camera.Zoom = Zoom.Current;
		if (_camera2 != null)
		{
			_camera2.Zoom = Zoom.Current;
		}
	}

	private void SetupCharacters()
	{
		Vector2i playerStartPosition = GetPlayerStartPosition();
		// The sidekick starts a short distance behind the protagonist.
		Vector2i sidekickStartPosition = playerStartPosition + new Vector2i(-64, 0);
		CharacterType protagonistType = PrepareSettings.ProtagonistCharacter;
		CharacterType sidekickType = PrepareSettings.SidekickCharacter;
		if (protagonistType == CharacterType.Null)
		{
			protagonistType = CharacterType.Sonic;
			sidekickType = CharacterType.Tails;
		}
		// Knuckles comes only from the import of "Knuckles in Sonic 2". Without it, Sonic plays.
		if (protagonistType == CharacterType.Knuckles && _gameContext.ResourceTree[CharacterTypeResourceKeys[(int)CharacterType.Knuckles]]?.Resource == null)
		{
			Trace.WriteLine("Knuckles is not imported (s2hd-data import-knuckles), so Sonic plays.");
			protagonistType = CharacterType.Sonic;
		}
		// In versus mode, Tails is the protagonist of player 2, not a sidekick
		_player2 = null;
		VersusRules = null;
		if (PrepareSettings.Versus)
		{
			sidekickType = CharacterType.Null;
		}
		string protagonistResourceKey = CharacterTypeResourceKeys[(int)protagonistType];
		string sidekickResourceKey = CharacterTypeResourceKeys[(int)sidekickType];
		// Create the sidekick first, so that the protagonist is added after it.
		// At a star post, the characters start on the path and the layer that the protagonist had there (Obj79_LoadData).
		bool atStarpost = _player.StarpostIndex != -1;
		int startPath = atStarpost ? _player.StarpostPath : PrepareSettings.StartPath;
		int startLayerIndex = (atStarpost && _player.StarpostLayerIndex >= 0) ? _player.StarpostLayerIndex : StartLayerIndex;
		ICharacter sidekick = (sidekickType != CharacterType.Null) ? CreateCharacter(sidekickResourceKey, sidekickStartPosition, startLayerIndex) : null;
		ICharacter protagonist = CreateCharacter(protagonistResourceKey, playerStartPosition, startLayerIndex);
		SetupCharacters(protagonist, sidekick, startPath);
		Player.ProtagonistCharacterType = protagonistType;
		Player.SidekickCharacterType = sidekickType;
		_camera2 = null;
		if (PrepareSettings.Versus)
		{
			SetupPlayer2(sidekickStartPosition, startLayerIndex, startPath);
			SetupVersusCameras();
			VersusRules = new VersusRules(this, new[]
			{
				(protagonist.Position, startPath, startLayerIndex),
				(_player2.Protagonist.Position, startPath, startLayerIndex)
			});
		}
		if (!string.IsNullOrEmpty(PrepareSettings.RecordedPlayGhostReadPath))
		{
			_ghostCharacter = CreateGhostCharacter(protagonist, playerStartPosition);
		}
	}

	private void SetupCharacters(ICharacter protagonist, ICharacter sidekick, int startPath)
	{
		protagonist.Player = Player;
		protagonist.Path = startPath;
		Player.Protagonist = protagonist;
		Player.ProtagonistGamepadIndex = 0;
		PlaceOnFloor(protagonist);
		if (sidekick != null)
		{
			sidekick.Player = Player;
			sidekick.IsSidekick = true;
			sidekick.Path = startPath;
			Player.Sidekick = sidekick;
			Player.SidekickGamepadIndex = 1;
			PlaceOnFloor(sidekick);
		}
		_camera.ObjectToTrack = (ActiveObject)protagonist;
		_camera.CentreObjectToTrack();
	}

	/// <summary>Makes player 2 of versus mode: Tails, with their own rings, score and lives, and the second controller.</summary>
	private void SetupPlayer2(Vector2i position, int layerIndex, int startPath)
	{
		_player2 = new Player(this) { Lives = PrepareSettings.Player2Lives ?? _player.Lives };
		ICharacter tails = CreateCharacter(CharacterTypeResourceKeys[(int)CharacterType.Tails], position, layerIndex);
		tails.Player = _player2;
		tails.Path = startPath;
		_player2.Protagonist = tails;
		_player2.ProtagonistCharacterType = CharacterType.Tails;
		_player2.ProtagonistGamepadIndex = 1;
		_player2.SidekickCharacterType = CharacterType.Null;
		PlaceOnFloor(tails);
	}

	/// <summary>
	/// Versus mode: each player has a camera for one half of the screen, player 1 at the top and player 2 at the
	/// bottom, as in the original.
	/// </summary>
	private void SetupVersusCameras()
	{
		_camera.ScreenBounds = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight / 2.0);
		_camera2 = new Camera(this)
		{
			ScreenBounds = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight / 2.0),
			ObjectToTrack = (ActiveObject)_player2.Protagonist,
			Zoom = Zoom.Current
		};
	}

	/// <summary>
	/// Versus mode: a player has no lives left. The level stops and shows the game over, and the match ends.
	/// </summary>
	internal void StartVersusGameOver()
	{
		StateFlags &= ~(LevelStateFlags.Animating | LevelStateFlags.Updating | LevelStateFlags.UpdateTime);
		StateFlags |= LevelStateFlags.Dead;
		StartGameOverSequence();
	}

	/// <summary>The character of player 2: the protagonist of player 2 in versus mode, else the sidekick, or null.</summary>
	private ICharacter Player2Character => _player2 != null ? _player2.Protagonist : _player.Sidekick;

	/// <summary>
	/// Puts a character on the floor under its start position. The start positions and the star post positions are
	/// for the height of the original Sonic. A shorter character would start in the air and fall when the level
	/// starts.
	/// </summary>
	private static void PlaceOnFloor(ICharacter character)
	{
		if (character is Character placedCharacter)
		{
			placedCharacter.PlaceOnFloor(StartFloorDistance);
		}
	}

	private ICharacter CreateCharacter(string resourceKey, Vector2i position, int layerIndex)
	{
		ObjectPlacement placement = new ObjectPlacement(resourceKey, layerIndex, position);
		ObjectEntry objectEntry = new ObjectEntry(this, placement);
		_objectManager.AddObjectEntry(objectEntry);
		ActiveObject activeObject = _objectManager.ActivateObjectEntry(objectEntry);
		activeObject.LockLifetime = true;
		return (ICharacter)activeObject;
	}

	private GhostCharacterInstance CreateGhostCharacter(ICharacter character, Vector2i position)
	{
		ObjectEntry objectEntry = new ObjectEntry(this, new GhostCharacterType(), Map.Layers[StartLayerIndex], position);
		GhostCharacterInstance ghostCharacter = (GhostCharacterInstance)_objectManager.ActivateObjectEntry(objectEntry);
		ghostCharacter.LockLifetime = true;
		ghostCharacter.Initialise(character.Animation.AnimationGroup);
		return ghostCharacter;
	}

	/// <summary>
	/// The time at the start from a star post. HD physics uses the time of the star post. Obj79_LoadData sets the
	/// frame of the saved time to 59 and takes 1 from the second, so the time goes back to the start of the second
	/// and 1 frame more.
	/// </summary>
	private int GetStarpostStartTime()
	{
		if (!OriginalPhysics)
		{
			return _player.StarpostTime;
		}
		return Math.Max(0, _player.StarpostTime / 60 * 60 - 1);
	}

	public Vector2i GetPlayerStartPosition()
	{
		Vector2i result = PrepareSettings.StartPosition ?? StartPosition;
		if (_player.StarpostIndex != -1)
		{
			result = _player.StarpostPosition;
		}
		return result;
	}

	public void Update()
	{
		if (StateFlags.HasFlag(LevelStateFlags.Paused))
		{
			return;
		}
		if (StateFlags.HasFlag(LevelStateFlags.TitleCardActive))
		{
			_titleCard.Update();
			// After a death, the level stays stopped. Otherwise the death takes a life on each tick of the fade.
			if (_titleCard.AllowLevelToStart && !StateFlags.HasFlag(LevelStateFlags.Dead))
			{
				StateFlags |= LevelStateFlags.Updating;
				// An act that ends while the title card shows keeps its timer stopped
				if ((StateFlags & (LevelStateFlags.CompletingStage | LevelStateFlags.WaitingForCharacterToWin | LevelStateFlags.StageCompleted)) == 0)
				{
					StateFlags |= LevelStateFlags.UpdateTime;
				}
				StateFlags |= LevelStateFlags.Animating;
				// The original unlocks the controls on the frame that the timer starts. HD physics waits for the title
				// card.
				if (OriginalPhysics)
				{
					StateFlags |= LevelStateFlags.AllowCharacterControl;
				}
			}
			if (_titleCard.AllowCharacterControl)
			{
				StateFlags |= LevelStateFlags.AllowCharacterControl;
			}
			if (_titleCard.Finished)
			{
				StateFlags &= ~LevelStateFlags.TitleCardActive;
			}
			if (_titleCard.Seamless && _titleCard.UnlockCamera && _cameraLockedSeamless)
			{
				_cameraLockedSeamless = false;
				Bounds = SeamlessNextBounds;
				Camera.MaxVelocity = new Vector2(0.0, 0.0);
			}
		}
		if (StateFlags.HasFlag(LevelStateFlags.CompletingStage))
		{
			if (_player.Sidekick != null && !_player.Sidekick.IsAirborne)
			{
				_player.Sidekick.IsWinning = true;
			}
			_completeHud.Update();
			if (_completeHud.Finished)
			{
				StateFlags &= ~LevelStateFlags.CompletingStage;
				StateFlags |= LevelStateFlags.StageCompleted;
				if (FinishOnCompleteLevel || PrepareSettings.TimeTrial)
				{
					if (!SeamlessAct)
					{
						FadeOut(LevelState.StageCompleted);
					}
					else
					{
						State = LevelState.StageCompleted;
					}
				}
				else
				{
					_player.Protagonist.IsWinning = false;
					if (_player.Sidekick != null)
					{
						_player.Sidekick.IsWinning = false;
					}
				}
			}
		}
		if (StateFlags.HasFlag(LevelStateFlags.TimeOver) || StateFlags.HasFlag(LevelStateFlags.GameOver))
		{
			_gameOverHud.Update();
			if (_gameOverHud.Finished)
			{
				StateFlags &= ~(LevelStateFlags.TimeOver | LevelStateFlags.GameOver);
				StateFlags |= LevelStateFlags.Dead;
				FadeOut(LevelState.Dead);
			}
		}
		PlayRecorder.Entry playbackEntry = null;
		if (_playRecorder.Playing)
		{
			playbackEntry = _playRecorder.GetNextEntry();
		}
		PlayRecorder.Entry player2Entry = _player2Recorder.Playing ? _player2Recorder.GetNextEntry() : null;
		// The teleport of versus mode stops the level for a short time
		if (StateFlags.HasFlag(LevelStateFlags.Updating) && VersusRules?.UpdateTeleport() != true)
		{
			if (playbackEntry != null)
			{
				// A recording without a ghost controls the protagonist. A ghost recording moves the ghost only.
				if (string.IsNullOrEmpty(PrepareSettings.RecordedPlayGhostReadPath))
				{
					ApplyRecordedInput(_player.Protagonist.Input, playbackEntry);
				}
				else
				{
					_ghostCharacter.Set(playbackEntry);
					ReadPlayerInput();
				}
			}
			else
			{
				ReadPlayerInput();
			}
			if (player2Entry != null && Player2Character is ICharacter player2Character)
			{
				// The input of player 2 stays from tick to tick, so that a held button is not a new press
				ApplyRecordedInput(_player2Input, player2Entry);
				player2Character.Input = _player2Input;
			}
			SonicOrca.Core.Debugging.StoryTest.Update(this);
			SetObjectLifetimeArea();
			_objectManager.Update();
			_particleManager.Update();
			_waterManager.Update();
			UpdateScrollBounds();
			Zoom.Update();
			ApplyZoom();
			_camera.Update();
			_camera2?.Update();
			VersusRules?.UpdateZoom();
			UpdateInverals();
			Player.Update();
			_player2?.Update();
			if (VersusRules != null)
			{
				VersusRules.Update();
			}
			else if (Player.Protagonist.IsDead)
			{
				if (_player.Lives > 0)
				{
					_player.Lives--;
				}
				StateFlags &= ~LevelStateFlags.Animating;
				StateFlags &= ~LevelStateFlags.Updating;
				StateFlags &= ~LevelStateFlags.UpdateTime;
				StateFlags |= LevelStateFlags.Dead;
				if (StateFlags.HasFlag(LevelStateFlags.TimeUp))
				{
					StartTimeOverSequence();
				}
				else if (Player.Lives == 0)
				{
					StartGameOverSequence();
				}
				else
				{
					FadeOut(LevelState.Dead);
				}
			}
			if (StateFlags.HasFlag(LevelStateFlags.WaitingForCharacterToWin))
			{
				CompleteLevel();
			}
			UpdateEarthquake();
		}
		else if (_earthquakeSampleInstance != null)
		{
			_earthquakeSampleInstance.Stop();
		}
		_soundManager.Update();
		if (StateFlags.HasFlag(LevelStateFlags.UpdateTime))
		{
			if (Time >= TimeBeforeDeath - 1)
			{
				_player.StarpostTime = 0;
				StateFlags &= ~LevelStateFlags.UpdateTime;
				StateFlags |= LevelStateFlags.TimeUp;
				Player.Protagonist.Kill(PlayerDeathCause.TimeOver);
			}
			else
			{
				Time++;
			}
		}
		_debugContext.Update();
		if (StateFlags.HasFlag(LevelStateFlags.FadingOut))
		{
			_fadeTransition.Update();
			if (_fadeTransition.Finished)
			{
				StateFlags &= ~LevelStateFlags.FadingOut;
				State = _fadeOutToState;
			}
		}
		if (_playRecorder.Recording)
		{
			CharacterInputState recordedInput = _player.Protagonist.Input;
			_playRecorder.WriteEntry(new PlayRecorder.Entry
			{
				Direction = new Vector2(recordedInput.Throttle, recordedInput.VerticalDirection),
				Action = recordedInput.ABC.HasFlag(CharacterInputButtonState.Down),
				Position = _player.Protagonist.Position,
				LayerIndex = Map.Layers.IndexOf(_player.Protagonist.Layer),
				State = (int)_player.Protagonist.StateFlags,
				AnimationIndex = _player.Protagonist.Animation.Index,
				AnimationFrameIndex = _player.Protagonist.Animation.CurrentFrameIndex,
				Angle = (float)_player.Protagonist.ShowAngle
			});
		}
		Ticks++;
		if (TestHooks.KillPlayerAtTick == Ticks && !TestHooks.KillPlayerTestDone && _player.Protagonist != null)
		{
			TestHooks.KillPlayerTestDone = true;
			_player.Protagonist.Kill(PlayerDeathCause.Hurt);
		}
		if (TestHooks.SpecialStageAtTick == Ticks && !TestHooks.SpecialStageTestDone)
		{
			TestHooks.SpecialStageTestDone = true;
			EnterSpecialStage();
		}
	}

	public void HandleInput()
	{
		if (!_gameContext.Console.IsOpen && _gameContext.Pressed[0].Start)
		{
			TogglePause();
		}
	}

	public void Animate()
	{
		if (!StateFlags.HasFlag(LevelStateFlags.Paused) && StateFlags.HasFlag(LevelStateFlags.Animating))
		{
			if (LandscapeAnimating)
			{
				TileSet.Animate();
			}
			Map.Animate();
			if (ObjectsAnimating)
			{
				_objectManager.Animate();
			}
			_waterManager.Animate();
			_hud.Animate();
		}
	}

	public void TogglePause()
	{
		if (StateFlags.HasFlag(LevelStateFlags.Paused))
		{
			Unpause();
		}
		else
		{
			Pause();
		}
	}

	public void Pause()
	{
		if (StateFlags.HasFlag(LevelStateFlags.Paused))
		{
			return;
		}
		// The player cannot pause while one of these states is active.
		LevelStateFlags blockingFlags = LevelStateFlags.FadingIn | LevelStateFlags.FadingOut | LevelStateFlags.CompletingStage | LevelStateFlags.Restarting | LevelStateFlags.TitleCardActive | LevelStateFlags.Dead | LevelStateFlags.StageCompleted | LevelStateFlags.TimeOver | LevelStateFlags.GameOver | LevelStateFlags.TimeUp;
		if ((StateFlags & blockingFlags) == 0)
		{
			StateFlags |= LevelStateFlags.Paused;
			SoundManager.PauseAll();
			if (_earthquakeSampleInstance != null)
			{
				_earthquakeSampleInstance.Stop();
			}
		}
	}

	public void Unpause()
	{
		if (StateFlags.HasFlag(LevelStateFlags.Paused) || StateFlags.HasFlag(LevelStateFlags.GameOver))
		{
			if (StateFlags.HasFlag(LevelStateFlags.Paused))
			{
				SoundManager.ResumeAll();
			}
			StateFlags &= ~LevelStateFlags.Paused;
			if (_earthquakeSampleInstance != null && _earthquakeIsCurrentlyActive)
			{
				_earthquakeSampleInstance.Play();
			}
		}
	}

	/// <summary>Gives a character the input of one tick of a recording.</summary>
	private static void ApplyRecordedInput(CharacterInputState input, PlayRecorder.Entry entry)
	{
		input.Throttle = entry.Direction.X;
		input.VerticalDirection = Math.Sign(entry.Direction.Y);
		if (entry.Action)
		{
			if (input.A == CharacterInputButtonState.Up || input.A == CharacterInputButtonState.Released)
			{
				input.A = CharacterInputButtonState.Pressed;
			}
			else
			{
				input.A = CharacterInputButtonState.Down;
			}
		}
		else if (input.A == CharacterInputButtonState.Down || input.A == CharacterInputButtonState.Pressed)
		{
			input.A = CharacterInputButtonState.Released;
		}
		else
		{
			input.A = CharacterInputButtonState.Up;
		}
	}

	private void ReadPlayerInput()
	{
		if (!_gameContext.Console.IsOpen && StateFlags.HasFlag(LevelStateFlags.AllowCharacterControl))
		{
			_player.Protagonist.Input = ReadCharacterInput(_player.ProtagonistGamepadIndex);
			if (_player.Sidekick != null)
			{
				_player.Sidekick.Input = ReadCharacterInput(_player.SidekickGamepadIndex);
			}
			if (_player2?.Protagonist != null)
			{
				_player2.Protagonist.Input = ReadCharacterInput(_player2.ProtagonistGamepadIndex);
			}
		}
	}

	private CharacterInputState ReadCharacterInput(int gamepadIndex)
	{
		Controller current = _gameContext.Current[gamepadIndex];
		Controller pressed = _gameContext.Pressed[gamepadIndex];
		Controller released = _gameContext.Released[gamepadIndex];
		return new CharacterInputState
		{
			Throttle = current.DirectionLeft.X,
			VerticalDirection = Math.Sign(current.DirectionLeft.Y),
			A = GetButtonState(current.Action1, pressed.Action1, released.Action1),
			B = GetButtonState(current.Action2, pressed.Action2, released.Action2),
			C = GetButtonState(current.Action3, pressed.Action3, released.Action3)
		};
	}

	private static CharacterInputButtonState GetButtonState(bool down, bool pressed, bool released)
	{
		if (!down)
		{
			if (!released)
			{
				return CharacterInputButtonState.Up;
			}
			return CharacterInputButtonState.Released;
		}
		if (!pressed)
		{
			return CharacterInputButtonState.Down;
		}
		return CharacterInputButtonState.Pressed;
	}

	/// <summary>
	/// Pauses the level and opens the level editor. The sounds stop. To leave the editor, the level starts again,
	/// see <see cref="LevelGameState"/>.
	/// </summary>
	public void EnterEditor()
	{
		StopEarthquake();
		_soundManager.StopAll();
		StateFlags &= ~LevelStateFlags.Paused;
		State = LevelState.Editing;
		_editor.Begin();
	}

	public void Draw(Renderer renderer)
	{
		TestHooks.DrawnLevelTick = State == LevelState.Editing ? TestHooks.EditorCaptureOffset + TestHooks.EditorFrame : Ticks;
		Viewport viewport = new Viewport(new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight / (_camera2 == null ? 1.0 : 2.0)));
		viewport.Bounds = _camera.Bounds;
		_levelRenderer.Render(renderer, viewport, _layerViewOptions);
		if (_camera2 != null && State != LevelState.Editing)
		{
			// Versus mode: player 2 in the bottom half, and a line between the halves
			Viewport viewport2 = new Viewport(new Rectangle(0.0, ScreenHeight / 2.0, ScreenWidth, ScreenHeight / 2.0));
			viewport2.Bounds = _camera2.Bounds;
			_levelRenderer.Render(renderer, viewport2, _layerViewOptions);
			I2dRenderer g = renderer.Get2dRenderer();
			// The copy of the bottom half leaves the clip on the bottom half. The line, the HUD and the title card are
			// on all the screen.
			g.ClipRectangle = new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight);
			g.ModelMatrix = Matrix4.Identity;
			g.BlendMode = BlendMode.Opaque;
			g.RenderQuad(Colours.Black, new Rectangle(0.0, ScreenHeight / 2.0 - 2.0, ScreenWidth, 4.0));
			if (VersusRules?.TeleportFlash is double flash && flash > 0.0)
			{
				// The white flash of the teleport goes away in its last ticks
				g.BlendMode = BlendMode.Alpha;
				g.RenderQuad(new Colour(0.75 * flash, 1.0, 1.0, 1.0), new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight));
			}
			renderer.DeativateRenderer();
		}
		if (_layerViewOptions.ShowObjectCollision)
		{
			DrawObjectCollision(renderer, viewport);
		}
		if (State == LevelState.Editing)
		{
			_editor.Draw(renderer);
			return;
		}
		_camera.Draw(renderer);
		if (ShowHUD)
		{
			_hud.Draw(renderer);
		}
		if (_gameContext.EditorEnabled)
		{
			_editor.DrawPlayReminder(renderer);
		}
		if (StateFlags.HasFlag(LevelStateFlags.CompletingStage))
		{
			_completeHud.Draw(renderer);
		}
		if (StateFlags.HasFlag(LevelStateFlags.TimeOver) || StateFlags.HasFlag(LevelStateFlags.GameOver))
		{
			_gameOverHud.Draw(renderer);
		}
		if (StateFlags.HasFlag(LevelStateFlags.TitleCardActive))
		{
			_titleCard.Draw(renderer);
		}
		DrawDebugThings(renderer);
		renderer.DeativateRenderer();
		// A positive value fades to white, a negative value to black
		_fadeTransitionRenderer.Opacity = (float)(_fadeToWhite ? _fadeTransition.Opacity : 0.0 - _fadeTransition.Opacity);
		_fadeTransitionRenderer.Render();
	}

	private static readonly Colour HitboxColour = new Colour(0.9, 1.0, 0.0, 0.0);

	/// <summary>Draws the hit boxes of the active objects, for <see cref="LayerViewOptions.ShowObjectCollision"/>.</summary>
	private void DrawObjectCollision(Renderer renderer, Viewport viewport)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		using (g.BeginMatixState())
		{
			// The level renderer leaves the scale of the zoom in the model matrix; the viewport gives the scale here
			g.ModelMatrix = Matrix4.Identity;
			g.BlendMode = BlendMode.Alpha;
			Rectangle view = viewport.Bounds;
			Vector2 scale = viewport.Scale;
			foreach (ActiveObject activeObject in _objectManager.ActiveObjects)
			{
				foreach (Rectanglei hitbox in activeObject.Hitboxes)
				{
					Rectangle bounds = ((Rectangle)hitbox).OffsetBy(activeObject.Position);
					if (bounds.IntersectsWith(view))
					{
						g.RenderRectangle(HitboxColour, new Rectangle((bounds.X - view.X) * scale.X, (bounds.Y - view.Y) * scale.Y, bounds.Width * scale.X, bounds.Height * scale.Y), 2.0);
					}
				}
			}
		}
		renderer.DeativateRenderer();
	}

	private void DrawDebugThings(Renderer renderer)
	{
		if (ShowCharacterInfo)
		{
			(Player.Protagonist as Character).DrawNewCollisionDebug(renderer);
		}
		_debugContext.Draw(renderer);
	}

	private void StartTitleCard(bool seamless = false)
	{
		StateFlags |= LevelStateFlags.TitleCardActive;
		_titleCard.Seamless = seamless;
		_titleCard.Start();
	}

	private void StartCompleteStageHud()
	{
		StateFlags |= LevelStateFlags.CompletingStage;
		_completeHud.Start();
	}

	private void StartTimeOverSequence()
	{
		StateFlags &= ~(LevelStateFlags.Animating | LevelStateFlags.Updating);
		StateFlags |= LevelStateFlags.TimeOver;
		_gameOverHud.Start(GameOverHud.Reason.TimeOver);
	}

	private void StartGameOverSequence()
	{
		StateFlags &= ~(LevelStateFlags.Animating | LevelStateFlags.Updating);
		StateFlags |= LevelStateFlags.GameOver;
		_gameOverHud.Start(GameOverHud.Reason.GameOver);
	}

	/// <summary>
	/// The player entered the star circle. The original leaves the level at once: the level stops, and the screen
	/// fades to white before the special stage.
	/// </summary>
	public void EnterSpecialStage()
	{
		StateFlags &= ~(LevelStateFlags.Updating | LevelStateFlags.UpdateTime | LevelStateFlags.Animating);
		_fadeToWhite = true;
		FadeOut(LevelState.SpecialStage);
	}

	/// <summary>Fades the screen to white in a number of frames, then changes the state of the level.</summary>
	public void FadeOutToWhite(LevelState finishingState, int frames)
	{
		_fadeToWhite = true;
		_fadeTransition.Duration = frames;
		FadeOut(finishingState);
	}

	public void FadeOut(LevelState finishingState)
	{
		_soundManager.FadeOutMusic(_fadeTransition.Duration);
		_fadeTransition.BeginFadeOut();
		StateFlags |= LevelStateFlags.FadingOut;
		_fadeOutToState = finishingState;
	}

	public void JustAboutToCompleteLevel()
	{
		StateFlags &= ~LevelStateFlags.UpdateTime;
		Camera.CentreObjectToTrack();
		Rectangle rectangle = Bounds;
		rectangle.Left = Camera.Bounds.Left;
		rectangle.Right = Camera.Bounds.Right;
		Bounds = rectangle;
		_player.RemovePowerups();
	}

	public void CompleteLevel()
	{
		ICharacter protagonist = _player.Protagonist;
		if (protagonist.IsAirborne)
		{
			StateFlags |= LevelStateFlags.WaitingForCharacterToWin;
			return;
		}
		StateFlags &= ~LevelStateFlags.WaitingForCharacterToWin;
		protagonist.IsWinning = true;
		StartCompleteStageHud();
	}

	public void SetInterval(int ticks, Action callback)
	{
		_intervals.Add(new Tuple<int, Action>(Ticks + ticks, callback));
	}

	private void UpdateInverals()
	{
		// Item1 is the tick at which the callback runs. Item2 is the callback. A callback that is due runs once,
		// also when its tick passed while the level did not update. A callback can add new intervals.
		Tuple<int, Action>[] due = _intervals.Where(x => x.Item1 <= Ticks).ToArray();
		_intervals.RemoveAll(x => x.Item1 <= Ticks);
		foreach (Tuple<int, Action> interval in due)
		{
			interval.Item2();
		}
	}

	public void ScrollBoundsTo(Rectanglei newBounds, int speed)
	{
		_targetBounds = newBounds;
		_boundsScrollSpeed = speed;
		Bounds = Rectanglei.Union(_camera.Bounds, newBounds);
	}

	private void UpdateScrollBounds()
	{
		if (!_cameraLockedSeamless)
		{
			Vector2 maxVelocity = Camera.MaxVelocity;
			// After a seamless act change, the camera speed limit goes back up a little on each tick.
			if (maxVelocity.X < CameraMaxVelocity)
			{
				maxVelocity.X = Math.Min(maxVelocity.X + CameraVelocityRecovery, CameraMaxVelocity);
				maxVelocity.Y = maxVelocity.X;
				Camera.MaxVelocity = maxVelocity;
			}
		}
		if (_boundsScrollSpeed != 0)
		{
			Rectanglei bounds = Bounds;
			bounds.Left = MathX.GoTowards(bounds.Left, _targetBounds.Left, _boundsScrollSpeed);
			bounds.Top = MathX.GoTowards(bounds.Top, _targetBounds.Top, _boundsScrollSpeed);
			bounds.Right = MathX.GoTowards(bounds.Right, _targetBounds.Right, _boundsScrollSpeed);
			bounds.Bottom = MathX.GoTowards(bounds.Bottom, _targetBounds.Bottom, _boundsScrollSpeed);
			Bounds = bounds;
			if (bounds == _targetBounds)
			{
				_boundsScrollSpeed = 0;
			}
		}
	}

	private void SetObjectLifetimeArea()
	{
		_objectManager.ResetLifetimeArea();
		foreach (ICharacter character in _objectManager.Characters)
		{
			_objectManager.AddLifeArea(new Rectanglei(character.Position.X - ObjectLifeRadius, character.Position.Y - ObjectLifeRadius, ObjectLifeRadius * 2, ObjectLifeRadius * 2));
		}
		foreach (Camera camera in Cameras)
		{
			Rectanglei area = ((Rectanglei)camera.Bounds).Inflate(new Vector2i(ObjectLifeRadius, ObjectLifeRadius));
			_objectManager.AddLifeArea(area);
		}
	}

	public void CreateScoreObject(int points, Vector2i position)
	{
		if (Map.Layers.Count != 0)
		{
			_objectManager.AddObject(new ObjectPlacement(_commonResources.GetResourcePath("pointsobject"), Map.Layers.IndexOf(Map.Layers.Last()), position, new
			{
				Value = points
			}));
		}
	}

	public void CreateRandomAnimalObject(int layer, Vector2i position, int direction = -1, int delay = 0)
	{
		if (Map.Layers.Count != 0)
		{
			string randomAnimalResourceKey = GetRandomAnimalResourceKey();
			if (!string.IsNullOrEmpty(randomAnimalResourceKey))
			{
				_objectManager.AddObject(new ObjectPlacement(randomAnimalResourceKey, layer, position, new
				{
					Delay = delay,
					Direction = direction
				}));
			}
		}
	}

	private string GetRandomAnimalResourceKey()
	{
		string[] keys = AnimalResourceKeys.ToArray();
		if (keys.Length != 0)
		{
			return keys[_random.Next(keys.Length)];
		}
		return null;
	}

	public LevelMarker GetMarker(string name)
	{
		if (TryGetMarker(name, out LevelMarker marker))
		{
			return marker;
		}
		throw new ArgumentException("No marker named " + name);
	}

	public bool TryGetMarker(string name, out LevelMarker marker)
	{
		return _markers.TryGetValue(name, out marker);
	}

	public void SetStartPosition(string markerName)
	{
		SetStartPosition(GetMarker(markerName));
	}

	public void SetStartPosition(LevelMarker marker)
	{
		StartPosition = marker.Position;
		StartLayerIndex = Map.Layers.IndexOf(marker.Layer);
		StartMarkerName = marker.Name;
	}

	public LevelMarker[] GetMarkersWithTag(string tag)
	{
		List<LevelMarker> markers = new List<LevelMarker>();
		foreach (LevelMarker marker in Map.Markers)
		{
			if (string.Compare(tag, marker.Tag, ignoreCase: true) == 0)
			{
				markers.Add(marker);
			}
		}
		return markers.ToArray();
	}

	private void InitialiseEarthquake()
	{
		_earthquakeIsCurrentlyActive = false;
		_earthquakeFade = 0.0;
		EarthquakeActive = false;
	}

	private void StopEarthquake()
	{
		_earthquakeIsCurrentlyActive = false;
		_earthquakeFade = 0.0;
		EarthquakeActive = false;
		if (_earthquakeSampleInstance != null)
		{
			_earthquakeSampleInstance.Stop();
		}
	}

	private void UpdateEarthquake()
	{
		GameContext.Output[0] = default;
		if (!EarthquakeActive && _earthquakeFade == 0.0)
		{
			return;
		}
		if (EarthquakeActive && _earthquakeFade < 1.0)
		{
			if (_earthquakeSampleInstance == null)
			{
				SampleInfo sampleInfo = _gameContext.ResourceTree.GetLoadedResource<SampleInfo>(_commonResources.GetResourcePath("earthquakesample"));
				_earthquakeSampleInstance = new SampleInstance(_gameContext, sampleInfo);
			}
			if (!_earthquakeSampleInstance.Playing)
			{
				_earthquakeSampleInstance.SeekToStart();
				_earthquakeSampleInstance.Play();
			}
			_earthquakeIsCurrentlyActive = true;
			_earthquakeFade = Math.Min(_earthquakeFade + EarthquakeFadeAmount, 1.0);
		}
		else if (_earthquakeIsCurrentlyActive && _earthquakeFade > 0.0)
		{
			_earthquakeFade -= EarthquakeFadeAmount;
			if (_earthquakeFade <= 0.0)
			{
				_earthquakeFade = 0.0;
				_earthquakeIsCurrentlyActive = false;
				if (_earthquakeSampleInstance != null)
				{
					_earthquakeSampleInstance.Stop();
				}
				return;
			}
		}
		_earthquakeSampleInstance.Volume = _earthquakeFade;
		// Shake the camera and vibrate the gamepad by an amount that follows the fade.
		int shake = (int)(_earthquakeFade * EarthquakeMaxShake);
		_camera.Shift(_random.Next(-shake, shake), _random.Next(-shake, shake));
		double maxVibration = _earthquakeFade;
		GameContext.Output[0] = new GamePadOutputState
		{
			LeftVibration = Math.Min(maxVibration, 0.5 + _random.NextDouble() * 0.5),
			RightVibration = Math.Min(maxVibration, 0.5 + _random.NextDouble() * 0.5)
		};
	}
}
