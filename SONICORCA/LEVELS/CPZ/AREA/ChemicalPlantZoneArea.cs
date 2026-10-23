// Decompiled with JetBrains decompiler
// Type: SONICORCA.LEVELS.CPZ.AREA.ChemicalPlantZoneArea
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.LEVELS.CPZ.AREA;

public class ChemicalPlantZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/WATEREGGMAN";
	private static IReadOnlyList<string> AnimalResourceKeys = new string[2]
	{
	"SONICORCA/OBJECTS/LOCKY",
	"SONICORCA/OBJECTS/POCKY"
	};
	private const string Act1MusicResourceKey = "SONICORCA/MUSIC/LEVELS/CPZ/ACT1";
	private const string Act2MusicResourceKey = "SONICORCA/MUSIC/LEVELS/CPZ/ACT2";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	private const string Act1FastMusicResourceKey = "SONICORCA/MUSIC/LEVELS/CPZ/ACT1/FAST";
	public static IReadOnlyList<string> AreaDependencies = (new string[8]
	{
	"//TILESET",
	"//MAP",
	"//BINDING",
	"SONICORCA/MUSIC/LEVELS/CPZ/ACT1",
	"SONICORCA/MUSIC/LEVELS/CPZ/ACT2",
	"SONICORCA/MUSIC/BOSS/S2",
	"SONICORCA/MUSIC/LEVELS/CPZ/ACT1/FAST",
	"SONICORCA/OBJECTS/WATEREGGMAN"
	}).Concat<string>(AnimalResourceKeys).ToArray<string>();
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
		_level.AnimalResourceKeys = ChemicalPlantZoneArea.AnimalResourceKeys.Select<string, string>(x => this.GetAbsolutePath(x)).ToArray<string>();
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, "//TILESET");
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, "//MAP"));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, "//BINDING"));
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
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/CPZ/ACT1";
		_level.CurrentAct = 1;
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = GetAct1Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		_level.SoundManager.SetJingle(JingleType.SpeedShoes, "SONICORCA/MUSIC/LEVELS/CPZ/ACT1/FAST");
	}

	private void PrepareAct2()
	{
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/CPZ/ACT2";
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
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/CPZ/ACT2";
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

	private void InitialiseWater()
	{
		_waterHeightOscillation = 0;
		Rectanglei bounds = _level.Map.Bounds;
		Rectanglei rectanglei = Rectanglei.FromLTRB(bounds.Left, 10304, bounds.Right, bounds.Bottom);
		if (_level.GetPlayerStartPosition().X - 960 >= 71936)
		{
			_waterHeight = 8256;
			_waterRaised = true;
		}
		else
		{
			_waterHeight = 10304;
			_waterRaised = false;
		}
		rectanglei.Top = 8256;
		_level.WaterManager.Enabled = true;
		_level.WaterManager.WaterAreas.Clear();
		_level.WaterManager.WaterAreas.Add(rectanglei);
	}

	private void UpdateWater()
	{
		Rectanglei waterArea = _level.WaterManager.WaterAreas[0];
		int top = waterArea.Top;
		if (!_waterRaised && _level.Camera.Bounds.Left >= 71936.0)
		{
			_waterRaised = true;
			_waterHeight = top;
			_waterHeightOscillation = 0;
		}
		int dest = _waterRaised ? 8256 : 10304;
		int num1;
		if (_waterHeight == dest)
		{
			++_waterHeightOscillation;
			double num2 = _waterHeightOscillation / 252.0;
			int waterHeight = _waterHeight;
			double num3 = (1.0 - Math.Cos(num2 * (2.0 * Math.PI))) / 2.0;
			num1 = waterHeight + 0;
		}
		else
		{
			_waterHeight = MathX.GoTowards(_waterHeight, dest, 4);
			num1 = _waterHeight;
		}
		waterArea.Top = num1;
		_level.WaterManager.WaterAreas[0] = waterArea;
		_level.WaterManager.HueTarget = 0.3;
		_level.WaterManager.HueAmount = 0.8;
	}

	public override void OnUpdate()
	{
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Updating) || _level.CurrentAct != 2)
			return;
		UpdateWater();
		Rectangle bounds1 = (Rectangle)_level.Bounds;
		Rectanglei bounds2 = (Rectanglei)_level.Camera.Bounds;
		switch (_state)
		{
			case 0:
				int x1 = _level.GetMarker("boss_oneway").Position.X;
				int y = _level.GetMarker("boss_bottom").Position.Y;
				if (bounds2.Left < x1)
					break;
				_level.ScrollBoundsTo((Rectanglei)bounds1 with
				{
					Left = x1,
					Bottom = y
				}, 2);
				++_state;
				break;
			case 1:
				int x2 = _level.GetMarker("boss_left").Position.X;
				if (_level.IsScrollingBounds || bounds2.Left < x2)
					break;
				_level.Bounds = GetBossBounds();
				_level.SoundManager.CrossFadeMusic(this.GetAbsolutePath("SONICORCA/MUSIC/BOSS/S2"));
				_stateTimer = 90;
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
				if (!_bossDefeated && _bossObject.Defeated)
				{
					_bossDefeated = true;
					_level.SoundManager.PlayMusic(this.GetAbsolutePath("SONICORCA/MUSIC/LEVELS/CPZ/ACT2"));
				}
				if (!_bossObject.Fleeing)
					break;
				++_state;
				break;
			case 4:
				Rectangle bounds3 = (Rectangle)_level.Bounds;
				bounds3.Right = Math.Min(bounds3.Right + 8.0, GetAct2Bounds().Right);
				bounds3.Left = Math.Max(bounds3.Left, _level.Camera.Bounds.Left);
				_level.Bounds = (Rectanglei)bounds3;
				if (!_level.StateFlags.HasFlag(LevelStateFlags.StageCompleted))
					break;
				_level.FadeOut(LevelState.StageCompleted);
				++_state;
				break;
		}
	}

	private void SpawnBoss()
	{
		LevelMarker marker1 = _level.GetMarker("boss_position");
		LevelMarker marker2 = _level.GetMarker("boss_fill_left");
		LevelMarker marker3 = _level.GetMarker("boss_fill_right");
		Vector2i position = marker2.Position;
		int x1 = position.X;
		position = marker3.Position;
		int x2 = position.X;
		var state = new { LeftFillX = x1, RightFillX = x2 };
		_bossObject = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath("SONICORCA/OBJECTS/WATEREGGMAN"), _level.Map.Layers.IndexOf(marker1.Layer), marker1.Position, state)) as BossObject;
	}

	private Rectanglei GetAct1Bounds()
	{
		return Rectanglei.FromLTRB(0, 0, _level.GetMarker("bounds_1_r").Position.X, 8192 /*0x2000*/);
	}

	private Rectanglei GetAct2Bounds()
	{
		int x = _level.GetMarker("bounds_2_l").Position.X;
		Rectanglei bounds = _level.Map.Bounds;
		int width = bounds.Width;
		bounds = _level.Map.Bounds;
		int height = bounds.Height;
		return Rectanglei.FromLTRB(x, 3072 /*0x0C00*/, width, height);
	}

	private Rectanglei GetBossBounds()
	{
		return Rectanglei.FromLTRB(_level.GetMarker("boss_left").Position.X, _level.Bounds.Top, _level.GetMarker("boss_right").Position.X, _level.GetMarker("boss_bottom").Position.Y);
	}
}
