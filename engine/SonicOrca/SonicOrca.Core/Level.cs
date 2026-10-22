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
using SonicOrca.Core.Lighting;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Menu;

namespace SonicOrca.Core;

public class Level : IDisposable
{
	private int TimeBeforeDeath = 36000;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly Player _player;

	private readonly Random _random = new Random();

	private readonly FadeTransition _fadeTransition = new FadeTransition(60);

	private readonly ObjectManager _objectManager;

	private readonly CollisionTable _collisionTable;

	private readonly ParticleManager _particleManager;

	private readonly WaterManager _waterManager;

	private readonly SoundManager _soundManager;

	private readonly LightingManager _lightingManager;

	private readonly Dictionary<string, LevelMarker> _markers = new Dictionary<string, LevelMarker>();

	private readonly DebugContext _debugContext;

	private readonly LevelHud _hud;

	private readonly LevelCompleteHud _completeHud;

	private readonly GameOverHud _gameOverHud;

	private ILevelTitleCard _titleCard;

	private readonly Camera _camera;

	private readonly LayerViewOptions _layerViewOptions = new LayerViewOptions();

	private readonly List<Tuple<int, Action>> _intervals = new List<Tuple<int, Action>>();

	private GhostCharacterInstance _ghostCharacter;

	private PlayRecorder _playRecorder;

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

	public Area Area { get; private set; }

	public LevelPrepareSettings PrepareSettings { get; set; }

	public string Name { get; set; }

	public bool ShowAsZone { get; set; }

	public bool ShowAsAct { get; set; }

	public int CurrentAct { get; set; }

	public LevelScheme Scheme { get; set; }

	public IReadOnlyCollection<string> AnimalResourceKeys { get; set; }

	public string LevelMusic { get; set; }

	public Vector2i StartPosition { get; set; }

	public int StartLayerIndex { get; set; }

	public Rectanglei Bounds { get; set; }

	public Rectanglei SeamlessNextBounds { get; set; }

	public bool SeamlessAct { get; set; }

	public double NightMode { get; set; }

	public bool FinishOnCompleteLevel { get; set; }

	public TileSet TileSet { get; set; }

	public LevelBinding Binding { get; set; }

	public LevelMap Map { get; set; }

	internal PlayRecorder PlayRecorder => _playRecorder;

	public LevelState State { get; set; }

	public LevelStateFlags StateFlags { get; set; }

	public int Ticks { get; set; }

	public int Time { get; set; }

	public Camera Camera => _camera;

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
		_commonResources.Dispose();
		_particleManager.Dispose();
		_gameOverHud.Dispose();
		_completeHud.Dispose();
		_hud.Dispose();
		_debugContext.Dispose();
		_fadeTransitionRenderer.Dispose();
	}

	public async Task LoadCommonAsync(CancellationToken ct = default(CancellationToken))
	{
		Trace.WriteLine("Loading common level components");
		await _debugContext.LoadAsync(ct);
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
		Trace.WriteLine("Map loading complete");
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
		StateFlags = (LevelStateFlags)0;
		Ticks = 0;
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
		_player.RemovePowerups();
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
		_soundManager.PlayMusic(LevelMusic);
		Time = ((_player.StarpostIndex != -1) ? _player.StarpostTime : 0);
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
			if (!string.IsNullOrEmpty(PrepareSettings.RecordedPlayWritePath))
			{
				_playRecorder.BeginRecording(PrepareSettings.RecordedPlayWritePath);
			}
		}
		State = LevelState.Playing;
		RingsCollected = 0;
		Area.OnStart();
	}

	public void Stop()
	{
		Trace.WriteLine("Stopping level " + Name);
		if (_playRecorder != null)
		{
			_playRecorder.EndPlaying();
			_playRecorder.EndRecording();
		}
		StopEarthquake();
		_soundManager.StopAll();
		_objectManager.Stop();
	}

	private void SetupCharacters()
	{
		Vector2i playerStartPosition = GetPlayerStartPosition();
		Vector2i position = playerStartPosition + new Vector2i(-64, 0);
		CharacterType characterType = PrepareSettings.ProtagonistCharacter;
		CharacterType characterType2 = PrepareSettings.SidekickCharacter;
		if (characterType == CharacterType.Null)
		{
			characterType = CharacterType.Sonic;
			characterType2 = CharacterType.Tails;
		}
		string resourceKey = CharacterTypeResourceKeys[(int)characterType];
		string resourceKey2 = CharacterTypeResourceKeys[(int)characterType2];
		ICharacter sidekick = ((characterType2 != CharacterType.Null) ? CreateCharacter(resourceKey2, position) : null);
		ICharacter character = CreateCharacter(resourceKey, playerStartPosition);
		SetupCharacters(character, sidekick);
		Player.ProtagonistCharacterType = characterType;
		Player.SidekickCharacterType = characterType2;
		if (!string.IsNullOrEmpty(PrepareSettings.RecordedPlayGhostReadPath))
		{
			_ghostCharacter = CreateGhostCharacter(character, playerStartPosition);
		}
	}

	private void SetupCharacters(ICharacter protagonist, ICharacter sidekick)
	{
		_camera.ObjectToTrack = (ActiveObject)protagonist;
		_camera.CentreObjectToTrack();
		protagonist.Player = Player;
		protagonist.Path = PrepareSettings.StartPath;
		Player.Protagonist = protagonist;
		Player.ProtagonistGamepadIndex = 0;
		if (sidekick != null)
		{
			sidekick.Player = Player;
			sidekick.IsSidekick = true;
			sidekick.Path = PrepareSettings.StartPath;
			Player.Sidekick = sidekick;
			Player.SidekickGamepadIndex = 1;
		}
	}

	private ICharacter CreateCharacter(string resourceKey, Vector2i position)
	{
		ObjectPlacement placement = new ObjectPlacement(resourceKey, Map.Layers.IndexOf(Map.Layers[StartLayerIndex]), position);
		ObjectEntry objectEntry = new ObjectEntry(this, placement);
		_objectManager.AddObjectEntry(objectEntry);
		ActiveObject activeObject = _objectManager.ActivateObjectEntry(objectEntry);
		activeObject.LockLifetime = true;
		return (ICharacter)activeObject;
	}

	private GhostCharacterInstance CreateGhostCharacter(ICharacter character, Vector2i position)
	{
		ObjectEntry objectEntry = new ObjectEntry(this, new GhostCharacterType(), Map.Layers[StartLayerIndex], position);
		GhostCharacterInstance ghostCharacterInstance = (GhostCharacterInstance)_objectManager.ActivateObjectEntry(objectEntry);
		ghostCharacterInstance.LockLifetime = true;
		ghostCharacterInstance.Initialise(character.Animation.AnimationGroup);
		return ghostCharacterInstance;
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
			if (_titleCard.AllowLevelToStart)
			{
				StateFlags |= LevelStateFlags.Updating;
				StateFlags |= LevelStateFlags.UpdateTime;
				StateFlags |= LevelStateFlags.Animating;
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
		PlayRecorder.Entry entry = null;
		if (_playRecorder.Playing)
		{
			entry = _playRecorder.GetNextEntry();
		}
		if (StateFlags.HasFlag(LevelStateFlags.Updating))
		{
			if (entry != null)
			{
				if (string.IsNullOrEmpty(PrepareSettings.RecordedPlayGhostReadPath))
				{
					CharacterInputState input = _player.Protagonist.Input;
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
				else
				{
					_ghostCharacter.Set(entry);
					ReadPlayerInput();
				}
			}
			else
			{
				ReadPlayerInput();
			}
			SetObjectLifetimeArea();
			_objectManager.Update();
			_particleManager.Update();
			_waterManager.Update();
			UpdateScrollBounds();
			_camera.Update();
			UpdateInverals();
			Player.Update();
			if (Player.Protagonist.IsDead)
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
			CharacterInputState input2 = _player.Protagonist.Input;
			_playRecorder.WriteEntry(new PlayRecorder.Entry
			{
				Direction = new Vector2(input2.Throttle, input2.VerticalDirection),
				Action = input2.ABC.HasFlag(CharacterInputButtonState.Down),
				Position = _player.Protagonist.Position,
				LayerIndex = Map.Layers.IndexOf(_player.Protagonist.Layer),
				State = (int)_player.Protagonist.StateFlags,
				AnimationIndex = _player.Protagonist.Animation.Index,
				AnimationFrameIndex = _player.Protagonist.Animation.CurrentFrameIndex,
				Angle = (float)_player.Protagonist.ShowAngle
			});
		}
		Ticks++;
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
		LevelStateFlags levelStateFlags = LevelStateFlags.FadingIn | LevelStateFlags.FadingOut | LevelStateFlags.CompletingStage | LevelStateFlags.Restarting | LevelStateFlags.TitleCardActive | LevelStateFlags.Dead | LevelStateFlags.StageCompleted | LevelStateFlags.TimeOver | LevelStateFlags.GameOver | LevelStateFlags.TimeUp;
		if ((StateFlags & levelStateFlags) == 0)
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

	private void ReadPlayerInput()
	{
		if (!_gameContext.Console.IsOpen && StateFlags.HasFlag(LevelStateFlags.AllowCharacterControl))
		{
			Controller controller = _gameContext.Current[_player.ProtagonistGamepadIndex];
			Controller controller2 = _gameContext.Pressed[_player.ProtagonistGamepadIndex];
			Controller controller3 = _gameContext.Released[_player.ProtagonistGamepadIndex];
			_player.Protagonist.Input = new CharacterInputState
			{
				Throttle = controller.DirectionLeft.X,
				VerticalDirection = Math.Sign(controller.DirectionLeft.Y),
				A = GetButtonState(controller.Action1, controller2.Action1, controller3.Action1),
				B = GetButtonState(controller.Action2, controller2.Action2, controller3.Action2),
				C = GetButtonState(controller.Action3, controller2.Action3, controller3.Action3)
			};
			controller = _gameContext.Current[_player.SidekickGamepadIndex];
			controller2 = _gameContext.Pressed[_player.SidekickGamepadIndex];
			controller3 = _gameContext.Released[_player.SidekickGamepadIndex];
			if (_player.Sidekick != null)
			{
				_player.Sidekick.Input = new CharacterInputState
				{
					Throttle = controller.DirectionLeft.X,
					VerticalDirection = Math.Sign(controller.DirectionLeft.Y),
					A = GetButtonState(controller.Action1, controller2.Action1, controller3.Action1),
					B = GetButtonState(controller.Action2, controller2.Action2, controller3.Action2),
					C = GetButtonState(controller.Action3, controller2.Action3, controller3.Action3)
				};
			}
		}
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

	public void Draw(Renderer renderer)
	{
		Viewport viewport = new Viewport(new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		viewport.Bounds = _camera.Bounds;
		_levelRenderer.Render(renderer, viewport, _layerViewOptions);
		_camera.Draw(renderer);
		if (ShowHUD)
		{
			_hud.Draw(renderer);
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
		_fadeTransitionRenderer.Opacity = (float)(0.0 - _fadeTransition.Opacity);
		_fadeTransitionRenderer.Render();
	}

	private void DrawMusicFrequency(Renderer renderer)
	{
		if (_soundManager == null || _soundManager.MusicInstance == null || _soundManager.MusicInstance.LastReadBytes == null || _soundManager.MusicInstance.LastReadBytes.Count == 0)
		{
			return;
		}
		int num = Sample.PCMToSamples(_soundManager.MusicInstance.LastReadBytes.ToArray(), out var leftSamples, out var _);
		ComplexNumber[] array = new ComplexNumber[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new ComplexNumber(leftSamples[i], 0.0);
		}
		num = 256;
		array = array.GetRange(0, num);
		FastFourierTransform.TimeToFrequency((int)Math.Log(array.Length, 2.0), array);
		int num2 = 1024 / (num / 2);
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.RenderQuad(new Colour(76, 0, 0, 0), new Rectangle(0.0, 0.0, 1024.0, 1024.0));
		for (int j = 0; j < array.Length / 2; j++)
		{
			ComplexNumber complexNumber = array[j];
			double num3 = 10.0 * Math.Log10(complexNumber.Magnitude);
			double num4 = -90.0;
			if (num3 < num4)
			{
				num3 = num4;
			}
			num3 = num3 / num4 * 1024.0;
			num3 = 1024.0 - num3;
			i2dRenderer.RenderQuad(Colours.White, new Rectangle(j * num2, 1024.0 - num3, num2, num3));
		}
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

	public void FadeOut(LevelState finishingState)
	{
		_soundManager.FadeOutMusic(60);
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
		foreach (Tuple<int, Action> item in _intervals.Where((Tuple<int, Action> x) => x.Item1 <= Ticks))
		{
			item.Item2();
		}
		_intervals.RemoveAll((Tuple<int, Action> x) => x.Item1 == Ticks);
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
			if (maxVelocity.X < 64.0)
			{
				maxVelocity.X = Math.Min(maxVelocity.X + 0.5, 64.0);
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
		int num = 1024;
		foreach (ICharacter character in _objectManager.Characters)
		{
			_objectManager.AddLifeArea(new Rectanglei(character.Position.X - num, character.Position.Y - num, num * 2, num * 2));
		}
		Rectanglei area = ((Rectanglei)_camera.Bounds).Inflate(new Vector2i(num, num));
		_objectManager.AddLifeArea(area);
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
		string[] array = AnimalResourceKeys.ToArray();
		if (array.Length != 0)
		{
			return array[_random.Next(array.Length)];
		}
		return null;
	}

	public LevelMarker GetMarker(string name)
	{
		if (TryGetMarker(name, out var marker))
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
	}

	public LevelMarker[] GetMarkersWithTag(string tag)
	{
		List<LevelMarker> list = new List<LevelMarker>();
		foreach (LevelMarker marker in Map.Markers)
		{
			if (string.Compare(tag, marker.Tag, ignoreCase: true) == 0)
			{
				list.Add(marker);
			}
		}
		return list.ToArray();
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
				SampleInfo loadedResource = _gameContext.ResourceTree.GetLoadedResource<SampleInfo>(_commonResources.GetResourcePath("earthquakesample"));
				_earthquakeSampleInstance = new SampleInstance(_gameContext, loadedResource);
			}
			if (!_earthquakeSampleInstance.Playing)
			{
				_earthquakeSampleInstance.SeekToStart();
				_earthquakeSampleInstance.Play();
			}
			_earthquakeIsCurrentlyActive = true;
			_earthquakeFade = Math.Min(_earthquakeFade + 1.0 / 90.0, 1.0);
		}
		else if (_earthquakeIsCurrentlyActive && _earthquakeFade > 0.0)
		{
			_earthquakeFade -= 1.0 / 90.0;
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
		int num = (int)(_earthquakeFade * 16.0);
		_camera.Shift(_random.Next(-num, num), _random.Next(-num, num));
		double val = _earthquakeFade * 1.0;
		GameContext.Output[0] = new GamePadOutputState
		{
			LeftVibration = Math.Min(val, 0.5 + _random.NextDouble() * 0.5),
			RightVibration = Math.Min(val, 0.5 + _random.NextDouble() * 0.5)
		};
	}
}
