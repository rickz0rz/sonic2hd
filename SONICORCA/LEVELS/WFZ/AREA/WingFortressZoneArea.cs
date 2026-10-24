using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;
using SONICORCA.OBJECTS.TORNADO;
using SONICORCA.OBJECTS.WFZTHRUST;

namespace SONICORCA.LEVELS.WFZ.AREA;

/// <summary>
/// The area for Wing Fortress Zone. It loads the tile set, the map and the binding, and sets the level music. A map
/// from "s2hd-data import-level wfz" adds the collision, the objects, and the markers for the start position, the act
/// bounds and the act origin. With these markers, the area runs the level events of the original (LevEvents_WFZ and
/// the camera changes of ObjC5): the boss, the locked controls after the boss, and the background that moves with
/// Eggman's getaway ship. The Tornado (ObjB2, subtype $54) then takes the player to the ship, and the act ends when the
/// original goes to Death Egg Zone (<see cref="WFZEnding"/>). Without the markers, the level bounds are the bounds of
/// the map.
/// </summary>
public class WingFortressZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/WFZ";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	/// <summary>Only a mod from "s2hd-data import-level wfz" has the boss.</summary>
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/WFZBOSS";

	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		LevelMusicResourceKey,
		BossMusicResourceKey,
		BossObjectResourceKey
	}.ToArray();

	private const int Scale = 4;

	private const int OriginalScreenWidth = 320;

	private const int OriginalScreenHeight = 224;

	/// <summary>The speed at which the level bounds move to the camera limits of the boss, in level units each tick.</summary>
	private const int BoundsScrollSpeed = 8;

	/// <summary>LevEvents_WFZ_Routine5: the boss events start when the camera passes this position.</summary>
	private static readonly Vector2i BossTrigger = new Vector2i(0x2880, 0x400);

	/// <summary>The camera cannot go back to the left of this x (Camera_Min_X_pos) from the boss events.</summary>
	private const int BossMinCameraX = 0x2880;

	/// <summary>The position of the laser case in the object layout of the original (ObjC5, subtype $92).</summary>
	private static readonly Vector2i BossPosition = new Vector2i(0x2B80, 0x450);

	/// <summary>ObjC5_CaseBoundary: the camera cannot go below this y while the boss is there.</summary>
	private const int BossMaxCameraY = 0x442;

	/// <summary>ObjC5_CaseWaitStart: the case starts when the player comes within $20 pixels of it.</summary>
	private const int BossStartDistance = 0x20;

	/// <summary>ObjC5_CaseStart: the boss music starts this number of frames after the player comes to the case.</summary>
	private const int BossMusicDelay = 0x5A;

	/// <summary>LevEvents_WFZ_Routine6: the end of the act starts when the camera goes below this y.</summary>
	private const int EndCameraY = 0x500;

	/// <summary>ObjB2_Wait_Leader_position: the Tornado takes the player at this y or lower.</summary>
	private const int EndPlayerY = 0x5EC;

	/// <summary>
	/// If the Tornado does not take the player this number of frames after the player comes to y $5EC (the Tornado
	/// waits $40 frames), the act ends without it. The same occurs when the controls are locked for this number of
	/// frames and the player does not come to y $5EC.
	/// </summary>
	private const int EndFallbackTicks = 0x80;

	private const int EndLockFallbackTicks = 0x400;

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private bool _events;
	private Vector2i _origin;
	private Rectanglei _actBounds;
	private EventState _state;
	private BossObject _boss;
	private bool _bossStarted;
	private int _musicTimer;
	private bool _levelMusicRestarted;
	private int _endTimer;
	private int _lockTimer;
	private bool _finished;
	private LevelLayer _background;
	private BackgroundScroll _backgroundScroll;

	/// <summary>The background layer of a map from "s2hd-data import-level", and the size of its tiles.</summary>
	private const string BackgroundLayerName = "back";

	private const int BackgroundTileSize = 64;

	/// <summary>The scroll values of the background layer in the map, before the area changes them.</summary>
	private static readonly ConditionalWeakTable<LevelLayer, BackgroundScroll> BackgroundScrolls = new ConditionalWeakTable<LevelLayer, BackgroundScroll>();

	private sealed class BackgroundScroll
	{
		public double[] RowOffsets { get; init; }

		public int OffsetY { get; init; }

		public bool WrapY { get; init; }
	}

	private enum EventState
	{
		WaitBoss,
		Boss,
		WaitEnd,
		End,
		Done
	}

	public WingFortressZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Wing Fortress";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.LevelMusic = LevelMusicResourceKey;
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			LevelMap map = _resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey);
			// The map is shared: put back the layout changes of the last play
			LayoutChanges.Reset(map);
			_level.LoadMap(map);
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		// The zone has one act. Act 0 shows no act number, and a completed stage ends the zone: act 1 would load an
		// act 2.
		_level.CurrentAct = 0;
		_events = false;
		_state = EventState.WaitBoss;
		_boss = null;
		_bossStarted = false;
		_levelMusicRestarted = false;
		_endTimer = 0;
		_lockTimer = 0;
		_finished = false;
		WFZEnding.For(_level).Reset();
		if (_level.TryGetMarker("bounds_1_lt", out LevelMarker leftTop) && _level.TryGetMarker("bounds_1_rb", out LevelMarker rightBottom))
		{
			// A map from "s2hd-data import-level wfz": the start and the bounds of the act
			_level.SetStartPosition("startpos_stk_1");
			_actBounds = Rectanglei.FromLTRB(leftTop.Position.X, leftTop.Position.Y, rightBottom.Position.X, rightBottom.Position.Y);
			// The blue sky: the backdrop colour of the original palette (line 2, colour 0)
			_level.BackdropColour = new Colour(255, 72, 72, 255);
			// Tails flies the Tornado, so he is not a sidekick here
			if (settings.ProtagonistCharacter == CharacterType.Null)
			{
				settings.ProtagonistCharacter = CharacterType.Sonic;
			}
			if (settings.ProtagonistCharacter != CharacterType.Tails)
			{
				settings.SidekickCharacter = CharacterType.Null;
			}
			_level.Bounds = _actBounds;
			if (_level.TryGetMarker("origin_1", out LevelMarker origin))
			{
				_origin = origin.Position;
				_events = true;
			}
		}
		else
		{
			_level.Bounds = _level.Map.Bounds;
		}
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		settings.StartPath = 0;
		PrepareBackground();
	}

	/// <summary>
	/// Finds the background layer of an imported map, and puts back its scroll values, which <see cref="UpdateBackground"/>
	/// changes. The map can stay loaded when the level starts again.
	/// </summary>
	private void PrepareBackground()
	{
		_background = _events ? _level.Map.Layers.FirstOrDefault(layer => layer.Name == BackgroundLayerName) : null;
		if (_background == null || _background.LayerRowDefinitions.Count == 0)
		{
			_background = null;
			return;
		}
		BackgroundScroll scroll = BackgroundScrolls.GetValue(_background, layer => new BackgroundScroll
		{
			RowOffsets = layer.LayerRowDefinitions.Select(row => row.CurrentOffset).ToArray(),
			OffsetY = layer.OffsetY,
			WrapY = layer.WrapY
		});
		_backgroundScroll = scroll;
		for (int i = 0; i < scroll.RowOffsets.Length && i < _background.LayerRowDefinitions.Count; i++)
		{
			_background.LayerRowDefinitions[i].CurrentOffset = scroll.RowOffsets[i];
		}
		_background.OffsetY = scroll.OffsetY;
		_background.WrapY = scroll.WrapY;
	}

	/// <summary>
	/// The background moves with the offsets of the original (Camera_BG_X_offset and Camera_BG_Y_offset, see
	/// <see cref="WFZShipOffset"/>): to the right and down while Eggman's ship flies off (LevEvents_WFZ_Routine3), and
	/// to the left and down while the player holds on to the ship (LevEvents_WFZ_Routine4). The background of the
	/// original scrolls with the camera, so it wraps at the bottom of the layer, as here.
	/// </summary>
	private void UpdateBackground()
	{
		int offsetX = WFZShipOffset.Get(_level);
		int offsetY = WFZShipOffset.GetY(_level);
		if (_background == null)
		{
			return;
		}
		for (int i = 0; i < _backgroundScroll.RowOffsets.Length && i < _background.LayerRowDefinitions.Count; i++)
		{
			_background.LayerRowDefinitions[i].CurrentOffset = _backgroundScroll.RowOffsets[i] + offsetX * Scale;
		}
		if (offsetY == 0)
		{
			return;
		}
		// The layer moves down. It starts above the screen, so that it fills the screen when it wraps.
		int height = _background.Rows * BackgroundTileSize;
		int y = (_backgroundScroll.OffsetY + offsetY * Scale) % height;
		_background.WrapY = true;
		_background.OffsetY = y > 0 ? y - height : y;
	}

	public override void OnUpdate()
	{
		if (!_events || !_level.StateFlags.HasFlag(LevelStateFlags.Updating) || _finished)
		{
			return;
		}
		UpdateBackground();
		if (WFZEnding.For(_level).Finished)
		{
			// The Tornado can also take a player who skipped the boss
			Finish();
			return;
		}
		switch (_state)
		{
			case EventState.WaitBoss:
				// LevEvents_WFZ_Routine5
				if (CameraX >= BossTrigger.X && CameraY >= BossTrigger.Y)
				{
					StartBossEvents();
				}
				break;
			case EventState.Boss:
				UpdateBoss();
				break;
			case EventState.WaitEnd:
				// LevEvents_WFZ_Routine6
				if (CameraY >= EndCameraY)
				{
					_state = EventState.End;
					LockControls();
				}
				break;
			case EventState.End:
				UpdateEnd();
				break;
		}
	}

	/// <summary>
	/// The camera cannot go back to the left, and the laser case comes. The original loads its art here and has the
	/// case in the object layout. The case sets the lowest camera y at once.
	/// </summary>
	private void StartBossEvents()
	{
		_state = EventState.Boss;
		Rectanglei bounds = _level.Bounds;
		int left = Math.Max(bounds.Left, Math.Min((int)_level.Camera.Bounds.Left, CentreToLeft(BossMinCameraX)));
		int bottom = CentreToBottom(BossMaxCameraY);
		_level.ScrollBoundsTo(Rectanglei.FromLTRB(left, bounds.Top, bounds.Right, bottom), BoundsScrollSpeed);
		if (_resourceTree[this.GetAbsolutePath(BossObjectResourceKey)]?.Resource == null)
		{
			return;
		}
		// The property names of the state object must match the state variables of the boss
		var state = new { OriginX = _origin.X, OriginY = _origin.Y };
		int layer = Math.Max(_level.Map.Layers.IndexOf(_level.Player.Protagonist.Layer), 0);
		Vector2i position = _origin + BossPosition * Scale;
		_boss = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath(BossObjectResourceKey), layer, position, state)) as BossObject;
	}

	/// <summary>
	/// The music of the boss (ObjC5_CaseStart and ObjC5_CaseSpeedDown), and the end of the boss (ObjC5_End): the level
	/// music plays again and the camera can go down to y $720.
	/// </summary>
	private void UpdateBoss()
	{
		if (_boss == null)
		{
			// Without the boss, the camera can go down at once
			RestoreBottom();
			_state = EventState.WaitEnd;
			return;
		}
		ICharacter player = _level.Player.Protagonist;
		if (!_bossStarted && player != null && Math.Abs((player.PositionPrecise.X - _origin.X) / Scale - BossPosition.X) < BossStartDistance)
		{
			// The case starts when the player comes within $20 pixels of it (ObjC5_CaseWaitStart)
			_bossStarted = true;
			_musicTimer = BossMusicDelay;
			_level.SoundManager.FadeOutMusic(60);
		}
		if (_bossStarted && _musicTimer > 0 && --_musicTimer == 0 && !_boss.Defeated)
		{
			PlayMusic(BossMusicResourceKey);
		}
		if (_boss.Defeated && !_levelMusicRestarted)
		{
			_levelMusicRestarted = true;
			PlayMusic(LevelMusicResourceKey);
			RestoreBottom();
			_state = EventState.WaitEnd;
		}
	}

	/// <summary>
	/// LevEvents_WFZ_Routine6: the controls are locked (Control_Locked) until the end of the act. The player falls to
	/// the bottom of the shaft, and the Tornado (ObjB2, subtype $54) takes the player after Eggman's ship.
	/// </summary>
	private void LockControls()
	{
		_level.StateFlags &= ~LevelStateFlags.AllowCharacterControl;
		ICharacter player = _level.Player.Protagonist;
		if (player != null)
		{
			player.Input = new CharacterInputState();
		}
	}

	/// <summary>
	/// Without the Tornado (for example in a map without it), the act ends when the player is at the bottom of the
	/// shaft and the Tornado does not come, or when the player does not come to the bottom of the shaft.
	/// </summary>
	private void UpdateEnd()
	{
		if (WFZEnding.For(_level).Started)
		{
			return;
		}
		ICharacter player = _level.Player.Protagonist;
		if (++_lockTimer >= EndLockFallbackTicks)
		{
			Finish();
			return;
		}
		if (player == null || (player.PositionPrecise.Y - _origin.Y) / Scale < EndPlayerY)
		{
			return;
		}
		if (++_endTimer >= EndFallbackTicks)
		{
			Finish();
		}
	}

	/// <summary>ObjB2_Start_DEZ: the original goes to Death Egg Zone at once, without a results screen.</summary>
	private void Finish()
	{
		if (_finished)
		{
			return;
		}
		_finished = true;
		_state = EventState.Done;
		_level.StateFlags &= ~LevelStateFlags.UpdateTime;
		_level.StateFlags |= LevelStateFlags.StageCompleted;
		_level.FadeOut(LevelState.StageCompleted);
	}

	private void RestoreBottom()
	{
		Rectanglei bounds = _level.Bounds;
		_level.ScrollBoundsTo(Rectanglei.FromLTRB(bounds.Left, bounds.Top, bounds.Right, _actBounds.Bottom), BoundsScrollSpeed);
	}

	private void PlayMusic(string key)
	{
		string path = this.GetAbsolutePath(key);
		if (_resourceTree[path]?.Resource != null)
		{
			_level.SoundManager.PlayMusic(path);
		}
	}

	/// <summary>The original camera x: the left edge of a 320 pixel screen at the centre of the HD camera.</summary>
	private int CameraX => (int)((_level.Camera.Bounds.X + _level.Camera.Bounds.Width / 2 - _origin.X) / Scale) - OriginalScreenWidth / 2;

	/// <summary>The original camera y: the top edge of a 224 pixel screen at the centre of the HD camera.</summary>
	private int CameraY => (int)((_level.Camera.Bounds.Y + _level.Camera.Bounds.Height / 2 - _origin.Y) / Scale) - OriginalScreenHeight / 2;

	/// <summary>The left of the level bounds for a minimum original camera x, so that the screen centres agree.</summary>
	private int CentreToLeft(int cameraX) => _origin.X + (cameraX + OriginalScreenWidth / 2) * Scale - (int)_level.Camera.Bounds.Width / 2;

	/// <summary>The bottom of the level bounds for a maximum original camera y, so that the screen centres agree.</summary>
	private int CentreToBottom(int cameraY) => _origin.Y + (cameraY + OriginalScreenHeight / 2) * Scale + (int)_level.Camera.Bounds.Height / 2;
}
