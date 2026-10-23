using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.LEVELS.HTZ.AREA;

/// <summary>
/// The area for Hill Top Zone. It sets the act bounds and music, makes characters darker inside caves,
/// and makes characters brighter near lava. In act 2 it runs the Submarine Eggman boss fight.
/// </summary>
public class HillTopZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/SUBMARINEEGGMAN";
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
	{
		"SONICORCA/OBJECTS/LOCKY",
		"SONICORCA/OBJECTS/WOCKY"
	};
	private const string Act1MusicResourceKey = "SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT1";
	private const string Act2MusicResourceKey = "SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT2";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	private const string DandelionResourceKey = "SONICORCA/PARTICLE/DENDALEON";
	private const string LavaObjectResourceKey = "SONICORCA/OBJECTS/LAVA";

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
		DandelionResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	/// <summary>
	/// The distance between the bottom of the map and the bottom of the act bounds.
	/// </summary>
	private const int BoundsBottomMargin = 64;

	/// <summary>
	/// The distance that the boss arena bounds extend past the left and right boss markers.
	/// </summary>
	private const int BossArenaMargin = 68;

	/// <summary>
	/// The number of ticks between the boss arena lock and the boss spawn.
	/// </summary>
	private const int BossSpawnDelay = 90;

	/// <summary>
	/// The speed at which the bounds move after the boss flees.
	/// </summary>
	private const double BoundsOpenSpeed = 8.0;

	/// <summary>
	/// The protagonist position after the boss arena at which the bottom of the bounds starts to move up,
	/// and the largest distance that it moves up.
	/// </summary>
	private const int BoundsRaiseStartX = 95700;
	private const int BoundsRaiseDistance = 3000;

	/// <summary>
	/// The distance into an inside marker, after the marker shrinks by <see cref="LightingMarkerInset"/>,
	/// at which a character becomes fully dark.
	/// </summary>
	private const int LightingFadeDistance = 64;
	private const int LightingMarkerInset = 32;
	private const float InsideBrightness = -0.41f;
	private const double LavaBrightness = 0.4;
	private const float LavaMaxBrightness = 0.4f;

	/// <summary>
	/// The height above the lava surface that the lava glow reaches.
	/// </summary>
	private const int LavaGlowHeight = 256;

	/// <summary>
	/// The map layer that the dandelion particles are on.
	/// </summary>
	private const int DandelionLayerIndex = 21;

	/// <summary>
	/// The number of ticks between two dandelion particles.
	/// </summary>
	private const int DandelionInterval = 8;

	/// <summary>
	/// The number of ticks that <see cref="InitialiseDandelions"/> runs the particle manager in advance.
	/// </summary>
	private const int DandelionWarmUpTicks = 300;

	/// <summary>
	/// The camera position after which the dandelion particles appear.
	/// </summary>
	private const double DandelionStartX = 32000.0;

	private SonicOrcaGameContext _gameContext;
	private Level _level;
	private int _state;
	private int _stateTimer;
	private BossObject _bossObject;
	private bool _bossDefeated;
	private LevelLayer _lavaLayerA;
	private LevelLayer _lavaLayerB;
	private LevelMarker[] _insideAreaMarkers;
	private LevelMarker[] _lavaCaveAreaMarkers;

	// The value starts at -1, and no code sets it to 0. Thus the time of day logic in OnUpdate does not run.
	private int _timeOfDayState = -1;
	private ITexture _dandelionTexture;

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

	public HillTopZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_level = level;
		_level.Name = "Hill Top";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.AnimalResourceKeys = AnimalResourceKeys;
		if (!settings.Seamless)
		{
			_level.TileSet = _gameContext.ResourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			_level.LoadMap(_gameContext.ResourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_gameContext.ResourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
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
	}

	private void PrepareAct2()
	{
		_level.LevelMusic = Act2MusicResourceKey;
		_level.CurrentAct = 2;
		_level.SetStartPosition("startpos_stk_2");
		_level.Bounds = GetAct2Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		_state = 0;
		_bossDefeated = false;
	}

	private void SeamlessPrepareAct2()
	{
		_level.LevelMusic = Act2MusicResourceKey;
		_level.CurrentAct = 2;
		Rectanglei act2Bounds = GetAct2Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(act2Bounds);
		ExtendSeamlessLevelBounds(_level, act2Bounds);
		_state = 0;
		_bossDefeated = false;
	}

	public override void OnStart()
	{
		_lavaLayerA = _level.Map.FindLayer("lavawallcave");
		_lavaLayerB = _level.Map.FindLayer("lavawall middle");
		_insideAreaMarkers = _level.GetMarkersWithTag("inside");
		_lavaCaveAreaMarkers = _level.GetMarkersWithTag("lavacave");
	}

	public override void OnUpdate()
	{
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Updating))
			return;
		Vector2 position = (Vector2)_level.Player.Protagonist.Position;
		switch (_timeOfDayState)
		{
			case 0:
				if (position.X > 22272.0)
					_timeOfDayState = 1;
				break;
			case 1:
				if (position.Y > 4480.0)
					_timeOfDayState = 2;
				break;
			case 2:
				_level.NightMode = MathX.Clamp(_level.NightMode, (position.X - 19456.0) / 14784.0, 1.0);
				if (_level.NightMode >= 1.0)
					_timeOfDayState = 3;
				break;
			case 3:
				_level.NightMode = MathX.Clamp(_level.NightMode, (position.X - 0.0) / 98816.0, 1.0);
				break;
		}
		if (_level.CurrentAct == 2)
			Act2Events();
		UpdateInsideLighting();
		UpdateLavaGlow();
	}

	private void SpawnBoss()
	{
		LevelMarker marker = _level.GetMarker("boss_position");
		_bossObject = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath(BossObjectResourceKey), _level.Map.Layers.IndexOf(marker.Layer), marker.Position)) as BossObject;
	}

	private Rectanglei GetAct1Bounds()
	{
		return Rectanglei.FromLTRB(0, 0, _level.GetMarker("bounds_1_r").Position.X, _level.Map.Bounds.Height - BoundsBottomMargin);
	}

	private Rectanglei GetAct2Bounds()
	{
		int left = _level.GetMarker("bounds_2_l").Position.X;
		int right = _level.Map.Bounds.Width;
		int bottom = _level.Map.Bounds.Height - BoundsBottomMargin;
		return Rectanglei.FromLTRB(left, 0, right, bottom);
	}

	/// <summary>
	/// Runs the boss fight in act 2. The states start at 6.
	/// </summary>
	private void Act2Events()
	{
		if (_state < 6)
		{
			// When the camera passes the one-way marker, the player cannot go back to the left.
			Vector2i oneWayPosition = _level.GetMarker("boss_oneway").Position;
			if (_level.Camera.Bounds.Left >= oneWayPosition.X)
			{
				_level.Bounds = (Rectanglei)((Rectangle)_level.Bounds with
				{
					Left = oneWayPosition.X,
					Bottom = _level.Camera.Bounds.Bottom
				});
				_state = 6;
			}
		}
		switch (_state)
		{
			case 6:
				{
					// Move the bottom of the bounds up to the arena. When the camera reaches the arena, lock it.
					int arenaBottom = _level.GetMarker("boss_oneway").Position.Y;
					int arenaLeft = _level.GetMarker("boss_left").Position.X - BossArenaMargin;
					int arenaRight = _level.GetMarker("boss_right").Position.X + BossArenaMargin;
					if (_level.Camera.Bounds.Left >= arenaLeft)
					{
						_level.Bounds = (Rectanglei)((Rectangle)_level.Bounds with
						{
							Left = arenaLeft,
							Right = arenaRight,
							Bottom = arenaBottom
						});
						_level.SoundManager.CrossFadeMusic(this.GetAbsolutePath(BossMusicResourceKey));
						_stateTimer = BossSpawnDelay;
						++_state;
						break;
					}
					Rectangle bounds = (Rectangle)_level.Bounds;
					bounds.Bottom = MathX.GoTowards(bounds.Bottom, arenaBottom, 2.0);
					_level.Bounds = (Rectanglei)bounds;
					break;
				}
			case 7:
				--_stateTimer;
				if (_stateTimer > 0)
					break;
				SpawnBoss();
				++_state;
				break;
			case 8:
				// The boss fight. When the boss is defeated, play the level music again.
				if (!_bossDefeated && _bossObject != null && _bossObject.Defeated)
				{
					_bossDefeated = true;
					_level.SoundManager.PlayMusic(this.GetAbsolutePath(Act2MusicResourceKey));
				}
				if (_bossObject == null || !_bossObject.Fleeing)
					break;
				++_state;
				break;
			case 9:
				{
					// Open the bounds to the right. After the arena, move the bottom of the bounds up.
					Rectangle bounds = (Rectangle)_level.Bounds;
					bounds.Right = Math.Min(bounds.Right + BoundsOpenSpeed, GetAct2Bounds().Right);
					bounds.Left = GetAct2Bounds().Left;
					bounds.Left = Math.Max(bounds.Left, _level.Camera.Bounds.Left);
					if (_level.Player.Protagonist.Position.X >= BoundsRaiseStartX)
						bounds.Bottom = Math.Max(bounds.Bottom - BoundsOpenSpeed, GetAct2Bounds().Bottom - BoundsRaiseDistance);
					_level.Bounds = (Rectanglei)bounds;
					break;
				}
		}
	}

	// Not called. It shows the lava cave layers only when the camera is in a lava cave area.
	private void UpdateLayerVisibility()
	{
		Rectanglei bounds = (Rectanglei)_level.Camera.Bounds;
		foreach (LevelMarker lavaCaveAreaMarker in _lavaCaveAreaMarkers)
		{
			if (bounds.IntersectsWith(lavaCaveAreaMarker.Bounds))
			{
				ShowLavaCaves(true);
				return;
			}
		}
		ShowLavaCaves(false);
	}

	private void ShowLavaCaves(bool value)
	{
		_lavaLayerA.Visible = value;
		_lavaLayerB.Visible = value;
	}

	// Not called. It adds dandelion particles and runs the particle manager in advance,
	// so that the particles are already on the screen.
	private void InitialiseDandelions()
	{
		if (_dandelionTexture == null)
			_dandelionTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>(DandelionResourceKey);
		if (!ShouldCreateDandelions())
			return;
		ParticleManager particleManager = _level.ParticleManager;
		for (int tick = 0; tick < DandelionWarmUpTicks; ++tick)
		{
			if (tick % DandelionInterval == 0)
				particleManager.Add(CreateParticle());
			particleManager.Update();
		}
	}

	// Not called. It adds a dandelion particle at a fixed interval.
	private void UpdateDandelions()
	{
		if (!ShouldCreateDandelions() || _level.Ticks % DandelionInterval != 0)
			return;
		_level.ParticleManager.Add(CreateParticle());
	}

	/// <summary>
	/// Creates a dandelion particle in a band to the right of the camera. Smaller particles move slower.
	/// </summary>
	private Particle CreateParticle()
	{
		Random random = _level.Random;
		Rectanglei cameraBounds = (Rectanglei)_level.Camera.Bounds;
		Rectanglei spawnArea = new Rectanglei(cameraBounds.Right, cameraBounds.Top + 4, 128, cameraBounds.Height - 8);
		int x = _level.Random.Next(spawnArea.Left, spawnArea.Right);
		int y = _level.Random.Next(spawnArea.Top, spawnArea.Bottom);
		double size = _level.Random.NextDouble(0.1, 0.7);
		double speed = MathX.Lerp(2.0, 18.0, (size - 0.1) / 0.6);
		return new Particle()
		{
			Type = ParticleType.Custom,
			Time = 1200,
			Velocity = new Vector2(-speed, _level.Random.NextDouble(-2.0, 2.0)),
			Angle = random.NextDouble(2.0 * Math.PI),
			AngularVelocity = random.NextDouble(0.05, 0.1),
			CustomTexture = _dandelionTexture,
			Position = new Vector2(x, y),
			Layer = _level.Map.Layers[DandelionLayerIndex],
			Size = size
		};
	}

	private bool ShouldCreateDandelions() => _level.Camera.Bounds.Right > DandelionStartX;

	/// <summary>
	/// Makes each character darker when it is inside an area that has an "inside" marker.
	/// The character becomes darker gradually near the edge of the area.
	/// </summary>
	private void UpdateInsideLighting()
	{
		foreach (ICharacter character in _level.ObjectManager.Characters)
		{
			float brightness = 0.0f;
			foreach (LevelMarker insideAreaMarker in _insideAreaMarkers)
			{
				Rectanglei insideBounds = insideAreaMarker.Bounds.Inflate(new Vector2i(-LightingMarkerInset, -LightingMarkerInset));
				int edgeDistance = Math.Min(GetHorizontalEdgeDistance(insideBounds, character.Position), GetVerticalEdgeDistance(insideBounds, character.Position));
				if (edgeDistance >= -LightingFadeDistance)
				{
					float darkness = (float)MathX.Clamp(0.0, (edgeDistance + LightingFadeDistance) / 64.0, 1.0);
					if (darkness == 1.0)
					{
						brightness = InsideBrightness;
						break;
					}
					brightness = Math.Min(darkness * InsideBrightness, brightness);
				}
			}
			character.Brightness = brightness;
		}
	}

	/// <summary>
	/// Makes each character brighter when it is near the surface of a lava object.
	/// This runs after <see cref="UpdateInsideLighting"/> and adds to its brightness.
	/// </summary>
	private void UpdateLavaGlow()
	{
		ObjectType lavaType = _level.ObjectManager.RegisteredTypes.FirstOrDefault(x => x.ResourceKey == LavaObjectResourceKey);
		List<Rectanglei> glowAreas = new List<Rectanglei>();
		if (lavaType != null)
		{
			foreach (ActiveObject activeObject in _level.ObjectManager.ActiveObjects)
			{
				if (activeObject.Type == lavaType && activeObject.CollisionVectors.Length >= 2)
				{
					// The second collision vector is the lava surface. The glow area is above it.
					CollisionVector surface = activeObject.CollisionVectors[1];
					Rectanglei glowArea = Rectanglei.FromLTRB(surface.AbsoluteA.X, surface.AbsoluteA.Y - LavaGlowHeight, surface.AbsoluteB.X, surface.AbsoluteB.Y).Inflate(new Vector2i(-32, 32));
					glowAreas.Add(glowArea);
				}
			}
		}
		foreach (ICharacter character in _level.ObjectManager.Characters)
		{
			float glow = 0.0f;
			foreach (Rectanglei glowArea in glowAreas)
			{
				int distanceX = GetHorizontalEdgeDistance(glowArea, character.Position);
				int distanceY = GetVerticalEdgeDistance(glowArea, character.Position);
				if (distanceX >= -LightingFadeDistance && distanceY >= -LightingFadeDistance)
					glow = Math.Min((float)(MathX.Clamp(0.0, (distanceX + LightingFadeDistance) / 64.0, 1.0) * LavaBrightness), (float)(MathX.Clamp(0.0, (distanceY + LightingFadeDistance) / (double)glowArea.Height, 1.0) * LavaBrightness));
			}
			character.Brightness = Math.Min(character.Brightness + glow * 2f, LavaMaxBrightness);
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
}
