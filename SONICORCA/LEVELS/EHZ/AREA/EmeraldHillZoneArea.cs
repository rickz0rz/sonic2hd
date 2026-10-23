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

/// <summary>
/// The area for Emerald Hill Zone. It sets the act bounds and music, plays the waterfall sound near waterfalls,
/// and makes characters darker inside caves. In act 2 it runs the Drill Eggman boss fight and the sunset at the end.
/// </summary>
public class EmeraldHillZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/DRILLEGGMAN";
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
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

	// AnimalResourceKeys must stay above AreaDependencies, because static fields are set in the order of the source.
	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		BossObjectResourceKey,
		Act1MusicResourceKey,
		Act2MusicResourceKey,
		BossMusicResourceKey,
		Act1FastMusicResourceKey,
		Act2FastMusicResourceKey,
		WaterfallResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	/// <summary>
	/// The size of a map tile, in level units.
	/// </summary>
	private const int TileSize = 64;

	/// <summary>
	/// The map layer that has the waterfall tiles.
	/// </summary>
	private const int WaterfallTileLayerIndex = 10;

	/// <summary>
	/// The map layer that draws the waterfall effects.
	/// </summary>
	private const int WaterfallEffectLayerIndex = 8;

	/// <summary>
	/// The largest horizontal and vertical distances from the camera centre at which the waterfall sound plays.
	/// </summary>
	private const int WaterfallAudibleDistanceX = 1000;
	private const int WaterfallAudibleDistanceY = 1400;

	private const double WaterfallMaxVolume = 0.75;

	/// <summary>
	/// The tile index range of all waterfall tiles, and the range of the tiles at the top of a waterfall.
	/// </summary>
	private const int WaterfallFirstTile = 780;
	private const int WaterfallLastTile = 803;
	private const int WaterfallTopFirstTile = 792;
	private const int WaterfallTopLastTile = 795;

	/// <summary>
	/// The bits of a tile value that hold the tile index. The other bits hold the flags.
	/// </summary>
	private const int TileIndexMask = 0x0FFF;

	/// <summary>
	/// The distance into an inside marker, after the marker shrinks by <see cref="InsideMarkerInset"/>,
	/// at which a character becomes fully dark.
	/// </summary>
	private const int InsideFadeDistance = 64;
	private const int InsideMarkerInset = 32;
	private const double InsideBrightness = -0.41;

	/// <summary>
	/// The number of ticks between the boss arena lock and the boss spawn.
	/// </summary>
	private const int BossSpawnDelay = 90;

	/// <summary>
	/// The speed at which the bounds open to the right after the boss flees.
	/// </summary>
	private const double BoundsOpenSpeed = 8.0;

	/// <summary>
	/// The distance from the right edge of the bounds at which the sunset starts.
	/// </summary>
	private const int SunsetStartDistance = 128;

	private const int SunsetNightModeDelay = 120;
	private const double SunsetNightModeStep = 1.0 / 160.0;
	private const int SunsetFadeOutTicks = 380;

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
			return new KeyValuePair<string, object>[]
			{
				new KeyValuePair<string, object>("STATE", _state),
				new KeyValuePair<string, object>("BOSS DEFEATED", _bossDefeated)
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
		_level.AnimalResourceKeys = AnimalResourceKeys.Select(x => this.GetAbsolutePath(x)).ToArray();
		if (!settings.Seamless)
		{
			_level.TileSet = _gameContext.ResourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			_level.LoadMap(_gameContext.ResourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_gameContext.ResourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
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
		_level.LevelMusic = Act1MusicResourceKey;
		_level.CurrentAct = 1;
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = GetAct1Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		_level.SoundManager.SetJingle(JingleType.SpeedShoes, Act1FastMusicResourceKey);
	}

	private void PrepareAct2(bool seamless)
	{
		_level.SoundManager.SetJingle(JingleType.SpeedShoes, Act2FastMusicResourceKey);
		_level.LevelMusic = Act2MusicResourceKey;
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
		_waterfallSampleInstance = new SampleInstance(_gameContext, _gameContext.ResourceTree.GetLoadedResource<SampleInfo>(WaterfallResourceKey));
		_waterfallRects = GetWaterfalls(_level.Map.Layers[WaterfallTileLayerIndex]).AsArray();
		_level.Map.Layers[WaterfallEffectLayerIndex].WaterfallEffects = _waterfallRects;
	}

	/// <summary>
	/// Plays the waterfall sound for the waterfall that is horizontally nearest to the camera centre.
	/// The volume and the pan change with the distance.
	/// </summary>
	private void UpdateWaterfallSounds()
	{
		Vector2i centre = (Vector2i)_level.Camera.Bounds.Centre;
		Rectanglei nearestWaterfall = new Rectanglei();
		int distanceX = int.MaxValue;
		foreach (Rectanglei waterfallRect in _waterfallRects)
		{
			// The waterfall rectangles are in tiles.
			int waterfallDistanceX = centre.X - waterfallRect.Centre.X * TileSize;
			if (Math.Abs(waterfallDistanceX) < Math.Abs(distanceX))
			{
				distanceX = waterfallDistanceX;
				nearestWaterfall = waterfallRect;
			}
		}
		int distanceY = Math.Min(Math.Abs(centre.Y - nearestWaterfall.Top * TileSize), Math.Abs(centre.Y - nearestWaterfall.Bottom * TileSize));
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Paused) && !_level.Player.Protagonist.IsDead && Math.Abs(distanceX) < WaterfallAudibleDistanceX && distanceY < WaterfallAudibleDistanceY)
		{
			_waterfallSampleInstance.Volume = WaterfallMaxVolume * Math.Min(1.0 - Math.Abs(distanceX) / 1000.0, 1.0 - distanceY / 1400.0);
			_waterfallSampleInstance.Pan = distanceX / 1000.0;
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
				// Wait until the camera passes the one-way marker, then move the bounds to the boss arena.
				Vector2i oneWayPosition = _level.GetMarker("boss_oneway").Position;
				if (_level.Camera.Bounds.Left < oneWayPosition.X)
					break;
				Rectanglei arenaApproachBounds = _level.Bounds;
				arenaApproachBounds.Left = oneWayPosition.X;
				arenaApproachBounds.Bottom = _level.GetMarker("boss_bottom").Position.Y;
				_level.ScrollBoundsTo(arenaApproachBounds, 2);
				++_state;
				break;
			case 1:
				// Lock the boss arena and start the boss music.
				if (_level.IsScrollingBounds || _level.Camera.Bounds.Left < _level.GetMarker("boss_left").Position.X)
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
				{
					// Open the bounds to the right until the stage is complete.
					Rectangle bounds = (Rectangle)_level.Bounds;
					bounds.Right = Math.Min(bounds.Right + BoundsOpenSpeed, GetAct2Bounds().Right);
					bounds.Left = Math.Max(bounds.Left, _level.Camera.Bounds.Left);
					_level.Bounds = (Rectanglei)bounds;
					if (!_level.StateFlags.HasFlag(LevelStateFlags.StageCompleted))
						break;
					++_state;
					// Lock the camera, but let the characters run to the right edge of the map.
					bounds = (Rectangle)_level.Bounds;
					_level.Camera.Limits = (Rectanglei)bounds;
					bounds.Right = _level.Map.Bounds.Right;
					_level.Bounds = (Rectanglei)bounds;
					break;
				}
			case 5:
				_level.StateFlags &= ~LevelStateFlags.AllowCharacterControl;
				MoveCharactersRight();
				if (_level.Player.Protagonist.Position.X <= _level.Bounds.Right - SunsetStartDistance)
					break;
				++_state;
				_sunFallTimer = 0;
				break;
			case 6:
				// The sunset: the level becomes darker, and then it fades out.
				MoveCharactersRight();
				++_sunFallTimer;
				if (_sunFallTimer >= SunsetNightModeDelay)
					_level.NightMode = Math.Min(_level.NightMode + SunsetNightModeStep, 1.0);
				if (_sunFallTimer < SunsetFadeOutTicks)
					break;
				_level.FadeOut(LevelState.StageCompleted);
				++_state;
				break;
			case 7:
				MoveCharactersRight();
				break;
		}
	}

	/// <summary>
	/// Moves the protagonist and the sidekick (if there is one) to the right, and jumps over obstacles.
	/// </summary>
	private void MoveCharactersRight()
	{
		CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Protagonist);
		if (_level.Player.Sidekick != null)
			CharacterIntelligence.SimpleMoveRightJumpObsticals(_level.Player.Sidekick);
	}

	/// <summary>
	/// Makes each character darker when it is inside an area that has an "inside" marker.
	/// The character becomes darker gradually near the edge of the area.
	/// </summary>
	private void UpdateInsideLighting()
	{
		foreach (ICharacter character in _level.ObjectManager.Characters)
		{
			float brightness = 0.0f;
			foreach (LevelMarker levelMarker in _level.GetMarkersWithTag("inside"))
			{
				Rectanglei insideBounds = levelMarker.Bounds.Inflate(new Vector2i(-InsideMarkerInset, -InsideMarkerInset));
				int edgeDistance = Math.Min(GetHorizontalEdgeDistance(insideBounds, character.Position), GetVerticalEdgeDistance(insideBounds, character.Position));
				if (edgeDistance >= -InsideFadeDistance)
				{
					brightness = (float)(MathX.Clamp(0.0, (edgeDistance + InsideFadeDistance) / 64.0, 1.0) * InsideBrightness);
					break;
				}
			}
			character.Brightness = brightness;
		}
	}

	/// <summary>
	/// Gets the distance from the position to the nearest left or right edge of the rectangle.
	/// The value is positive when the position is inside the rectangle horizontally.
	/// </summary>
	private static int GetHorizontalEdgeDistance(Rectanglei rect, Vector2i position)
	{
		if (position.X < rect.X + rect.Width / 2)
			return position.X - rect.Left;
		return rect.Right - position.X;
	}

	/// <summary>
	/// Gets the distance from the position to the nearest top or bottom edge of the rectangle.
	/// The value is positive when the position is inside the rectangle vertically.
	/// </summary>
	private static int GetVerticalEdgeDistance(Rectanglei rect, Vector2i position)
	{
		if (position.Y < rect.Y + rect.Height / 2)
			return position.Y - rect.Top;
		return rect.Bottom - position.Y;
	}

	private void SpawnBoss()
	{
		Rectanglei bossBounds = GetBossBounds();
		LevelMarker marker = _level.GetMarker("boss_position");
		// The property names of the state object must match the state variables of the boss.
		_bossObject = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath(BossObjectResourceKey), _level.Map.Layers.IndexOf(marker.Layer), marker.Position, new
		{
			LeftEdge = bossBounds.Left,
			RightEdge = bossBounds.Right
		})) as BossObject;
	}

	private Rectanglei GetAct1Bounds() => GetMarkerBounds("bounds_1_lt", "bounds_1_rb");

	private Rectanglei GetAct2Bounds() => GetMarkerBounds("bounds_2_lt", "bounds_2_rb");

	/// <summary>
	/// Gets the rectangle between a left-top marker and a right-bottom marker.
	/// </summary>
	private Rectanglei GetMarkerBounds(string leftTopMarkerName, string rightBottomMarkerName)
	{
		Vector2i leftTop = _level.GetMarker(leftTopMarkerName).Position;
		Vector2i rightBottom = _level.GetMarker(rightBottomMarkerName).Position;
		return Rectanglei.FromLTRB(leftTop.X, leftTop.Y, rightBottom.X, rightBottom.Y);
	}

	private Rectanglei GetBossBounds()
	{
		return Rectanglei.FromLTRB(_level.GetMarker("boss_left").Position.X, _level.Bounds.Top, _level.GetMarker("boss_right").Position.X, _level.GetMarker("boss_bottom").Position.Y);
	}

	/// <summary>
	/// Finds the waterfalls in the layer. Each waterfall is a rectangle in tiles.
	/// A column of waterfall tiles becomes one rectangle, and the method joins rectangles of the same height
	/// in adjacent columns.
	/// </summary>
	private IEnumerable<Rectanglei> GetWaterfalls(LevelLayer layer)
	{
		List<Rectanglei> rects = new List<Rectanglei>();
		for (int x = 0; x < layer.Columns; ++x)
		{
			Rectanglei currentRect = new Rectanglei(x, 0, 1, 0);
			for (int y = 0; y < layer.Rows; ++y)
			{
				if (IsWaterfallTopTile(layer.Tiles[x, y]) && currentRect.Height != 0)
				{
					// A new waterfall starts below the current waterfall.
					AggregateWaterfalls(rects, currentRect);
					currentRect.Y = y;
					currentRect.Height = 1;
				}
				else if (IsWaterfallTile(layer.Tiles[x, y]))
				{
					if (currentRect.Height == 0)
					{
						currentRect.Y = y;
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

	/// <summary>
	/// Makes each rectangle that touches the left side of the current rectangle, and has the same top and height,
	/// one column wider. If no rectangle matches, adds the current rectangle.
	/// </summary>
	private void AggregateWaterfalls(IList<Rectanglei> rects, Rectanglei currentRect)
	{
		bool joined = false;
		for (int index = 0; index < rects.Count; ++index)
		{
			Rectanglei rect = rects[index];
			if (rect.Right == currentRect.X && rect.Y == currentRect.Y && rect.Height == currentRect.Height)
			{
				++rect.Width;
				rects[index] = rect;
				joined = true;
			}
		}
		if (joined)
			return;
		rects.Add(currentRect);
	}

	private bool IsWaterfallTile(int tile)
	{
		int tileIndex = tile & TileIndexMask;
		return tileIndex >= WaterfallFirstTile && tileIndex <= WaterfallLastTile;
	}

	private bool IsWaterfallTopTile(int tile)
	{
		int tileIndex = tile & TileIndexMask;
		return tileIndex >= WaterfallTopFirstTile && tileIndex <= WaterfallTopLastTile;
	}
}
