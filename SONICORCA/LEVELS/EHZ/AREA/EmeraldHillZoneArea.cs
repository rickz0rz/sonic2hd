// Decompiled with JetBrains decompiler
// Type: SONICORCA.LEVELS.EHZ.AREA.EmeraldHillZoneArea
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.LEVELS.EHZ.AREA;

public class EmeraldHillZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/DRILLEGGMAN";
	private static IReadOnlyList<string> AnimalResourceKeys = new string[2]
	{
	"SONICORCA/OBJECTS/FLICKY",
	"SONICORCA/OBJECTS/RICKY"
	};
	private const string Act1MusicResourceKey = "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1";
	private const string Act2MusicResourceKey = "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT2";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	private const string Act1FastMusicResourceKey = "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1/FAST";
	private const string Act2FastMusicResourceKey = "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT2/FAST";
	private const string WaterfallResourceKey = "SONICORCA/SOUND/WATERFALL";
	public static IReadOnlyList<string> AreaDependencies = (new string[10]
	{
	"//TILESET",
	"//MAP",
	"//BINDING",
	"SONICORCA/OBJECTS/DRILLEGGMAN",
	"SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1",
	"SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT2",
	"SONICORCA/MUSIC/BOSS/S2",
	"SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1/FAST",
	"SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT2/FAST",
	"SONICORCA/SOUND/WATERFALL"
	}).Concat<string>(AnimalResourceKeys).ToArray<string>();
	private SonicOrcaGameContext _gameContext;
	private Level _level;
	private int _state;
	private int _stateTimer;
	private BossObject _bossObject;
	private bool _bossDefeated;
	private int _sunFallTimer;
	private Rectanglei[] _waterfallRects;
	private SampleInstance _waterfallSampleInstance;

	public override IEnumerable<KeyValuePair<string, object>> StateVariables
	{
		get
		{
			return new KeyValuePair<string, object>[2]
			{
		new KeyValuePair<string, object>("STATE",  _state),
		new KeyValuePair<string, object>("BOSS DEFEATED",  _bossDefeated)
			};
		}
	}

	public EmeraldHillZoneArea()
	  : base(AreaDependencies)
	{
	}

	public override void Dispose() => _waterfallSampleInstance?.Dispose();

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_level = level;
		_level.Name = "Emerald Hill";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.AnimalResourceKeys = EmeraldHillZoneArea.AnimalResourceKeys.Select<string, string>(x => this.GetAbsolutePath(x)).ToArray<string>();
		if (!settings.Seamless)
		{
			_level.TileSet = _gameContext.ResourceTree.GetLoadedResource<TileSet>(this, "//TILESET");
			_level.LoadMap(_gameContext.ResourceTree.GetLoadedResource<LevelMap>(this, "//MAP"));
			_level.LoadBinding(_gameContext.ResourceTree.GetLoadedResource<LevelBinding>(this, "//BINDING"));
			PrepareWaterfalls();
		}
		if (settings.Act == 1)
			PrepareAct1();
		else if (settings.Act == 2)
			PrepareAct2(settings.Seamless);
		settings.StartPath = 0;
	}

	private void PrepareAct1()
	{
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1";
		_level.CurrentAct = 1;
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = GetAct1Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		_level.SoundManager.SetJingle(JingleType.SpeedShoes, "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1/FAST");
	}

	private void PrepareAct2(bool seamless)
	{
		_level.SoundManager.SetJingle(JingleType.SpeedShoes, "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT2/FAST");
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT2";
		_level.CurrentAct = 2;
		_level.SetStartPosition("startpos_stk_2");
		Rectanglei act2Bounds = GetAct2Bounds() with
		{
			Right = _level.GetMarker("boss_right").Position.X
		};
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(act2Bounds);
		if (seamless)
			ExtendSeamlessLevelBounds(_level, act2Bounds);
		else
			_level.Bounds = act2Bounds;
		_state = 0;
		_bossDefeated = false;
	}

	private void PrepareWaterfalls()
	{
		_waterfallSampleInstance = new SampleInstance(_gameContext, _gameContext.ResourceTree.GetLoadedResource<SampleInfo>("SONICORCA/SOUND/WATERFALL"));
		_waterfallRects = GetWaterfalls(_level.Map.Layers[10]).AsArray<Rectanglei>();
		_level.Map.Layers[8].WaterfallEffects = _waterfallRects;
	}

	private void UpdateWaterfallSounds()
	{
		Vector2i centre = (Vector2i)_level.Camera.Bounds.Centre;
		Rectanglei rectanglei = new Rectanglei();
		int num1 = int.MaxValue;
		foreach (Rectanglei waterfallRect in _waterfallRects)
		{
			int num2 = centre.X - waterfallRect.Centre.X * 64 /*0x40*/;
			if (Math.Abs(num2) < Math.Abs(num1))
			{
				num1 = num2;
				rectanglei = waterfallRect;
			}
		}
		int num3 = Math.Min(Math.Abs(centre.Y - rectanglei.Top * 64 /*0x40*/), Math.Abs(centre.Y - rectanglei.Bottom * 64 /*0x40*/));
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Paused) && !_level.Player.Protagonist.IsDead && Math.Abs(num1) < 1000 && num3 < 1400)
		{
			_waterfallSampleInstance.Volume = 0.75 * Math.Min(1.0 - Math.Abs(num1) / 1000.0, 1.0 - num3 / 1400.0);
			_waterfallSampleInstance.Pan = num1 / 1000.0;
			_waterfallSampleInstance.Play();
		}
		else
			_waterfallSampleInstance.Stop();
	}

	public override void OnPause() => _waterfallSampleInstance?.Stop();

	public override void OnUpdate()
	{
		UpdateInsideLighting();
		UpdateWaterfallSounds();
		if (_level.CurrentAct == 1)
			return;
		_level.FinishOnCompleteLevel = false;
		switch (_state)
		{
			case 0:
				double left = _level.Camera.Bounds.Left;
				Vector2i position = _level.GetMarker("boss_oneway").Position;
				double x1 = position.X;
				if (left < x1)
					break;
				Rectanglei bounds1 = _level.Bounds;
				ref Rectanglei local1 = ref bounds1;
				position = _level.GetMarker("boss_oneway").Position;
				int x2 = position.X;
				local1.Left = x2;
				ref Rectanglei local2 = ref bounds1;
				position = _level.GetMarker("boss_bottom").Position;
				int y = position.Y;
				local2.Bottom = y;
				_level.ScrollBoundsTo(bounds1, 2);
				++_state;
				break;
			case 1:
				if (_level.IsScrollingBounds || _level.Camera.Bounds.Left < _level.GetMarker("boss_left").Position.X)
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
					_level.SoundManager.PlayMusic(this.GetAbsolutePath("SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT2"));
				}
				if (!_bossObject.Fleeing)
					break;
				++_state;
				break;
			case 4:
				Rectangle bounds2 = (Rectangle)_level.Bounds;
				ref Rectangle local3 = ref bounds2;
				double val1 = bounds2.Right + 8.0;
				Rectanglei rectanglei = GetAct2Bounds();
				double right1 = rectanglei.Right;
				double num = Math.Min(val1, right1);
				local3.Right = num;
				bounds2.Left = Math.Max(bounds2.Left, _level.Camera.Bounds.Left);
				_level.Bounds = (Rectanglei)bounds2;
				if (!_level.StateFlags.HasFlag(LevelStateFlags.StageCompleted))
					break;
				++_state;
				bounds2 = (Rectangle)_level.Bounds;
				_level.Camera.Limits = (Rectanglei)bounds2;
				ref Rectangle local4 = ref bounds2;
				rectanglei = _level.Map.Bounds;
				double right2 = rectanglei.Right;
				local4.Right = right2;
				_level.Bounds = (Rectanglei)bounds2;
				break;
			case 5:
				_level.StateFlags &= ~LevelStateFlags.AllowCharacterControl;
				CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Protagonist);
				if (_level.Player.Sidekick != null)
					CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Sidekick);
				if (_level.Player.Protagonist.Position.X <= _level.Bounds.Right - 128 /*0x80*/)
					break;
				++_state;
				_sunFallTimer = 0;
				break;
			case 6:
				CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Protagonist);
				if (_level.Player.Sidekick != null)
					CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Sidekick);
				++_sunFallTimer;
				if (_sunFallTimer >= 120)
					_level.NightMode = Math.Min(_level.NightMode + 1.0 / 160.0, 1.0);
				if (_sunFallTimer < 380)
					break;
				_level.FadeOut(LevelState.StageCompleted);
				++_state;
				break;
			case 7:
				CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Protagonist);
				if (_level.Player.Sidekick == null)
					break;
				CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Sidekick);
				break;
		}
	}

	private void UpdateInsideLighting()
	{
		foreach (ICharacter character in _level.ObjectManager.Characters)
		{
			float num1 = 0.0f;
			foreach (LevelMarker levelMarker in _level.GetMarkersWithTag("inside"))
			{
				Rectanglei rectanglei = levelMarker.Bounds;
				rectanglei = rectanglei.Inflate(new Vector2i(-32, -32));
				Vector2i position = character.Position;
				int val1;
				if (position.X < rectanglei.X + rectanglei.Width / 2)
				{
					position = character.Position;
					val1 = position.X - rectanglei.Left;
				}
				else
				{
					int right = rectanglei.Right;
					position = character.Position;
					int x = position.X;
					val1 = right - x;
				}
				position = character.Position;
				int num2;
				if (position.Y < rectanglei.Y + rectanglei.Height / 2)
				{
					position = character.Position;
					num2 = position.Y - rectanglei.Top;
				}
				else
				{
					int bottom = rectanglei.Bottom;
					position = character.Position;
					int y = position.Y;
					num2 = bottom - y;
				}
				int val2 = num2;
				int num3 = Math.Min(val1, val2);
				if (num3 >= -64)
				{
					num1 = (float)(MathX.Clamp(0.0, (num3 + 64 /*0x40*/) / 64.0, 1.0) * -0.41);
					break;
				}
			}
			character.Brightness = num1;
		}
	}

	private void SpawnBoss()
	{
		Rectanglei bossBounds = GetBossBounds();
		LevelMarker marker = _level.GetMarker("boss_position");
		_bossObject = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath("SONICORCA/OBJECTS/DRILLEGGMAN"), _level.Map.Layers.IndexOf(marker.Layer), marker.Position, new
		{
			LeftEdge = bossBounds.Left,
			RightEdge = bossBounds.Right
		})) as BossObject;
	}

	private Rectanglei GetAct1Bounds()
	{
		Vector2i position1 = _level.GetMarker("bounds_1_lt").Position;
		Vector2i position2 = _level.GetMarker("bounds_1_rb").Position;
		return Rectanglei.FromLTRB(position1.X, position1.Y, position2.X, position2.Y);
	}

	private Rectanglei GetAct2Bounds()
	{
		Vector2i position1 = _level.GetMarker("bounds_2_lt").Position;
		Vector2i position2 = _level.GetMarker("bounds_2_rb").Position;
		return Rectanglei.FromLTRB(position1.X, position1.Y, position2.X, position2.Y);
	}

	private Rectanglei GetBossBounds()
	{
		return Rectanglei.FromLTRB(_level.GetMarker("boss_left").Position.X, _level.Bounds.Top, _level.GetMarker("boss_right").Position.X, _level.GetMarker("boss_bottom").Position.Y);
	}

	private IEnumerable<Rectanglei> GetWaterfalls(LevelLayer layer)
	{
		List<Rectanglei> rects = new List<Rectanglei>();
		for (int x = 0; x < layer.Columns; ++x)
		{
			Rectanglei currentRect = new Rectanglei(x, 0, 1, 0);
			for (int index = 0; index < layer.Rows; ++index)
			{
				if (IsWaterfallTopTile(layer.Tiles[x, index]) && currentRect.Height != 0)
				{
					AggregateWaterfalls(rects, currentRect);
					currentRect.Y = index;
					currentRect.Height = 1;
				}
				else if (IsWaterfallTile(layer.Tiles[x, index]))
				{
					if (currentRect.Height == 0)
					{
						currentRect.Y = index;
						currentRect.Height = 1;
					}
					else
						++currentRect.Height;
				}
				else if (currentRect.Height != 0)
				{
					AggregateWaterfalls(rects, currentRect);
					currentRect.Y = 0;
					currentRect.Height = 0;
				}
			}
			if (currentRect.Height != 0)
				AggregateWaterfalls(rects, currentRect);
		}
		return rects;
	}

	private void AggregateWaterfalls(IList<Rectanglei> rects, Rectanglei currentRect)
	{
		bool flag = false;
		for (int index = 0; index < rects.Count; ++index)
		{
			Rectanglei rect = rects[index];
			if (rect.Right == currentRect.X && rect.Y == currentRect.Y && rect.Height == currentRect.Height)
			{
				++rect.Width;
				rects[index] = rect;
				flag = true;
			}
		}
		if (flag)
			return;
		rects.Add(currentRect);
	}

	private bool IsWaterfallTile(int tile)
	{
		int num = tile & 4095 /*0x0FFF*/;
		return num >= 780 && num <= 803;
	}

	private bool IsWaterfallTopTile(int tile)
	{
		int num = tile & 4095 /*0x0FFF*/;
		return num >= 792 && num <= 795;
	}
}
