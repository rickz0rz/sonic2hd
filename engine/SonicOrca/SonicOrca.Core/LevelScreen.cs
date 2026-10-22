#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Core.Network;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Menu;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class LevelScreen : Screen
{
	private enum LevelScreenState
	{
		Initialise,
		Loading,
		ReadyToQuit,
		ReadyToPlay,
		Playing,
		EstablishingNetworkPlay,
		NetworkWaitingForStart,
		FadingOut,
		Dead
	}

	public const int DefaultPort = 7237;

	private const string FontResourceKey = "SONICORCA/FONTS/HUD";

	private readonly SonicOrcaGameContext _gameContext;

	private readonly ResourceSession _resourceSession;

	private Player _player;

	private Area _area;

	private Level _level;

	private LevelScreenState _state;

	private LevelScreenState _nextState;

	private int _currentAct = 1;

	private bool _seamless;

	private string _areaResourceKey = "SONICORCA/S2/LEVELS/EHZ/AREA";

	private Font _font;

	private Task _loadingAreaTask;

	private Task _preparingLevelTask;

	private bool _networkPlay;

	private bool _hosting;

	private NetworkGameServer _server;

	private NetworkGameClient _client;

	private Task _joiningServer;

	private int _lastNotificationTickCount;

	private Vector2 _inputDirection;

	private bool _inputAction;

	public Vector2i? OverrideStartPosition { get; set; }

	public Area Area => _area;

	public Player Player => _player;

	public Level Level => _level;

	public LevelScreen(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
		_level = new Level(_gameContext);
		_currentAct = int.Parse(_gameContext.Configuration.GetProperty("debug", "startact", "1"));
		string property = _gameContext.Configuration.GetProperty("debug", "startpos");
		if (property != null)
		{
			string[] array = property.Split(',');
			OverrideStartPosition = new Vector2i(int.Parse(array[0]), int.Parse(array[1]));
		}
		int num = int.Parse(_gameContext.Configuration.GetProperty("debug", "zone", "1"));
		if (num == 2)
		{
			_areaResourceKey = "SONICORCA/S2/LEVELS/CPZ/AREA";
		}
		switch (num)
		{
		case 3:
			_areaResourceKey = "SONICORCA/S2/LEVELS/ARZ/AREA";
			break;
		case 5:
			_areaResourceKey = "SONICORCA/S2/LEVELS/HTZ/AREA";
			break;
		}
	}

	public LevelScreen(SonicOrcaGameContext gameContext, string areaResourceKey, int act)
	{
		_gameContext = gameContext;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
		_level = new Level(_gameContext);
		_areaResourceKey = areaResourceKey;
		_currentAct = act;
	}

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default(CancellationToken))
	{
		_resourceSession.PushDependency("SONICORCA/FONTS/HUD");
		await _resourceSession.LoadAsync(ct);
		_font = _gameContext.ResourceTree.GetLoadedResource<Font>("SONICORCA/FONTS/HUD");
	}

	public override void Unload()
	{
		if (_level != null)
		{
			_level.Dispose();
		}
		_resourceSession.Unload();
	}

	public override void Update()
	{
		switch (_state)
		{
		case LevelScreenState.Initialise:
			if (NetworkSetupCheck())
			{
				_state = LevelScreenState.EstablishingNetworkPlay;
			}
			else
			{
				_state = LevelScreenState.Loading;
			}
			break;
		case LevelScreenState.EstablishingNetworkPlay:
			EstablishNetworkUpdate();
			break;
		case LevelScreenState.Loading:
			LoadingUpdate();
			break;
		case LevelScreenState.ReadyToQuit:
			UnloadLevel();
			Finish();
			break;
		case LevelScreenState.NetworkWaitingForStart:
			NetworkWaitingUpdate();
			break;
		case LevelScreenState.ReadyToPlay:
		case LevelScreenState.Playing:
			PlayingUpdate();
			break;
		case LevelScreenState.FadingOut:
			PlayingUpdate();
			if (!_level.StateFlags.HasFlag(LevelStateFlags.FadingOut))
			{
				_state = _nextState;
			}
			break;
		case LevelScreenState.Dead:
			if (_level.StateFlags.HasFlag(LevelStateFlags.TimeUp))
			{
				_player.StarpostTime = 0;
			}
			_level.Time = _player.StarpostTime;
			UnloadLevel();
			_player.Lives--;
			if (_player.Lives <= 0)
			{
				Finish();
			}
			else
			{
				_state = LevelScreenState.Loading;
			}
			break;
		}
	}

	public override void Draw(Renderer renderer)
	{
		switch (_state)
		{
		case LevelScreenState.EstablishingNetworkPlay:
			EstablishNetworkDraw(renderer);
			break;
		case LevelScreenState.Loading:
			if (_seamless)
			{
				PlayingDraw(renderer);
			}
			else
			{
				LoadingDraw(renderer);
			}
			break;
		case LevelScreenState.NetworkWaitingForStart:
			NetworkWaitingDraw(renderer);
			break;
		case LevelScreenState.ReadyToPlay:
			if (_seamless)
			{
				PlayingDraw(renderer);
			}
			break;
		case LevelScreenState.Playing:
		case LevelScreenState.FadingOut:
			PlayingDraw(renderer);
			break;
		case LevelScreenState.ReadyToQuit:
			break;
		}
	}

	private async Task LoadArea(CancellationToken ct = default(CancellationToken))
	{
		_resourceSession.PushDependency(_areaResourceKey);
		await _resourceSession.LoadAsync(ct);
		_area = _gameContext.ResourceTree.GetLoadedResource<Area>(_areaResourceKey);
		_player = _level.Player;
		await _level.LoadCommonAsync(ct);
	}

	private Task PrepareLevel(bool seamless)
	{
		Trace.WriteLine(string.Join(" ", "Preparing", seamless ? "seamless" : string.Empty, "level"));
		LevelPrepareSettings settings = new LevelPrepareSettings
		{
			Act = _currentAct,
			Seamless = seamless
		};
		_area.Prepare(_level, settings);
		if (OverrideStartPosition.HasValue)
		{
			_level.StartPosition = OverrideStartPosition.Value;
			OverrideStartPosition = null;
		}
		return _level.LoadAsync(_area);
	}

	private void LoadingUpdate()
	{
		if (_area == null)
		{
			if (_loadingAreaTask == null)
			{
				_loadingAreaTask = LoadArea();
			}
			return;
		}
		_loadingAreaTask = null;
		if (_preparingLevelTask == null)
		{
			_preparingLevelTask = PrepareLevel(_seamless);
		}
		else if (_preparingLevelTask.IsCompleted)
		{
			_preparingLevelTask = null;
			_state = (_networkPlay ? LevelScreenState.NetworkWaitingForStart : LevelScreenState.ReadyToPlay);
		}
	}

	private void LoadingDraw(Renderer renderer)
	{
		_ = _area;
		renderer.GetFontRenderer();
	}

	private void UnloadLevel()
	{
		_level.Unload();
	}

	private void PlayingUpdate()
	{
		if (_state == LevelScreenState.ReadyToPlay)
		{
			if (!_seamless)
			{
				_level.Start();
			}
			else
			{
				_seamless = false;
				_level.Start();
			}
			_state = LevelScreenState.Playing;
		}
		if (_state == LevelScreenState.Playing)
		{
			switch (_level.State)
			{
			case LevelState.Dead:
				_level.FadeOut(LevelState.Dead);
				_state = LevelScreenState.FadingOut;
				_nextState = LevelScreenState.Dead;
				break;
			case LevelState.StageCompleted:
				if (_level.CurrentAct == 1)
				{
					_currentAct = 2;
					_level.Time = 0;
					_player.StarpostIndex = -1;
					_seamless = true;
					_state = LevelScreenState.Loading;
				}
				else
				{
					_level.FadeOut(LevelState.StageCompleted);
					_state = LevelScreenState.FadingOut;
					_nextState = LevelScreenState.ReadyToQuit;
				}
				break;
			}
		}
		if (_networkPlay)
		{
			NetworkPlayUpdate();
		}
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Editing))
		{
			_area.OnUpdate();
		}
		_level.Update();
		_level.Animate();
	}

	private void PlayingDraw(Renderer renderer)
	{
		_level.Draw(renderer);
	}

	private void EstablishNetworkUpdate()
	{
		if (_hosting)
		{
			if (_server.NetworkPlayers.Count > 0)
			{
				_server.AllowClientsToConnect = false;
				_state = LevelScreenState.Loading;
			}
		}
		else if (_joiningServer.IsCompleted)
		{
			if (_joiningServer.IsFaulted)
			{
				throw _joiningServer.Exception.InnerException;
			}
			if (_client.Connected)
			{
				_state = LevelScreenState.Loading;
			}
		}
	}

	private void EstablishNetworkDraw(Renderer renderer)
	{
		string text = (_hosting ? "WAITING FOR CLIENT TO CONNECT..." : "CONNECTING TO SERVER...");
		renderer.GetFontRenderer().RenderStringWithShadow(text, new Rectangle(8.0, 8.0, 0.0, 0.0), FontAlignment.Left, _font, 0);
	}

	private void NetworkWaitingUpdate()
	{
		if (_hosting)
		{
			_server.Update();
			if (_server.NetworkPlayers.All((NetworkPlayer x) => x.Ready))
			{
				_server.SendPacketToAllClients(new NotifyPacket(PacketType.ReadyToStartLevel));
				_state = LevelScreenState.ReadyToPlay;
			}
			return;
		}
		_client.Update();
		if (_client.Ready)
		{
			_state = LevelScreenState.ReadyToPlay;
		}
		else if (Environment.TickCount > _lastNotificationTickCount + 3000)
		{
			_client.SendPacket(new NotifyPacket(PacketType.ReadyToStartLevel));
			_lastNotificationTickCount = Environment.TickCount;
		}
	}

	private void NetworkWaitingDraw(Renderer renderer)
	{
		renderer.GetFontRenderer().RenderStringWithShadow("WAITING FOR OTHER PLAYERS TO LOAD GAME...", new Rectangle(8.0, 8.0, 0.0, 0.0), FontAlignment.Left, _font, 0);
	}

	private bool NetworkSetupCheck()
	{
		IReadOnlyList<string> commandLineArgs = Environment.GetCommandLineArgs();
		if (commandLineArgs.Count > 1)
		{
			string text = commandLineArgs[0].ToLower();
			if (text == "host")
			{
				_server = new NetworkGameServer(_level, 7237);
				_networkPlay = true;
				_hosting = true;
				return true;
			}
			if (text == "join")
			{
				_client = new NetworkGameClient(_level);
				_joiningServer = _client.InitiateHandshake(commandLineArgs[2], 7237);
				_networkPlay = true;
				return true;
			}
		}
		return false;
	}

	private void NetworkPlayUpdate()
	{
		if (_hosting)
		{
			_server.Update();
			int characterId = 1;
			if (!(_inputDirection == _gameContext.Current[0].DirectionLeft) || _inputAction != _gameContext.Current[0].Action1)
			{
				_inputDirection = _gameContext.Current[0].DirectionLeft;
				_inputAction = _gameContext.Current[0].Action1;
				PlayInputPacket packet = new PlayInputPacket(characterId, _inputDirection, _inputAction);
				_server.SendPacketToAllClients(packet);
			}
		}
		else
		{
			_client.Update();
		}
	}
}
