using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;
using SONICORCA.OBJECTS.DEATHEGGROBOT;
using SONICORCA.OBJECTS.SILVERSONIC;

namespace SONICORCA.LEVELS.DEZ.AREA;

/// <summary>
/// The area for Death Egg Zone. It loads the tile set, the map and the binding. The game data has the tiles, but no
/// collision, no objects and no music. A map from "s2hd-data import-level dez" adds the collision, the objects, and
/// the markers for the start position, the bounds and the origin of the act. With these markers, the area runs the
/// level events of the original (LevEvents_DEZ):
/// <list type="number">
/// <item>When the camera reaches x 320, Silver Sonic comes. At x $224 the camera locks for the fight.</item>
/// <item>After Silver Sonic explodes, the camera opens to the right and cannot go back to the left.</item>
/// <item>At x $680 the camera locks from $680 to $740 for the Death Egg Robot.</item>
/// <item>When the robot is defeated, the camera opens to the right again, and the player runs to the end.</item>
/// </list>
/// The original then shows the ending. Here the robot completes the stage. Death Egg Zone has one act, and the area
/// makes it the last act of the zone, so the stage does not go to an act 2. It is the last level of the game, so the
/// game shows the credits after it. Without the markers, the level bounds are the bounds of the map.
/// </summary>
public class DeathEggZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	/// <summary>The game data has no music for this zone. The level plays without music if no mod adds it.</summary>
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/DEZ";
	/// <summary>Only a mod from "s2hd-data import-level dez" has the bosses.</summary>
	private const string SilverSonicResourceKey = "SONICORCA/OBJECTS/SILVERSONIC";

	private const int Scale = 4;

	private const int OriginalScreenWidth = 320;

	/// <summary>LevEvents_DEZ_Routine1: Silver Sonic comes at this camera x.</summary>
	private const int SilverSonicCameraX = 320;

	/// <summary>LevEvents_DEZ_Routine3: the art of the robot loads at this camera x.</summary>
	private const int RobotArtCameraX = 0x300;

	/// <summary>LevEvents_DEZ_Routine4: the camera locks from this x to $C0 more for the robot.</summary>
	private const int RobotArenaX = 0x680;

	private const int RobotArenaWidth = 0xC0;

	/// <summary>The speed at which the bounds move to an arena: 2 original pixels each frame.</summary>
	private const int BoundsScrollSpeed = 2 * Scale;

	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		LevelMusicResourceKey,
		SilverSonicResourceKey
	}.ToArray();

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;

	/// <summary>The origin of the act in the map, or null without the markers of an imported map.</summary>
	private Vector2i? _origin;

	/// <summary>The bounds of the act, from the markers.</summary>
	private Rectanglei _actBounds;

	/// <summary>Dynamic_Resize_Routine: the step of the level events.</summary>
	private EventState _state;

	private SilverSonicInstance _silverSonic;

	private enum EventState
	{
		WaitForSilverSonic,
		WaitForSilverSonicArena,
		SilverSonicFight,
		WaitForRobotArt,
		WaitForRobotArena,
		RobotFight,
		Escape
	}

	public DeathEggZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Death Egg";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.LevelMusic = LevelMusicResourceKey;
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		// The zone has one act. Act 0 shows no act number, and a completed stage ends the zone: act 1 would load an
		// act 2.
		_level.CurrentAct = 0;
		_origin = null;
		_silverSonic = null;
		_state = EventState.WaitForSilverSonic;
		if (_level.TryGetMarker("bounds_1_lt", out LevelMarker leftTop) && _level.TryGetMarker("bounds_1_rb", out LevelMarker rightBottom))
		{
			// A map from "s2hd-data import-level dez": the start and the bounds of the act
			_level.SetStartPosition("startpos_stk_1");
			_actBounds = Rectanglei.FromLTRB(leftTop.Position.X, leftTop.Position.Y, rightBottom.Position.X, rightBottom.Position.Y);
			// The original does not load Tails in Death Egg Zone
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
			}
		}
		else
		{
			_level.Bounds = _level.Map.Bounds;
		}
		_level.Camera.Limits = Rectanglei.Empty;
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		settings.StartPath = 0;
	}

	public override void OnUpdate()
	{
		if (_level.StateFlags.HasFlag(LevelStateFlags.Updating) && _origin != null &&
			_resourceTree[this.GetAbsolutePath(SilverSonicResourceKey)]?.Resource != null)
		{
			UpdateEvents();
		}
	}

	/// <summary>LevEvents_DEZ, and the camera locks of Silver Sonic (ObjAF) and the Death Egg Robot (ObjC7).</summary>
	private void UpdateEvents()
	{
		double cameraX = GetCameraX();
		switch (_state)
		{
			case EventState.WaitForSilverSonic:
				if (cameraX < SilverSonicCameraX)
				{
					break;
				}
				SpawnSilverSonic();
				_state++;
				break;
			case EventState.WaitForSilverSonicArena:
				// ObjAF locks the camera at x $224 (Camera_Min_X_pos and Camera_Max_X_pos)
				if (cameraX < SilverSonicInstance.LockCameraX)
				{
					break;
				}
				LockArena(SilverSonicInstance.LockCameraX, SilverSonicInstance.LockCameraX);
				_state++;
				break;
			case EventState.SilverSonicFight:
				if (_silverSonic != null && !_silverSonic.Exploded)
				{
					break;
				}
				// Camera_Max_X_pos = $1000. Camera_Min_X_pos follows the camera from now.
				OpenRight();
				_state++;
				break;
			case EventState.WaitForRobotArt:
				KeepLeft();
				if (cameraX >= RobotArtCameraX)
				{
					_state++;
				}
				break;
			case EventState.WaitForRobotArena:
				KeepLeft();
				if (cameraX < RobotArenaX)
				{
					break;
				}
				LockArena(RobotArenaX, RobotArenaX + RobotArenaWidth);
				_state++;
				break;
			case EventState.RobotFight:
				// ObjC7 sets Camera_Max_X_pos to $1000 after the robot explodes
				if (!_level.ObjectManager.ActiveObjects.OfType<DeathEggRobotInstance>().Any(robot => robot.Escaping))
				{
					break;
				}
				OpenRight();
				_state++;
				break;
			case EventState.Escape:
				KeepLeft();
				break;
		}
	}

	private void SpawnSilverSonic()
	{
		Vector2i origin = _origin.Value;
		Vector2i position = new Vector2i(origin.X + SilverSonicInstance.InitX * Scale, origin.Y + SilverSonicInstance.InitY * Scale);
		// The property names of the state object must match the state variables of the boss
		var state = new { OriginX = origin.X, OriginY = origin.Y };
		int layer = Math.Max(_level.Map.Layers.IndexOf(_level.Player.Protagonist.Layer), 0);
		_silverSonic = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath(SilverSonicResourceKey), layer, position, state))
			as SilverSonicInstance;
	}

	/// <summary>
	/// Locks the camera between two original camera x values, with the screen of the original. When the HD screen is
	/// wider than that, the characters stay in the screen of the original, and the camera limits are as wide as the HD
	/// screen, centred on it. The camera is there already, because the lock comes when the camera reaches the x.
	/// Otherwise the bounds move to the arena at 2 original pixels each frame.
	/// </summary>
	private void LockArena(int minX, int maxX)
	{
		int left = ToLevelX(minX);
		int right = ToLevelX(maxX + OriginalScreenWidth);
		var arena = Rectanglei.FromLTRB(left, _actBounds.Top, right, _actBounds.Bottom);
		int width = (int)_level.Camera.Bounds.Width;
		if (right - left >= width)
		{
			_level.ScrollBoundsTo(arena, BoundsScrollSpeed);
			return;
		}
		_level.Bounds = arena;
		left = (left + right) / 2 - width / 2;
		right = left + width;
		_level.Camera.Limits = Rectanglei.FromLTRB(left, _actBounds.Top, right, _actBounds.Bottom);
	}

	/// <summary>The camera can go right to the end of the act, but not back to the left.</summary>
	private void OpenRight()
	{
		int left = Math.Min(_level.Bounds.Left, (int)_level.Camera.Bounds.Left);
		_level.Camera.Limits = Rectanglei.Empty;
		_level.Bounds = Rectanglei.FromLTRB(left, _actBounds.Top, _actBounds.Right, _actBounds.Bottom);
	}

	/// <summary>Camera_Min_X_pos follows the camera: the camera cannot go back to the left.</summary>
	private void KeepLeft()
	{
		Rectanglei bounds = _level.Bounds;
		int left = Math.Max(bounds.Left, (int)_level.Camera.Bounds.Left);
		if (left != bounds.Left)
		{
			_level.Bounds = Rectanglei.FromLTRB(left, bounds.Top, bounds.Right, bounds.Bottom);
		}
	}

	/// <summary>The original camera X: the left edge of a 320 pixel screen at the centre of the HD camera.</summary>
	private double GetCameraX() => (_level.Camera.Bounds.X + _level.Camera.Bounds.Width / 2 - _origin.Value.X) / Scale - OriginalScreenWidth / 2;

	private int ToLevelX(int x) => _origin.Value.X + x * Scale;
}
