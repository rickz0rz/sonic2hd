using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.DEATHEGGROBOT.SENSOR;

/// <summary>
/// The targeting sensor of the Death Egg Robot, as in Sonic 2 (ObjC7, routines $18 and $1A). It starts on the player
/// and follows the player for 160 frames, with the velocity of the player 4 frames before. When the player does not
/// move on an axis, it goes to the player on that axis. Then it goes to the player and locks on for 64 frames, with a
/// flashing lock mark. Then it gives its x to the robot in <see cref="TargetX"/> and goes. It beeps faster and faster,
/// but the game data has no beep.
/// </summary>
public class DeathEggRobotSensorInstance : ActiveObject
{
	/// <summary>The number of mapping frames of the robot art. The second palette line follows them.</summary>
	private const int FrameCount = 23;

	private const int LockFrame = 0x14;

	private const int FollowTicks = 0xA0;

	private const int LockTicks = 0x40;

	/// <summary>Ani_objC7_c.</summary>
	private readonly AnimateSpriteScript _animator = new AnimateSpriteScript(new byte[] { 3, 0x13, 0x12, 0x11, 0x10, 0x16, 0xFF });

	/// <summary>objoff_30 to objoff_3F: the velocities of the player on the last 4 frames, the newest first.</summary>
	private readonly Vector2[] _history = new Vector2[4];

	private AnimationInstance _animation;

	private bool _locked;

	private int _timer;

	private Vector2 _velocity;

	private Vector2i _lockPosition;

	private int _lockTimer;

	private bool _lockFlash;

	/// <summary>The x where the sensor locked on, in level units, or null before that.</summary>
	[HideInEditor]
	public int? TargetX { get; private set; }

	public DeathEggRobotSensorInstance() => DesignBounds = new Rectanglei(-1024, -1024, 2048, 2048);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(DeathEggRobotSensorType.AnimationGroupResourceKey));
		_animator.Frame = 0x10;
		LockLifetime = true;
		// Priority 1: in front of the player
		Priority = 2048;
		_timer = FollowTicks;
		ICharacter player = Level.Player.Protagonist;
		if (player != null)
		{
			Position = player.Position;
			_history[0] = player.Velocity;
		}
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		ICharacter player = Level.Player.Protagonist;
		if (player == null)
		{
			return;
		}
		if (!_locked)
		{
			UpdateFollow(player);
			return;
		}
		if (--_timer < 0)
		{
			TargetX = Position.X;
			FinishForever();
			return;
		}
		_animator.Step();
		// The lock mark changes its palette line every 4 frames
		if (--_lockTimer == 0)
		{
			_lockTimer = 4;
			_lockFlash = !_lockFlash;
		}
	}

	/// <summary>loc_3DDA6: follow the player with the velocity of 4 frames before.</summary>
	private void UpdateFollow(ICharacter player)
	{
		if (--_timer < 0)
		{
			// loc_3DE0A: lock on
			_locked = true;
			_timer = LockTicks;
			Position = player.Position;
			_lockPosition = Position;
			_lockTimer = 4;
			return;
		}
		_velocity = _history[3];
		_history[3] = _history[2];
		_history[2] = _history[1];
		_history[1] = _history[0];
		Vector2 playerVelocity = player.Velocity;
		Vector2 position = PositionPrecise;
		if (playerVelocity.X == 0)
		{
			position.X = player.Position.X;
		}
		if (playerVelocity.Y == 0)
		{
			position.Y = player.Position.Y;
		}
		_history[0] = playerVelocity;
		PositionPrecise = position + _velocity;
		_animator.Step();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The lock is a later object with the same priority, so it is behind the sensor
		if (_locked)
		{
			_animation.Index = (_lockFlash ? FrameCount : 0) + LockFrame;
			objectRenderer.RenderOriginal(_animation, (Vector2)(_lockPosition - Position));
		}
		_animation.Index = _animator.Frame;
		objectRenderer.RenderOriginal(_animation);
	}
}
