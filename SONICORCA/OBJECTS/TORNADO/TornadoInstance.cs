using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.TORNADO;

/// <summary>
/// The Tornado, the biplane of Tails, as in Sonic 2 (ObjB2). The subtype selects the behaviour:
/// <list type="bullet">
/// <item>$50 (Sky Chase Zone): the plane flies with the camera and carries the player on its top. It follows the
/// player: it stays within 16 pixels of a player who walks on it, and moves under a player in the air by 1 pixel each
/// frame. Up and down move it 1 pixel each frame between $34 and $A8 pixels from the top of the screen. It dips when
/// the player lands on it or jumps off it. When the camera stops at $1400, the player runs to the right, and the act
/// ends when the player reaches $1568.</item>
/// <item>$52 (start of Wing Fortress Zone): the plane flies right at 1 pixel each frame for $C0 frames and keeps the
/// player within 16 pixels of its centre. Then it is shot down: it goes down and to the right for $60 frames, drops
/// the player, and falls. The smoke of the original (ObjC3) is not drawn.</item>
/// <item>$54 (end of Wing Fortress Zone): the plane takes the player from the fortress to Eggman's getaway ship. See
/// TornadoInstance.WingFortressEnd.cs. The parts that the original makes as other subtypes of ObjB2 are part of
/// this object or other objects: the grip under the ship ($56), the fire at the ship ($58, a fixed
/// SONICORCA/OBJECTS/WFZTHRUST) and the rocket under the plane ($5C).</item>
/// </list>
/// Tails flies the plane. When the player is Tails alone, Sonic flies it in the original, but the art of the Sonic
/// pilot is not imported, so the seat is empty.
/// </summary>
public partial class TornadoInstance : Platform
{
	private const int Scale = 4;

	/// <summary>Speeds in 1/256 original pixels for each frame, at four times the resolution.</summary>
	private const double Unit = Scale / 256.0;

	private const int SkyChaseSubtype = 0x50;

	private const int WingFortressStartSubtype = 0x52;

	/// <summary>The solid top: $1B pixels to each side, 9 pixels above the centre (SolidObject).</summary>
	private const int HalfWidth = 0x1B * Scale;

	private const int Top = -9 * Scale;

	/// <summary>The plane keeps a walking player within $10 pixels of its centre.</summary>
	private const int FollowDistance = 0x10;

	/// <summary>A player faster than $900 runs off the plane.</summary>
	private const int FollowMaxSpeed = 0x900;

	/// <summary>The speed of the plane when the player presses up or down.</summary>
	private const int SteerSpeed = 0x80;

	/// <summary>The dip when the player lands on the plane, and when the player jumps off it.</summary>
	private const int DipSpeed = 0x200;

	private const int DipDeceleration = 0x20;

	private const int DipMaxRise = -0x100;

	private const int LandDipTicks = 0x14;

	private const int LeaveDipTicks = 0x2B;

	/// <summary>The plane stays between these distances from the top of the original screen (ObjB2_Vertical_limit).</summary>
	private const int ScreenTop = 0x34;

	private const int ScreenBottom = 0xA8;

	/// <summary>At this camera X, the player runs to the right (ObjB2_Main_SCZ).</summary>
	private const int EndCameraX = 0x1400;

	/// <summary>The act ends when the player reaches this X.</summary>
	private const int EndPlayerX = 0x1568;

	private const int WingFortressFlyTicks = 0xC0;

	private const int WingFortressShotDownTicks = 0x60;

	/// <summary>Each frame of the pilot shows for 9 frames.</summary>
	private const int PilotFrameTicks = 9;

	/// <summary>The pilot frames of Tails (Tails_pilot_frames), as indexes into the pilot sprites.</summary>
	private static readonly int[] TailsPilotFrames = { 0, 0, 0, 0, 1, 2, 3, 2, 1, 1, 0, 0, 0, 0, 1, 2, 3, 2, 1, 1, 4, 4, 1, 1 };

	private static readonly string[] PilotResourceKeys =
	{
		TornadoType.Pilot0AnimationGroupResourceKey,
		TornadoType.Pilot1AnimationGroupResourceKey,
		TornadoType.Pilot2AnimationGroupResourceKey,
		TornadoType.Pilot3AnimationGroupResourceKey,
		TornadoType.Pilot4AnimationGroupResourceKey
	};

	private AnimationInstance _animation;

	private AnimationInstance[] _pilots;

	private SkyChaseScroll _scroll;

	private int _subtype = SkyChaseSubtype;

	private int _ticks;

	private bool _tailsAlone;

	private bool _standing;

	private int _velocityY;

	private bool _landDip;

	private bool _leaveDip;

	private int _dipTimer;

	/// <summary>The x distance from the player in the air to the plane, in original pixels (objoff_38).</summary>
	private int _followOffset;

	/// <summary>The part of this frame's movement that does not carry the player: the jump to the player.</summary>
	private double _followMove;

	private WingFortressState _wingFortressState;

	private int _timer;

	private enum WingFortressState
	{
		Fly,
		ShotDown,
		Fall
	}

	/// <summary>The subtype of the original. See the summary of the class.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public TornadoInstance() => DesignBounds = new Rectanglei(-0x60 * Scale, -0x10 * Scale, 0x78 * Scale, 0x40 * Scale);

	private bool Supported => _subtype == SkyChaseSubtype || _subtype == WingFortressStartSubtype || _subtype == WingFortressEndSubtype;

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(TornadoType.AnimationGroupResourceKey));
		_pilots = new AnimationInstance[PilotResourceKeys.Length];
		for (int i = 0; i < PilotResourceKeys.Length; i++)
		{
			_pilots[i] = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(PilotResourceKeys[i]));
		}
		_scroll = SkyChaseScroll.For(Level);
		_tailsAlone = Level.Player.ProtagonistCharacterType == CharacterType.Tails;
		_timer = WingFortressFlyTicks;
		if (!Supported)
		{
			CollisionVectors = new CollisionVector[0];
			return;
		}
		// The player stands on the plane for the whole act
		LockLifetime = true;
		if (_subtype == WingFortressEndSubtype)
		{
			StartWingFortressEnd();
			return;
		}
		SetSolid(true);
	}

	/// <summary>The solid top of the plane (SolidObject), or no collision.</summary>
	private void SetSolid(bool solid)
	{
		CollisionVectors = solid
			? new[] { new CollisionVector(this, new Vector2i(-HalfWidth, Top), new Vector2i(HalfWidth, Top), flags: CollisionFlags.Conveyor) }
			: new CollisionVector[0];
		RegisterCollisionUpdate();
	}

	protected override void OnUpdatePrepare()
	{
		base.OnUpdatePrepare();
		// The player moves with the plane, but not with its jump to the player
		_velocityBasedOnNextPosition -= new Vector2(_followMove, 0.0);
	}

	protected override void UpdateMovement()
	{
		_followMove = 0.0;
		if (!Supported)
		{
			return;
		}
		_ticks++;
		ICharacter player = Level.Player.Protagonist;
		if (player == null || player.IsDebug)
		{
			return;
		}
		if (_subtype == SkyChaseSubtype)
		{
			UpdateSkyChase(player);
		}
		else if (_subtype == WingFortressStartSubtype)
		{
			UpdateWingFortressStart(player);
		}
		else
		{
			UpdateWingFortressEnd(player);
		}
	}

	/// <summary>ObjB2_Main_SCZ.</summary>
	private void UpdateSkyChase(ICharacter player)
	{
		// The plane takes the sprite priority of the player, in front of or behind the high tiles
		Layer = player.Layer;
		bool onObject = !player.IsAirborne && player.ObjectLink != null;
		bool standing = onObject && player.ObjectLink == this;
		bool changed = standing != _standing;
		_standing = standing;
		double x = PositionPrecise.X;
		double y = PositionPrecise.Y;
		if (onObject)
		{
			// ObjB2_Move_obbey_player: up and down move the plane, and the plane follows the player
			if (!_landDip)
			{
				_velocityY = Math.Sign(player.Input.VerticalDirection) * SteerSpeed;
				y = MoveVertically(y);
			}
			double dx = (x - player.Position.X) / Scale;
			if (Math.Abs(dx) >= FollowDistance && Math.Abs(player.GroundVelocity) < FollowMaxSpeed * Unit)
			{
				double followX = player.Position.X + (dx >= 0.0 ? FollowDistance : -FollowDistance) * Scale;
				_followMove = followX - x;
				x = followX;
			}
			// ObjB2_Move_with_player and ObjB2_Move_vert: the plane dips when the player lands on it
			if (!_landDip && changed && standing)
			{
				_landDip = true;
				_velocityY = DipSpeed;
				_dipTimer = LandDipTicks;
			}
			if (_landDip)
			{
				UpdateDip(ref _landDip);
			}
			y = MoveVertically(y);
		}
		else
		{
			// ObjB2_Move_vert2: the plane dips when the player jumps off it
			if (!_leaveDip && changed)
			{
				_leaveDip = true;
				_landDip = false;
				_velocityY = DipSpeed;
				_dipTimer = LeaveDipTicks;
			}
			if (_leaveDip)
			{
				UpdateDip(ref _leaveDip);
				y = MoveVertically(y);
			}
			// ObjB2_Move_below_player: the plane moves under the player in the air
			if (changed)
			{
				_followOffset = (int)Math.Round((x - player.Position.X) / Scale);
			}
			_followOffset -= Math.Sign(_followOffset);
			x = player.Position.X + _followOffset * Scale;
		}
		PositionPrecise = new Vector2(x, y) + _scroll.Velocity;
		if (_scroll.Active && _scroll.CameraX >= EndCameraX)
		{
			// The camera has stopped: the player runs to the right until the end of the act
			if (_scroll.ToOriginalX(player.Position.X) >= EndPlayerX)
			{
				_scroll.Finished = true;
			}
			else
			{
				player.Input.Throttle = 1.0;
				player.Input.VerticalDirection = 0;
				player.Input.A = CharacterInputButtonState.Up;
				player.Input.B = CharacterInputButtonState.Up;
				player.Input.C = CharacterInputButtonState.Up;
			}
		}
	}

	/// <summary>One frame of a dip: the plane goes down at $200, then rises, up to $100, until the timer ends.</summary>
	private void UpdateDip(ref bool dipping)
	{
		if (--_dipTimer < 0)
		{
			dipping = false;
			_velocityY = 0;
			return;
		}
		if (_velocityY > DipMaxRise)
		{
			_velocityY -= DipDeceleration;
		}
	}

	/// <summary>ObjB2_Vertical_limit and ObjectMove: the plane stays in its part of the screen.</summary>
	private double MoveVertically(double y)
	{
		if (_scroll.Active)
		{
			int top = _scroll.ToLevelY(_scroll.CameraY + ScreenTop);
			int bottom = _scroll.ToLevelY(_scroll.CameraY + ScreenBottom);
			if ((_velocityY < 0 && y < top) || (_velocityY > 0 && y >= bottom))
			{
				_velocityY = 0;
			}
		}
		return y + _velocityY * Unit;
	}

	/// <summary>ObjB2_Main_WFZ_Start.</summary>
	private void UpdateWingFortressStart(ICharacter player)
	{
		switch (_wingFortressState)
		{
			case WingFortressState.Fly:
				if (--_timer < 0)
				{
					_wingFortressState = WingFortressState.ShotDown;
					_timer = WingFortressShotDownTicks;
					break;
				}
				PositionPrecise += new Vector2(Scale, 0.0);
				KeepPlayerNear(player);
				break;
			case WingFortressState.ShotDown:
				if (--_timer < 0)
				{
					// The plane drops the player and falls
					_wingFortressState = WingFortressState.Fall;
					if (player.ObjectLink == this)
					{
						player.LeaveGround();
						player.IsAirborne = true;
					}
					break;
				}
				PositionPrecise += new Vector2(Scale, Scale);
				break;
			case WingFortressState.Fall:
				PositionPrecise += new Vector2(Scale * 2, Scale * 2);
				if (Position.Y > Level.Camera.Bounds.Bottom + 0x40 * Scale)
				{
					FinishForever();
				}
				break;
		}
	}

	/// <summary>ObjB2_Horizontal_limit: the player stays within 16 pixels of the centre of the plane.</summary>
	private void KeepPlayerNear(ICharacter player)
	{
		double dx = (PositionPrecise.X - player.Position.X) / Scale;
		if (Math.Abs(dx) >= FollowDistance)
		{
			double x = PositionPrecise.X - (dx >= 0.0 ? FollowDistance : -FollowDistance) * Scale;
			player.PositionPrecise = new Vector2(x, player.PositionPrecise.Y);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!Supported)
		{
			return;
		}
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (_subtype == WingFortressEndSubtype)
		{
			DrawWingFortressEnd(objectRenderer);
			return;
		}
		DrawPlane(objectRenderer);
	}

	private void DrawPlane(IObjectRenderer objectRenderer)
	{
		// The propeller turns: frames 0 to 3, one frame each (frames 4 to 7 have the seat of the Sonic pilot)
		_animation.Index = (_tailsAlone ? 4 : 0) + _ticks % 4;
		objectRenderer.RenderOriginal(_animation);
		if (!_tailsAlone)
		{
			AnimationInstance pilot = _pilots[TailsPilotFrames[(_ticks / PilotFrameTicks) % TailsPilotFrames.Length]];
			pilot.Index = 0;
			objectRenderer.RenderOriginal(pilot);
		}
	}
}
