// Decompiled with JetBrains decompiler
// Type: SONICORCA.LEVELS.HTZ.AREA.HillTopZoneArea
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
namespace SONICORCA.LEVELS.HTZ.AREA;

public class HillTopZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/SUBMARINEEGGMAN";
	private static IReadOnlyList<string> AnimalResourceKeys = new string[2]
	{
	"SONICORCA/OBJECTS/LOCKY",
	"SONICORCA/OBJECTS/WOCKY"
	};
	private const string Act1MusicResourceKey = "SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT1";
	private const string Act2MusicResourceKey = "SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT2";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	private const string DendaleonResourceKey = "SONICORCA/PARTICLE/DENDALEON";
	public static IReadOnlyList<string> AreaDependencies = (new string[8]
	{
	"//TILESET",
	"//MAP",
	"//BINDING",
	"SONICORCA/OBJECTS/SUBMARINEEGGMAN",
	"SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT1",
	"SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT2",
	"SONICORCA/MUSIC/BOSS/S2",
	"SONICORCA/PARTICLE/DENDALEON"
	}).Concat<string>(AnimalResourceKeys).ToArray<string>();
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
	private int _timeOfDayState = -1;
	private ITexture _dendaleonTexture;

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
			_level.TileSet = _gameContext.ResourceTree.GetLoadedResource<TileSet>(this, "//TILESET");
			_level.LoadMap(_gameContext.ResourceTree.GetLoadedResource<LevelMap>(this, "//MAP"));
			_level.LoadBinding(_gameContext.ResourceTree.GetLoadedResource<LevelBinding>(this, "//BINDING"));
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
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT1";
		_level.CurrentAct = 1;
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = GetAct1Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
	}

	private void PrepareAct2()
	{
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT2";
		_level.CurrentAct = 2;
		_level.SetStartPosition("startpos_stk_2");
		_level.Bounds = GetAct2Bounds();
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		_state = 0;
		_bossDefeated = false;
	}

	private void SeamlessPrepareAct2()
	{
		_level.LevelMusic = "SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT2";
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
				{
					_timeOfDayState = 1;
					break;
				}
				break;
			case 1:
				if (position.Y > 4480.0)
				{
					_timeOfDayState = 2;
					break;
				}
				break;
			case 2:
				_level.NightMode = MathX.Clamp(_level.NightMode, (position.X - 19456.0) / 14784.0, 1.0);
				if (_level.NightMode >= 1.0)
				{
					_timeOfDayState = 3;
					break;
				}
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
		_bossObject = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath("SONICORCA/OBJECTS/SUBMARINEEGGMAN"), _level.Map.Layers.IndexOf(marker.Layer), marker.Position)) as BossObject;
	}

	private Rectanglei GetAct1Bounds()
	{
		return Rectanglei.FromLTRB(0, 0, _level.GetMarker("bounds_1_r").Position.X, _level.Map.Bounds.Height - 64 /*0x40*/);
	}

	private Rectanglei GetAct2Bounds()
	{
		int x = _level.GetMarker("bounds_2_l").Position.X;
		Rectanglei bounds = _level.Map.Bounds;
		int width = bounds.Width;
		bounds = _level.Map.Bounds;
		int bottom = bounds.Height - 64 /*0x40*/;
		return Rectanglei.FromLTRB(x, 0, width, bottom);
	}

	private void Act2Events()
	{
		if (_state < 6)
		{
			Vector2i position = _level.GetMarker("boss_oneway").Position;
			if (_level.Camera.Bounds.Left >= position.X)
			{
				_level.Bounds = (Rectanglei)((Rectangle)_level.Bounds with
				{
					Left = position.X,
					Bottom = _level.Camera.Bounds.Bottom
				});
				_state = 6;
			}
		}
		switch (_state)
		{
			case 6:
				int y = _level.GetMarker("boss_oneway").Position.Y;
				int num1 = _level.GetMarker("boss_left").Position.X - 68;
				int num2 = _level.GetMarker("boss_right").Position.X + 68;
				if (_level.Camera.Bounds.Left >= num1)
				{
					_level.Bounds = (Rectanglei)((Rectangle)_level.Bounds with
					{
						Left = num1,
						Right = num2,
						Bottom = y
					});
					_level.SoundManager.CrossFadeMusic(this.GetAbsolutePath("SONICORCA/MUSIC/BOSS/S2"));
					_stateTimer = 90;
					++_state;
					break;
				}
				Rectangle bounds1 = (Rectangle)_level.Bounds;
				bounds1.Bottom = MathX.GoTowards(bounds1.Bottom, y, 2.0);
				_level.Bounds = (Rectanglei)bounds1;
				break;
			case 7:
				--_stateTimer;
				if (_stateTimer > 0)
					break;
				SpawnBoss();
				++_state;
				break;
			case 8:
				if (!_bossDefeated && _bossObject != null && _bossObject.Defeated)
				{
					_bossDefeated = true;
					_level.SoundManager.PlayMusic(this.GetAbsolutePath("SONICORCA/MUSIC/LEVELS/HTZ/COOPERATIVE/ACT2"));
				}
				if (_bossObject == null || !_bossObject.Fleeing)
					break;
				++_state;
				break;
			case 9:
				Rectangle bounds2 = (Rectangle)_level.Bounds;
				bounds2.Right = Math.Min(bounds2.Right + 8.0, GetAct2Bounds().Right);
				bounds2.Left = GetAct2Bounds().Left;
				bounds2.Left = Math.Max(bounds2.Left, _level.Camera.Bounds.Left);
				if (_level.Player.Protagonist.Position.X >= 95700)
					bounds2.Bottom = Math.Max(bounds2.Bottom - 8.0, GetAct2Bounds().Bottom - 3000);
				_level.Bounds = (Rectanglei)bounds2;
				break;
		}
	}

	private void UpdateLayerVisibillity()
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

	private void InitialiseDendaleons()
	{
		if (_dendaleonTexture == null)
			_dendaleonTexture = _gameContext.ResourceTree.GetLoadedResource<ITexture>("SONICORCA/PARTICLE/DENDALEON");
		if (!ShouldCreateDendaleons())
			return;
		int num = 300;
		ParticleManager particleManager = _level.ParticleManager;
		for (int index = 0; index < num; ++index)
		{
			if (index % 8 == 0)
				particleManager.Add(CreateParticle());
			particleManager.Update();
		}
	}

	private void UpdateDendaleons()
	{
		if (!ShouldCreateDendaleons() || _level.Ticks % 8 != 0)
			return;
		_level.ParticleManager.Add(CreateParticle());
	}

	private Particle CreateParticle()
	{
		Random random = _level.Random;
		Rectanglei bounds = (Rectanglei)_level.Camera.Bounds;
		Rectanglei rectanglei = new Rectanglei(bounds.Right, bounds.Top + 4, 128 /*0x80*/, bounds.Height - 8);
		int x = _level.Random.Next(rectanglei.Left, rectanglei.Right);
		int y = _level.Random.Next(rectanglei.Top, rectanglei.Bottom);
		double num1 = _level.Random.NextDouble(0.1, 0.7);
		double num2 = MathX.Lerp(2.0, 18.0, (num1 - 0.1) / 0.6);
		return new Particle()
		{
			Type = ParticleType.Custom,
			Time = 1200,
			Velocity = new Vector2(-num2, _level.Random.NextDouble(-2.0, 2.0)),
			Angle = random.NextDouble(2.0 * Math.PI),
			AngularVelocity = random.NextDouble(0.05, 0.1),
			CustomTexture = _dendaleonTexture,
			Position = new Vector2(x, y),
			Layer = _level.Map.Layers[21],
			Size = num1
		};
	}

	private bool ShouldCreateDendaleons() => _level.Camera.Bounds.Right > 32000.0;

	private void UpdateInsideLighting()
	{
		foreach (ICharacter character in _level.ObjectManager.Characters)
		{
			float val2_1 = 0.0f;
			foreach (LevelMarker insideAreaMarker in _insideAreaMarkers)
			{
				Rectanglei rectanglei = insideAreaMarker.Bounds;
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
				int num1;
				if (position.Y < rectanglei.Y + rectanglei.Height / 2)
				{
					position = character.Position;
					num1 = position.Y - rectanglei.Top;
				}
				else
				{
					int bottom = rectanglei.Bottom;
					position = character.Position;
					int y = position.Y;
					num1 = bottom - y;
				}
				int val2_2 = num1;
				int num2 = Math.Min(val1, val2_2);
				if (num2 >= -64)
				{
					float num3 = (float)MathX.Clamp(0.0, (num2 + 64 /*0x40*/) / 64.0, 1.0);
					if ((double)num3 == 1.0)
					{
						val2_1 = -0.41f;
						break;
					}
					val2_1 = Math.Min(num3 * -0.41f, val2_1);
				}
			}
			character.Brightness = val2_1;
		}
	}

	private void UpdateLavaGlow()
	{
		ObjectType objectType = _level.ObjectManager.RegisteredTypes.FirstOrDefault<ObjectType>(x => x.ResourceKey == "SONICORCA/OBJECTS/LAVA");
		List<Rectanglei> rectangleiList = new List<Rectanglei>();
		if (objectType != null)
		{
			foreach (ActiveObject activeObject in (IEnumerable<ActiveObject>)_level.ObjectManager.ActiveObjects)
			{
				if (activeObject.Type == objectType && activeObject.CollisionVectors.Length >= 2)
				{
					CollisionVector collisionVector = activeObject.CollisionVectors[1];
					Vector2i vector2i = collisionVector.AbsoluteA;
					int x1 = vector2i.X;
					vector2i = collisionVector.AbsoluteA;
					int top = vector2i.Y - 256 /*0x0100*/;
					vector2i = collisionVector.AbsoluteB;
					int x2 = vector2i.X;
					vector2i = collisionVector.AbsoluteB;
					int y = vector2i.Y;
					Rectanglei rectanglei = Rectanglei.FromLTRB(x1, top, x2, y).Inflate(new Vector2i(-32, 32 /*0x20*/));
					rectangleiList.Add(rectanglei);
				}
			}
		}
		foreach (ICharacter character in _level.ObjectManager.Characters)
		{
			float num1 = 0.0f;
			foreach (Rectanglei rectanglei in rectangleiList)
			{
				Vector2i position;
				int num2;
				if (character.Position.X < rectanglei.X + rectanglei.Width / 2)
				{
					position = character.Position;
					num2 = position.X - rectanglei.Left;
				}
				else
					num2 = rectanglei.Right - character.Position.X;
				int num3 = num2;
				position = character.Position;
				int num4;
				if (position.Y < rectanglei.Y + rectanglei.Height / 2)
				{
					position = character.Position;
					num4 = position.Y - rectanglei.Top;
				}
				else
				{
					int bottom = rectanglei.Bottom;
					position = character.Position;
					int y = position.Y;
					num4 = bottom - y;
				}
				int num5 = num4;
				if (num3 >= -64 && num5 >= -64)
					num1 = Math.Min((float)(MathX.Clamp(0.0, (num3 + 64 /*0x40*/) / 64.0, 1.0) * 0.4), (float)(MathX.Clamp(0.0, (num5 + 64 /*0x40*/) / (double)rectanglei.Height, 1.0) * 0.4));
			}
			character.Brightness = Math.Min(character.Brightness + num1 * 2f, 0.4f);
		}
	}
}
