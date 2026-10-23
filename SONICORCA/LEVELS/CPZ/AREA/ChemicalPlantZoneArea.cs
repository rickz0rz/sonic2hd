using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SONICORCA.LEVELS.CPZ.AREA;

/// <summary>
/// The area for Chemical Plant Zone. It sets the act bounds and music. In act 2 it controls the level of the
/// mega mack (the water) and runs the Water Eggman boss fight.
/// </summary>
public class ChemicalPlantZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/WATEREGGMAN";
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
	{
		"SONICORCA/OBJECTS/LOCKY",
		"SONICORCA/OBJECTS/POCKY"
	};
	private const string Act1MusicResourceKey = "SONICORCA/MUSIC/LEVELS/CPZ/ACT1";
	private const string Act2MusicResourceKey = "SONICORCA/MUSIC/LEVELS/CPZ/ACT2";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	private const string Act1FastMusicResourceKey = "SONICORCA/MUSIC/LEVELS/CPZ/ACT1/FAST";

	// AnimalResourceKeys must stay above AreaDependencies, because static fields are set in the order of the source.
	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		Act1MusicResourceKey,
		Act2MusicResourceKey,
		BossMusicResourceKey,
		Act1FastMusicResourceKey,
		BossObjectResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	/// <summary>
	/// The bottom of the act 1 bounds, and the top of the act 2 bounds.
	/// </summary>
	private const int Act1BoundsBottom = 8192;
	private const int Act2BoundsTop = 3072;

	/// <summary>
	/// The top of the water when it is low and when it is raised.
	/// </summary>
	private const int LowWaterTop = 10304;
	private const int RaisedWaterTop = 8256;

	/// <summary>
	/// The left edge of the camera at which the water rises.
	/// </summary>
	private const int WaterRiseCameraLeft = 71936;

	/// <summary>
	/// Half of the screen width. The left edge of the camera is this distance to the left of the start position.
	/// </summary>
	private const int HalfScreenWidth = 960;

	/// <summary>
	/// The speed at which the water moves to its target level.
	/// </summary>
	private const int WaterSpeed = 4;

	/// <summary>
	/// The number of ticks between the boss arena lock and the boss spawn.
	/// </summary>
	private const int BossSpawnDelay = 90;

	/// <summary>
	/// The speed at which the bounds open to the right after the boss flees.
	/// </summary>
	private const double BoundsOpenSpeed = 8.0;

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private int _state;
	private int _stateTimer;
	private bool _bossDefeated;
	private BossObject _bossObject;
	private bool _waterRaised;
	private int _waterHeight;
	private int _waterHeightOscillation;

	public ChemicalPlantZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Chemical Plant";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.AnimalResourceKeys = AnimalResourceKeys.Select(x => this.GetAbsolutePath(x)).ToArray();
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		if (settings.Act == 1)
			PrepareAct1();
		else if (settings.Act == 2)
		{
			if (settings.Seamless)
				SeamlessPrepareAct2();
			else
				PrepareAct2();
		}
		settings.StartPath = 1;
	}

	private void PrepareAct1()
	{
		_level.LevelMusic = Act1MusicResourceKey;
		_level.CurrentAct = 1;
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = GetAct1Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		_level.SoundManager.SetJingle(JingleType.SpeedShoes, Act1FastMusicResourceKey);
	}

	private void PrepareAct2()
	{
		_level.LevelMusic = Act2MusicResourceKey;
		_level.CurrentAct = 2;
		_level.SetStartPosition("startpos_stk_2");
		Rectanglei act2Bounds = GetAct2Bounds() with
		{
			Right = _level.GetMarker("boss_right").Position.X
		};
		_level.Bounds = act2Bounds;
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(act2Bounds);
		_state = 0;
		_bossDefeated = false;
		InitialiseWater();
	}

	private void SeamlessPrepareAct2()
	{
		_level.LevelMusic = Act2MusicResourceKey;
		_level.CurrentAct = 2;
		Rectanglei act2Bounds = GetAct2Bounds() with
		{
			Right = _level.GetMarker("boss_right").Position.X
		};
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(act2Bounds);
		ExtendSeamlessLevelBounds(_level, act2Bounds);
		_state = 0;
		_bossDefeated = false;
		InitialiseWater();
	}

	/// <summary>
	/// Adds the water area. If the player starts after the water rise point, the water starts raised.
	/// </summary>
	private void InitialiseWater()
	{
		_waterHeightOscillation = 0;
		Rectanglei mapBounds = _level.Map.Bounds;
		Rectanglei waterArea = Rectanglei.FromLTRB(mapBounds.Left, LowWaterTop, mapBounds.Right, mapBounds.Bottom);
		if (_level.GetPlayerStartPosition().X - HalfScreenWidth >= WaterRiseCameraLeft)
		{
			_waterHeight = RaisedWaterTop;
			_waterRaised = true;
		}
		else
		{
			_waterHeight = LowWaterTop;
			_waterRaised = false;
		}
		// The top is always the raised level here, not _waterHeight. UpdateWater sets the correct top on the next update.
		waterArea.Top = RaisedWaterTop;
		_level.WaterManager.Enabled = true;
		_level.WaterManager.WaterAreas.Clear();
		_level.WaterManager.WaterAreas.Add(waterArea);
	}

	/// <summary>
	/// Raises the water when the camera passes the rise point, and moves the water to its target level.
	/// </summary>
	private void UpdateWater()
	{
		Rectanglei waterArea = _level.WaterManager.WaterAreas[0];
		if (!_waterRaised && _level.Camera.Bounds.Left >= WaterRiseCameraLeft)
		{
			_waterRaised = true;
			_waterHeight = waterArea.Top;
			_waterHeightOscillation = 0;
		}
		int targetHeight = _waterRaised ? RaisedWaterTop : LowWaterTop;
		int waterTop;
		if (_waterHeight == targetHeight)
		{
			// The water oscillation is turned off: the offset is always 0. The original code calculated
			// (1 - cos(2 * pi * _waterHeightOscillation / 252)) / 2, but did not use the result.
			++_waterHeightOscillation;
			waterTop = _waterHeight + 0;
		}
		else
		{
			_waterHeight = MathX.GoTowards(_waterHeight, targetHeight, WaterSpeed);
			waterTop = _waterHeight;
		}
		waterArea.Top = waterTop;
		_level.WaterManager.WaterAreas[0] = waterArea;
		_level.WaterManager.HueTarget = 0.3;
		_level.WaterManager.HueAmount = 0.8;
	}

	public override void OnUpdate()
	{
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Updating) || _level.CurrentAct != 2)
			return;
		UpdateWater();
		Rectangle levelBounds = (Rectangle)_level.Bounds;
		Rectanglei cameraBounds = (Rectanglei)_level.Camera.Bounds;
		switch (_state)
		{
			case 0:
				// Wait until the camera passes the one-way marker, then move the bounds to the boss arena.
				int oneWayX = _level.GetMarker("boss_oneway").Position.X;
				int arenaBottom = _level.GetMarker("boss_bottom").Position.Y;
				if (cameraBounds.Left < oneWayX)
					break;
				_level.ScrollBoundsTo((Rectanglei)levelBounds with
				{
					Left = oneWayX,
					Bottom = arenaBottom
				}, 2);
				++_state;
				break;
			case 1:
				// Lock the boss arena and start the boss music.
				int arenaLeft = _level.GetMarker("boss_left").Position.X;
				if (_level.IsScrollingBounds || cameraBounds.Left < arenaLeft)
					break;
				_level.Bounds = GetBossBounds();
				_level.SoundManager.CrossFadeMusic(this.GetAbsolutePath(BossMusicResourceKey));
				_stateTimer = BossSpawnDelay;
				++_state;
				break;
			case 2:
				--_stateTimer;
				if (_stateTimer > 0)
					break;
				SpawnBoss();
				++_state;
				break;
			case 3:
				// The boss fight. When the boss is defeated, play the level music again.
				if (!_bossDefeated && _bossObject.Defeated)
				{
					_bossDefeated = true;
					_level.SoundManager.PlayMusic(this.GetAbsolutePath(Act2MusicResourceKey));
				}
				if (!_bossObject.Fleeing)
					break;
				++_state;
				break;
			case 4:
				// Open the bounds to the right until the stage is complete.
				Rectangle openingBounds = (Rectangle)_level.Bounds;
				openingBounds.Right = Math.Min(openingBounds.Right + BoundsOpenSpeed, GetAct2Bounds().Right);
				openingBounds.Left = Math.Max(openingBounds.Left, _level.Camera.Bounds.Left);
				_level.Bounds = (Rectanglei)openingBounds;
				if (!_level.StateFlags.HasFlag(LevelStateFlags.StageCompleted))
					break;
				_level.FadeOut(LevelState.StageCompleted);
				++_state;
				break;
		}
	}

	private void SpawnBoss()
	{
		LevelMarker positionMarker = _level.GetMarker("boss_position");
		LevelMarker leftFillMarker = _level.GetMarker("boss_fill_left");
		LevelMarker rightFillMarker = _level.GetMarker("boss_fill_right");
		// The property names of the state object must match the state variables of the boss.
		var state = new { LeftFillX = leftFillMarker.Position.X, RightFillX = rightFillMarker.Position.X };
		_bossObject = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath(BossObjectResourceKey), _level.Map.Layers.IndexOf(positionMarker.Layer), positionMarker.Position, state)) as BossObject;
	}

	private Rectanglei GetAct1Bounds()
	{
		return Rectanglei.FromLTRB(0, 0, _level.GetMarker("bounds_1_r").Position.X, Act1BoundsBottom);
	}

	private Rectanglei GetAct2Bounds()
	{
		int left = _level.GetMarker("bounds_2_l").Position.X;
		int right = _level.Map.Bounds.Width;
		int bottom = _level.Map.Bounds.Height;
		return Rectanglei.FromLTRB(left, Act2BoundsTop, right, bottom);
	}

	private Rectanglei GetBossBounds()
	{
		return Rectanglei.FromLTRB(_level.GetMarker("boss_left").Position.X, _level.Bounds.Top, _level.GetMarker("boss_right").Position.X, _level.GetMarker("boss_bottom").Position.Y);
	}
}
