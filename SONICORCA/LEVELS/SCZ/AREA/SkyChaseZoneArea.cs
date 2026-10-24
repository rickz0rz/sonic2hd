using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SONICORCA.LEVELS.SCZ.AREA;

/// <summary>
/// The area for Sky Chase Zone. It loads the tile set, the map and the binding, and sets the level music. A map from
/// "s2hd-data import-level scz" adds the collision, the objects, and the markers for the start position and the act
/// origin. With these markers, the area runs the level events of the original (LevEvents_SCZ and SwScrl_SCZ): the
/// camera moves by itself with the Tornado, and the act ends when the player runs off the right side of the Tornado
/// at the end. Without them, the level bounds are the bounds of the map.
/// </summary>
public class SkyChaseZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/SCZ";

	/// <summary>
	/// The animals that destroyed badniks set free. The original has a turtle and a chicken. The game data has
	/// neither, so Ricky and Flicky take their place.
	/// </summary>
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
	{
		"SONICORCA/OBJECTS/RICKY",
		"SONICORCA/OBJECTS/FLICKY"
	};

	// AnimalResourceKeys must stay above AreaDependencies, because static fields are set in the order of the source.
	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		LevelMusicResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	private const int Scale = 4;

	private const int OriginalScreenWidth = 320;

	private const int OriginalScreenHeight = 224;

	/// <summary>The camera starts $A0 pixels to the left of the player (LevelSize: X from 0, Y always 0).</summary>
	private const int CameraStartOffsetX = 0xA0;

	/// <summary>LevEvents_SCZ_Routine2: at this camera X, the Tornado flies down and to the left.</summary>
	private const int DescentX = 0x1180;

	/// <summary>LevEvents_SCZ_Routine3: at this camera Y, the Tornado flies to the right again.</summary>
	private const int DescentBottom = 0x500;

	/// <summary>LevEvents_SCZ_Routine4: at this camera X, the camera stops. The Tornado then takes the player to the end.</summary>
	private const int EndX = 0x1400;

	/// <summary>The left limit of the player: Camera_Min_X_pos (the camera X) plus $10 (Sonic_LevelBound).</summary>
	private const int PlayerLeft = 0x10;

	/// <summary>
	/// The right limit of the player: Camera_Max_X_pos plus $168 (Sonic_LevelBound). The Tornado sets
	/// Camera_Max_X_pos to the camera X minus $40, and at the end to the camera X.
	/// </summary>
	private const int PlayerRight = 0x168 - 0x40;

	private const int PlayerRightAtEnd = 0x168;

	/// <summary>Character.LevelBoundsMargin: a character stays this far inside the level bounds.</summary>
	private const int CharacterBoundsMargin = 36;

	/// <summary>
	/// SwScrl_SCZ: the background moves 1/2 pixel to the right each frame while the camera moves to the side, in
	/// either direction.
	/// </summary>
	private const double BackgroundSpeed = 0.5;

	/// <summary>The HD screen shows 80 original pixels more on each side than the original screen.</summary>
	private const int HdMargin = 80;

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private SkyChaseScroll _scroll;
	private LevelLayer _background;
	private double _backgroundX;
	private int _routine;
	private bool _finished;

	public SkyChaseZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Sky Chase";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.AnimalResourceKeys = AnimalResourceKeys.Select(x => this.GetAbsolutePath(x)).ToArray();
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
		_scroll = SkyChaseScroll.For(_level);
		_scroll.Stop();
		_routine = 0;
		_finished = false;
		_level.Bounds = _level.Map.Bounds;
		_level.Camera.Limits = Rectanglei.Empty;
		if (_level.TryGetMarker("origin_1", out LevelMarker origin) && _level.TryGetMarker("startpos_stk_1", out LevelMarker start))
		{
			// A map from "s2hd-data import-level scz". Tails flies the Tornado, so he is not a sidekick here.
			_level.SetStartPosition("startpos_stk_1");
			if (settings.ProtagonistCharacter == CharacterType.Null)
			{
				settings.ProtagonistCharacter = CharacterType.Sonic;
			}
			if (settings.ProtagonistCharacter != CharacterType.Tails)
			{
				settings.SidekickCharacter = CharacterType.Null;
			}
			// The blue sky: the backdrop colour of the original palette (line 2, colour 0)
			_level.BackdropColour = new Colour(255, 72, 72, 255);
			// The background does not move up or down (Camera_BG_Y_pos stays 0). Its top is at the top of the
			// original screen. It moves to the side by itself (see UpdateBackground).
			_background = _level.Map.Layers[0];
			_background.ParallaxY = 0.0;
			_background.OffsetY = (int)_level.Camera.Bounds.Height / 2 - OriginalScreenHeight / 2 * Scale;
			foreach (LayerRowDefinition row in _background.LayerRowDefinitions)
			{
				row.Parallax = 0.0;
			}
			_backgroundX = 0;
			int startX = (start.Position.X - origin.Position.X) / Scale;
			_scroll.Start(origin.Position, System.Math.Max(0, startX - CameraStartOffsetX), 0);
			UpdateBounds();
			UpdateBackground();
		}
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Map.Bounds);
		settings.StartPath = 0;
	}

	public override void OnUpdate()
	{
		if (_scroll == null || !_scroll.Active || !_level.StateFlags.HasFlag(LevelStateFlags.Updating))
		{
			return;
		}
		RunLevelEvents();
		_scroll.CameraX += _scroll.VelocityX;
		_scroll.CameraY += _scroll.VelocityY;
		if (_scroll.VelocityX != 0)
		{
			_backgroundX += BackgroundSpeed;
		}
		UpdateBounds();
		UpdateBackground();
		if (_scroll.Finished && !_finished)
		{
			// The original goes to Wing Fortress Zone at once, without a results screen (ObjB2_SCZ_Finished)
			_finished = true;
			_level.StateFlags &= ~LevelStateFlags.UpdateTime;
			_level.StateFlags |= LevelStateFlags.StageCompleted;
			_level.FadeOut(LevelState.StageCompleted);
		}
	}

	/// <summary>LevEvents_SCZ: the Tornado flies right, then down and left, then right again, and then stops.</summary>
	private void RunLevelEvents()
	{
		switch (_routine)
		{
			case 0:
				SetVelocity(1, 0);
				_routine++;
				break;
			case 1:
				if (_scroll.CameraX >= DescentX)
				{
					SetVelocity(-1, 1);
					_routine++;
				}
				break;
			case 2:
				if (_scroll.CameraY >= DescentBottom)
				{
					SetVelocity(1, 0);
					_routine++;
				}
				break;
			case 3:
				if (_scroll.CameraX >= EndX)
				{
					SetVelocity(0, 0);
					_routine++;
				}
				break;
		}
	}

	private void SetVelocity(int x, int y)
	{
		_scroll.VelocityX = x;
		_scroll.VelocityY = y;
	}

	/// <summary>
	/// Puts the camera at the original camera position: the 320 x 224 screen of the original is at the centre of the
	/// HD screen. The characters stay between the left and the right limits of the original, and a character that
	/// falls below the HD screen dies.
	/// </summary>
	private void UpdateBounds()
	{
		Camera camera = _level.Camera;
		int width = (int)camera.Bounds.Width;
		int height = (int)camera.Bounds.Height;
		int left = _scroll.ToLevelX(_scroll.CameraX + OriginalScreenWidth / 2) - width / 2;
		int top = _scroll.ToLevelY(_scroll.CameraY + OriginalScreenHeight / 2) - height / 2;
		camera.Limits = new Rectanglei(left, top, width, height);
		int playerRight = _scroll.CameraX >= EndX ? PlayerRightAtEnd : PlayerRight;
		_level.Bounds = Rectanglei.FromLTRB(
			_scroll.ToLevelX(_scroll.CameraX + PlayerLeft) - CharacterBoundsMargin,
			top - height,
			_scroll.ToLevelX(_scroll.CameraX + playerRight) + CharacterBoundsMargin + 1,
			top + height);
	}

	/// <summary>Camera_BG_X_pos: the background X at the left edge of the original screen.</summary>
	private void UpdateBackground()
	{
		foreach (LayerRowDefinition row in _background.LayerRowDefinitions)
		{
			row.CurrentOffset = -(_backgroundX - HdMargin) * Scale;
		}
	}
}
