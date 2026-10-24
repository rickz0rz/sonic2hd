using System;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// The camera positions of a boss arena, from the level events of the original act (LevEvents_xxx2 in s2disasm). All
/// values are original camera positions in pixels, from the origin of the act.
/// </summary>
/// <param name="TriggerX">The camera X at which the camera cannot go back to the left and goes down to <paramref name="MaxY"/>.</param>
/// <param name="MaxY">The lowest camera Y from the trigger.</param>
/// <param name="LockX">The camera X at which the arena locks.</param>
/// <param name="ArenaLeft">The minimum camera X of the arena.</param>
/// <param name="ArenaRight">The maximum camera X of the arena.</param>
/// <param name="MinY">The highest camera Y of the arena.</param>
/// <param name="BossPosition">The start position of the boss, from the init routine of the boss object.</param>
/// <param name="FleeRight">
/// The maximum camera X after the boss flees. The boss object of the original moves the limit 2 pixels each frame to
/// this value, so that the egg prison comes into view.
/// </param>
/// <param name="SpawnOnLock">True when the boss comes when the arena locks (ARZ). Otherwise it comes 90 frames later.</param>
public sealed record BossArenaSettings(int TriggerX, int MaxY, int LockX, int ArenaLeft, int ArenaRight, int MinY, Vector2i BossPosition,
	int FleeRight, bool SpawnOnLock = false);

/// <summary>
/// Runs the boss events of an imported act 2: the camera locks into the arena, the music stops, the boss comes, and
/// the level music plays again when the boss is defeated. The boss object takes the act origin in its OriginX and
/// OriginY state variables. Without the boss object in the resources (no mod), nothing happens.
/// </summary>
public sealed class BossArena
{
	private const int Scale = 4;

	private const int OriginalScreenWidth = 320;

	private const int OriginalScreenHeight = 224;

	private const int SpawnDelay = 90;

	private const int BoundsScrollSpeed = 2;

	/// <summary>The speed at which the right limit opens after the boss flees: 2 original pixels each frame.</summary>
	private const int FleeOpenSpeed = 2 * Scale;

	/// <summary>The HD screen shows 80 original pixels more on each side than the original screen.</summary>
	private const int HdMargin = 80;

	private readonly Level _level;

	private readonly Vector2i _origin;

	private readonly BossArenaSettings _settings;

	private readonly string _bossKey;

	private readonly string _bossMusic;

	private readonly string _levelMusic;

	private int _state;

	private int _timer;

	private BossObject _boss;

	private bool _levelMusicRestarted;

	/// <param name="origin">The position of the act origin in the level, in level units.</param>
	public BossArena(Level level, Vector2i origin, BossArenaSettings settings, string bossKey, string bossMusic, string levelMusic)
	{
		_level = level;
		_origin = origin;
		_settings = settings;
		_bossKey = bossKey;
		_bossMusic = bossMusic;
		_levelMusic = levelMusic;
	}

	public BossObject Boss => _boss;

	/// <summary>True when the camera has passed the trigger X: the boss events have started.</summary>
	public bool Triggered => _state > 0;

	/// <summary>The original camera X: the left edge of a 320 pixel screen at the centre of the HD camera.</summary>
	private int CameraX => (int)((_level.Camera.Bounds.X + _level.Camera.Bounds.Width / 2 - _origin.X) / Scale) - OriginalScreenWidth / 2;

	public void Update()
	{
		if (_level.GameContext.ResourceTree[_bossKey]?.Resource == null)
		{
			return;
		}
		switch (_state)
		{
			case 0:
				if (CameraX < _settings.TriggerX)
				{
					break;
				}
				_level.ScrollBoundsTo(_level.Bounds with
				{
					Left = (int)_level.Camera.Bounds.Left,
					Bottom = ToLevelY(_settings.MaxY + OriginalScreenHeight)
				}, BoundsScrollSpeed);
				_state++;
				break;
			case 1:
				if (CameraX < _settings.LockX || _level.IsScrollingBounds)
				{
					break;
				}
				_level.ScrollBoundsTo(GetArenaBounds(), BoundsScrollSpeed);
				_level.SoundManager.FadeOutMusic(60);
				_timer = SpawnDelay;
				if (_settings.SpawnOnLock)
				{
					SpawnBoss();
				}
				_state++;
				break;
			case 2:
				if (--_timer > 0)
				{
					break;
				}
				if (_boss == null)
				{
					SpawnBoss();
				}
				PlayMusic(_bossMusic);
				_state++;
				break;
			case 3:
				if (_boss == null)
				{
					break;
				}
				if (_boss.Defeated && !_levelMusicRestarted)
				{
					_levelMusicRestarted = true;
					PlayMusic(_levelMusic);
				}
				if (_boss.Fleeing)
				{
					OpenRight();
				}
				break;
		}
	}

	/// <summary>
	/// The arena: the original camera range plus the screen, centred and made larger when the HD screen is larger.
	/// </summary>
	private Rectanglei GetArenaBounds()
	{
		int left = ToLevelX(_settings.ArenaLeft);
		int right = ToLevelX(_settings.ArenaRight + OriginalScreenWidth);
		int top = ToLevelY(_settings.MinY);
		int bottom = ToLevelY(_settings.MaxY + OriginalScreenHeight);
		int width = (int)_level.Camera.Bounds.Width;
		int height = (int)_level.Camera.Bounds.Height;
		if (right - left < width)
		{
			int centre = (left + right) / 2;
			left = centre - width / 2;
			right = left + width;
		}
		if (bottom - top < height)
		{
			int centre = (top + bottom) / 2;
			top = centre - height / 2;
			bottom = top + height;
		}
		return Rectanglei.FromLTRB(left, top, right, bottom);
	}

	/// <summary>The camera cannot go back to the left, and the right limit moves to the egg prison.</summary>
	private void OpenRight()
	{
		Rectanglei bounds = _level.Bounds;
		int right = Math.Max(bounds.Right, Math.Min(bounds.Right + FleeOpenSpeed, ToLevelX(_settings.FleeRight + OriginalScreenWidth + HdMargin)));
		int left = Math.Max(bounds.Left, (int)_level.Camera.Bounds.Left);
		_level.Bounds = Rectanglei.FromLTRB(left, bounds.Top, right, bounds.Bottom);
	}

	private void SpawnBoss()
	{
		Vector2i position = new Vector2i(ToLevelX(_settings.BossPosition.X), ToLevelY(_settings.BossPosition.Y));
		// The property names of the state object must match the state variables of the boss
		var state = new { OriginX = _origin.X, OriginY = _origin.Y };
		int layer = _level.Map.Layers.IndexOf(_level.Player.Protagonist.Layer);
		_boss = _level.ObjectManager.AddObject(new ObjectPlacement(_bossKey, Math.Max(layer, 0), position, state)) as BossObject;
	}

	private void PlayMusic(string key)
	{
		if (!string.IsNullOrEmpty(key) && _level.GameContext.ResourceTree[key]?.Resource != null)
		{
			_level.SoundManager.PlayMusic(key);
		}
	}

	private int ToLevelX(int x) => _origin.X + x * Scale;

	private int ToLevelY(int y) => _origin.Y + y * Scale;
}
