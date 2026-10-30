using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SONICORCA.OBJECTS.DEATHEGGROBOT.SENSOR;
using SONICORCA.OBJECTS.DEZEGGMAN;

namespace SONICORCA.OBJECTS.DEATHEGGROBOT;

/// <summary>
/// The Death Egg Robot, the final boss of Sonic 2 (ObjC7, Eggrobo). The robot is a body and ten parts: the shoulder,
/// the arm, the two forearms, the two thighs, the two lower legs, the head and the jet. It waits below the floor. When
/// Eggman jumps in (<see cref="DEZEggmanInstance.Boarded"/>), the head shows him, the music stops, and 60 frames later
/// the robot rises $78 pixels with the final boss music. Then it takes 12 hits on the body or the head, and the other
/// parts hurt. In turn it walks forward, jumps, punches and jumps again:
/// <list type="bullet">
/// <item>It walks forward with heavy steps, then steps back.</item>
/// <item>It jumps out of the screen. A targeting sensor follows the player with a delay of 4 frames for 160 frames, and
/// locks on for 64 frames. The robot comes down on that x, with the screen shaking.</item>
/// <item>It leans forward and fires the front forearm, then the back forearm, at the player. A forearm drops a little,
/// flies $100 pixels out and comes back.</item>
/// </list>
/// When the player is behind it after a jump or a lean, it throws two bombs back. After the last hit the parts fly
/// off, the body falls and explodes, and the player runs to the right. In the original, the ending sequence starts
/// when the player reaches x $EC0. Here the stage completes there: the area makes Death Egg Zone the last act, so the
/// game shows the credits after it, as after the last level of the HD game.
/// </summary>
public class DeathEggRobotInstance : BossObject
{
	private const int Scale = 4;

	/// <summary>The position from the object layout, in original level pixels.</summary>
	public const int InitX = 0x840;

	public const int InitY = 0x19C;

	private const int OriginalScreenWidth = 320;

	private const int Health12 = 12;

	private const int FlashTicks = 60;

	private const int Gravity = 0x38;

	private const int DefeatScore = 1000;

	private const int MusicFadeTicks = 60;

	/// <summary>The robot lands facing right when the target is left of this x.</summary>
	private const int MiddleX = 0x780;

	/// <summary>The bombs stop on the floor at this y.</summary>
	private const int BombFloorY = 0x170;

	/// <summary>The defeated body falls to this y.</summary>
	private const int WreckFloorY = 0x15C;

	/// <summary>After the explosions, the player runs right. The ending starts at these x values.</summary>
	private const int EndingCameraX = 0x840;

	private const int EndingPlayerX = 0xEC0;

	/// <summary>The frames of the fade to white at the end (anim_frame_duration $16 in loc_3D9BE).</summary>
	private const int EndingFadeFrames = 0x16;

	/// <summary>The right camera limit after the robot is defeated (Camera_Max_X_pos).</summary>
	public const int EscapeCameraMaxX = 0x1000;

	private const int FirstBombId = 100;

	/// <summary>byte_3D680: the actions in turn. 0 walks, 2 jumps, 4 punches.</summary>
	private static readonly int[] Actions = { 2, 0, 2, 4 };

	/// <summary>EggmanBomb_InitSpeeds.</summary>
	private static readonly Vector2[] BombSpeeds = { new Vector2(0x60, -0x800), new Vector2(0xC0, -0xA00) };

	/// <summary>ObjC7_BreakOffsets and ObjC7_BreakSpeeds: the parts that fly off after the last hit.</summary>
	private static readonly (Part Part, Vector2 Velocity)[] BreakSpeeds =
	{
		(Part.Shoulder, new Vector2(0x200, -0x400)),
		(Part.FrontLowerLeg, new Vector2(-0x100, -0x100)),
		(Part.FrontForearm, new Vector2(0x300, -0x300)),
		(Part.Arm, new Vector2(-0x100, -0x400)),
		(Part.FrontThigh, new Vector2(0x180, -0x200)),
		(Part.BackLowerLeg, new Vector2(-0x200, -0x300)),
		(Part.BackForearm, new Vector2(0, -0x400)),
		(Part.BackThigh, new Vector2(0x100, -0x300)),
	};

	/// <summary>ObjC7_ChildDeltas: the positions of the arms and the legs from the body.</summary>
	private static readonly (Part Part, int X, int Y)[] ChildDeltas =
	{
		(Part.FrontLowerLeg, -4, 0x3C),
		(Part.FrontForearm, -0xC, 8),
		(Part.Arm, 0xC, -8),
		(Part.FrontThigh, 4, 0x24),
		(Part.BackLowerLeg, -4, 0x3C),
		(Part.BackForearm, -0xC, 8),
		(Part.BackThigh, 4, 0x24),
	};

	/// <summary>
	/// The touch areas of the parts (ObjC7_ChildCollision), as half sizes in original pixels. The body ($16) and the
	/// head ($2A) can be hit. The other parts hurt.
	/// </summary>
	private static readonly (Part Part, int HalfWidth, int HalfHeight, bool Hurts)[] PartTouch =
	{
		(Part.Body, 0x20, 0x20, false),
		(Part.Head, 0x20, 0x10, false),
		(Part.FrontLowerLeg, 0x18, 0x18, true),
		(Part.FrontForearm, 0x18, 4, true),
		(Part.FrontThigh, 0x10, 0x10, true),
		(Part.Jet, 8, 8, true),
		(Part.BackLowerLeg, 0x18, 0x18, true),
		(Part.BackForearm, 0x18, 4, true),
		(Part.BackThigh, 8, 8, true),
	};

	/// <summary>The drawing order, from the back: the parts with priority 5, then the parts with priority 4.</summary>
	private static readonly Part[] DrawOrder =
	{
		Part.BackThigh, Part.BackForearm, Part.BackLowerLeg, Part.Body,
		Part.Jet, Part.Head, Part.FrontThigh, Part.Arm, Part.FrontLowerLeg, Part.FrontForearm, Part.Shoulder
	};

	// The group animations (ObjC7_GroupAni_3E318, 3E3D8 and 3E438): each frame moves some parts by 1/16 pixel per
	// delta on each tick, for a number of ticks
	private static readonly GroupFrame[] LeanFrames =
	{
		Lean(8, -0x20, 0xC, -8, 4),
		Lean(8, -0x14, 0x14, -6, 6),
		Lean(8, -8, 0x14, -2, 4),
		Lean(8, -4, 0xC, 0, 2),
		new GroupFrame(8, new[] { new Delta(Part.Body, 0, 0) }),
		Lean(8, 4, -0x18, 2, -6),
		Lean(8, 0xC, -0x18, 4, -4),
		Lean(8, 0x18, -0xC, 4, -4),
		Lean(8, 0x18, -4, 6, -2),
	};

	private static readonly GroupFrame[] CrouchFrames =
	{
		Lean(0x10, 0, 4, 0, 4),
		new GroupFrame(0x10, new[] { new Delta(Part.Body, 0, 0) }),
		Lean(8, 0, -8, 0, -8),
	};

	private static readonly GroupFrame[] WalkFrames =
	{
		Walk(0x20, (Part.FrontThigh, -8, -8), (Part.FrontLowerLeg, -8, -8), (Part.Body, 0, -4), (Part.FrontForearm, 4, -5),
			(Part.Arm, 3, -5), (Part.BackForearm, -4, -5), (Part.BackThigh, 0, -2)),
		Walk(0x10, (Part.FrontThigh, -0x10, -4), (Part.FrontLowerLeg, -0x10, -4), (Part.Body, -0x10, -4), (Part.FrontForearm, -0xC, -5),
			(Part.Arm, -0xD, -5), (Part.BackForearm, -0x14, -5), (Part.BackThigh, -8, 0)),
		Walk(0x10, (Part.FrontThigh, -8, 4), (Part.FrontLowerLeg, -8, 4), (Part.Body, -8, 4), (Part.FrontForearm, -4, 3),
			(Part.Arm, -5, 3), (Part.BackForearm, -0xC, 3)),
		Walk(0x10, (Part.FrontThigh, -4, 0x10), (Part.FrontLowerLeg, -8, 0x10), (Part.Body, 0, 8), (Part.FrontForearm, -8, 0xA),
			(Part.Arm, -6, 0xA), (Part.BackForearm, 8, 0xA), (Part.BackThigh, 0, 8)),
		Walk(0x20, (Part.FrontThigh, -2, -2), (Part.Body, -0xC, -4), (Part.FrontForearm, -0x10, -3), (Part.Arm, -0xF, -3),
			(Part.BackForearm, -8, -3), (Part.BackThigh, -0x14, -6), (Part.BackLowerLeg, -0x18, -4)),
		Walk(0x20, (Part.BackThigh, -8, -4), (Part.BackLowerLeg, -8, -4), (Part.FrontForearm, -4, -1), (Part.Arm, -3, -1),
			(Part.BackForearm, 4, -1)),
		Walk(0x10, (Part.BackThigh, -0x10, -4), (Part.BackLowerLeg, -0x10, -4), (Part.Body, -0x10, -4), (Part.FrontForearm, -0x14, -5),
			(Part.Arm, -0x13, -5), (Part.BackForearm, -0xC, -5), (Part.FrontThigh, -8, 0)),
		Walk(0x10, (Part.BackThigh, -8, 4), (Part.BackLowerLeg, -8, 4), (Part.Body, -8, 4), (Part.FrontForearm, -0xC, 3),
			(Part.Arm, -0xB, 3), (Part.BackForearm, -4, 3)),
		Walk(0x10, (Part.BackThigh, -4, 0x10), (Part.BackLowerLeg, -8, 0x10), (Part.Body, 0, 8), (Part.FrontForearm, 8, 0xA),
			(Part.Arm, 6, 0xA), (Part.BackForearm, -8, 0xA), (Part.FrontThigh, 0, 8)),
		Walk(0x20, (Part.BackThigh, -2, -2), (Part.Body, -0xC, -4), (Part.FrontForearm, -8, -3), (Part.Arm, -9, -3),
			(Part.BackForearm, -0xF, -3), (Part.FrontThigh, -0x14, -6), (Part.FrontLowerLeg, -0x18, -4)),
		Walk(0x20, (Part.FrontThigh, -8, -4), (Part.FrontLowerLeg, -8, -4), (Part.FrontForearm, 4, -1), (Part.Arm, 3, -1),
			(Part.BackForearm, -4, -1)),
		Walk(0x10, (Part.BackThigh, 0, 8), (Part.BackLowerLeg, 0, 8), (Part.Body, 0, 8), (Part.FrontForearm, 0, 8),
			(Part.Arm, 0, 8), (Part.BackForearm, 0, 8), (Part.FrontThigh, 0, 8)),
	};

	// The scripts of the group animations: a frame, $80 + frame to play a frame backwards, $40 and a sound (SndID_Hammer,
	// which the game data does not have), or $C0 (or $FF) at the end
	private static readonly GroupScript LeanForward = new GroupScript(LeanFrames, new byte[] { 0, 1, 2, 3, 0xFF });

	private static readonly GroupScript LeanBack = new GroupScript(LeanFrames, new byte[] { 5, 6, 7, 8, 0xFF });

	private static readonly GroupScript Land = new GroupScript(LeanFrames, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 0xC0 });

	private static readonly GroupScript Crouch = new GroupScript(CrouchFrames, new byte[] { 0, 1, 2, 0xC0 });

	private static readonly GroupScript WalkForward = new GroupScript(WalkFrames, new byte[]
	{
		0, 1, 2, 3, 0x40, 0, 4, 5, 6, 7, 8, 0x40, 0, 9, 0xA, 1, 2, 3, 0x40, 0, 4, 5, 6, 7, 8, 0x40, 0, 0xC0
	});

	private static readonly GroupScript StepBack = new GroupScript(WalkFrames, new byte[] { 0x88, 0x87, 0x86, 0x85, 0xB, 0x40, 0, 0xC0 });

	/// <summary>Ani_objC7_a: the head shows Eggman when he jumps in.</summary>
	private readonly AnimateSpriteScript _headAnimator = new AnimateSpriteScript(new byte[] { 7, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0, 1, 2, 0xFA });

	/// <summary>Ani_objC7_b: the flame of the jet.</summary>
	private readonly AnimateSpriteScript _jetAnimator = new AnimateSpriteScript(
		new byte[] { 1, 0xC, 0xD, 0xFF },
		new byte[] { 1, 0xC, 0xD, 0xC, 0xC, 0xD, 0xD, 0xC, 0xC, 0xC, 0xD, 0xD, 0xD, 0xC, 0xC, 0xC, 0xC, 0xC, 0xD, 0xD, 0xD, 0xD, 0xD, 0xD, 0xFA, 0 },
		new byte[] { 1, 0xD, 0xD, 0xD, 0xD, 0xD, 0xD, 0xC, 0xC, 0xC, 0xC, 0xC, 0xD, 0xD, 0xD, 0xC, 0xC, 0xC, 0xD, 0xD, 0xC, 0xC, 0xD, 0xC, 0xFD, 0 },
		new byte[] { 0, 0xD, 0x15, 0xFF });

	private readonly PartState[] _parts = Enumerable.Range(0, 11).Select(_ => new PartState()).ToArray();

	private readonly List<Bomb> _bombs = new List<Bomb>();

	private AnimationInstance _animation;

	private BodyState _state;

	/// <summary>anim: the action (0, 2 or 4), and prev_anim: the step of the action. In the defeat, anim is the step.</summary>
	private int _action;

	private int _step;

	/// <summary>anim_frame_duration of the body.</summary>
	private int _timer;

	/// <summary>angle: counts the actions.</summary>
	private int _actionCount;

	/// <summary>anim_frame and objoff_1F: the position in the script of a group animation.</summary>
	private int _groupIndex;

	private int _groupTick;

	/// <summary>render_flags.x_flip of the body and all parts: the robot faces right. The art faces left.</summary>
	private bool _facingRight;

	/// <summary>The parts touch (ObjC7_InitCollision), until the last hit (ObjC7_RemoveCollision).</summary>
	private bool _touching;

	/// <summary>status.npc.misc: the head showed Eggman. The robot starts.</summary>
	private bool _started;

	/// <summary>status.npc.p2_standing and p1_pushing: the front and the back forearm fly.</summary>
	private bool _launchFront;

	private bool _launchBack;

	private HeadState _headState;

	private int _headTimer;

	private int _jetState;

	private bool _jetVisible = true;

	private DeathEggRobotSensorInstance _sensor;

	/// <summary>The x of the target, in original level pixels (objoff_28), or null.</summary>
	private double? _targetX;

	private int _shakeTimer;

	/// <summary>objoff_2A in the ending: the explosions are this far to the left of the player.</summary>
	private int _endingOffset;

	private bool _fadingOut;

	private enum Part
	{
		Body,
		Shoulder,
		FrontLowerLeg,
		FrontForearm,
		Arm,
		FrontThigh,
		Head,
		Jet,
		BackLowerLeg,
		BackForearm,
		BackThigh
	}

	private enum BodyState
	{
		WaitForEggman,
		WaitForMusic,
		Rise,
		Ready,
		ChooseAction,
		Action,
		Defeated,
		Ending
	}

	private enum HeadState
	{
		WaitForEggman,
		ShowEggman,
		Wait,
		Done
	}

	private readonly record struct Delta(Part Part, int X, int Y);

	private sealed record GroupFrame(int Ticks, Delta[] Deltas);

	private sealed record GroupScript(GroupFrame[] Frames, byte[] Sequence);

	private sealed class PartState
	{
		/// <summary>The position in original level pixels, and the velocity in 1/256 pixel per frame.</summary>
		public Vector2 Position;

		public Vector2 Velocity;

		public int Frame;

		public bool Visible = true;

		/// <summary>The part flies off after the last hit (routine $1E).</summary>
		public bool Falling;

		public int Timer;

		/// <summary>routine_secondary of a forearm, and the y where it was before it flew (objoff_2E).</summary>
		public int ForearmState;

		public double SavedY;
	}

	private sealed class Bomb
	{
		public Vector2 Position;

		public Vector2 Velocity;

		public int State;

		public int Timer;

		/// <summary>The ticks from the explosion: the explosion hurts for 40 ticks and goes after 56.</summary>
		public int ExplosionTicks = -1;
	}

	/// <summary>The x of the original level origin, in HD units. Death Egg Zone starts at the origin of the map.</summary>
	[StateVariable]
	public int OriginX { get; set; }

	/// <summary>The y of the original level origin, in HD units.</summary>
	[StateVariable]
	public int OriginY { get; set; }

	/// <summary>The robot is defeated and the player runs right: the camera can go right to x $1000.</summary>
	[HideInEditor]
	public bool Escaping { get; private set; }

	public DeathEggRobotInstance() => DesignBounds = new Rectanglei(-4096, -4096, 8192, 8192);

	private PartState Body => _parts[(int)Part.Body];

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(DeathEggRobotType.AnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(DeathEggRobotType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(DeathEggRobotType.BossHitSoundResourceKey);
		LockLifetime = true;
		Health = Health12;
		// loc_3D52A: the parts start at the body, then the arms and the legs move to their places
		Vector2 position = new Vector2((double)(Position.X - OriginX) / Scale, (double)(Position.Y - OriginY) / Scale);
		int[] frames = { 3, 4, 0xB, 6, 5, 0xA, 0x15, 0xC, 0xB, 6, 0xA };
		for (int i = 0; i < _parts.Length; i++)
		{
			_parts[i].Position = position;
			_parts[i].Frame = frames[i];
		}
		PositionChildren();
		UpdateAttachedParts();
	}

	protected override void OnStop()
	{
		if (Level != null)
		{
			Level.EarthquakeActive = false;
		}
		FinishForever();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdateBody();
		UpdateForearm(_parts[(int)Part.FrontForearm], ref _launchFront);
		UpdateForearm(_parts[(int)Part.BackForearm], ref _launchBack);
		UpdateHead();
		UpdateJet();
		UpdateFallingParts();
		UpdateAttachedParts();
		UpdateBombs();
		UpdateShake();
		PositionPrecise = ToLevel(Body.Position);
		UpdateTouchAreas();
	}

	private void UpdateBody()
	{
		switch (_state)
		{
			case BodyState.WaitForEggman:
				if (_started)
				{
					_state = BodyState.WaitForMusic;
					_timer = 60;
					Level.SoundManager.FadeOutMusic(MusicFadeTicks);
				}
				break;
			case BodyState.WaitForMusic:
				if (--_timer < 0)
				{
					_state = BodyState.Rise;
					_timer = 0x79;
					Body.Velocity = new Vector2(0, -0x100);
					_jetState = 4;
					PlayMusic(Type.GetAbsolutePath(DeathEggRobotType.BossMusicResourceKey));
				}
				break;
			case BodyState.Rise:
				if (--_timer != 0)
				{
					MoveBody();
					PositionChildren();
					break;
				}
				// The robot stands: from now the parts touch
				_state = BodyState.Ready;
				Body.Velocity = default;
				_timer = 0x1F;
				_touching = true;
				_jetState = 6;
				break;
			case BodyState.Ready:
				if (--_timer < 0)
				{
					_state = BodyState.ChooseAction;
				}
				break;
			case BodyState.ChooseAction:
				ChooseAction();
				break;
			case BodyState.Action:
				switch (_action)
				{
					case 0:
						UpdateWalk();
						break;
					case 2:
						UpdateJump();
						break;
					case 4:
						UpdatePunch();
						break;
				}
				break;
			case BodyState.Defeated:
				UpdateDefeated();
				break;
			case BodyState.Ending:
				UpdateEnding();
				break;
		}
	}

	/// <summary>loc_3D640: the next action, after $20 frames.</summary>
	private void ChooseAction()
	{
		_state = BodyState.Action;
		_timer = 0x20;
		_actionCount++;
		_action = Actions[_actionCount & 3];
		_step = 0;
		if (_action == 2)
		{
			_jetState = 4;
			_jetAnimator.Animation = 2;
		}
	}

	/// <summary>loc_3D6AA: wait, walk forward, wait, step back.</summary>
	private void UpdateWalk()
	{
		switch (_step)
		{
			case 0:
			case 4:
				Wait();
				break;
			case 2:
				if (RunGroup(WalkForward))
				{
					_step += 2;
					_timer = 0x40;
				}
				break;
			case 6:
				if (RunGroup(StepBack))
				{
					_state = BodyState.ChooseAction;
					_timer = 0x40;
				}
				break;
		}
	}

	/// <summary>loc_3D702: wait, crouch, fly up, follow the sensor, come down on the target, and land.</summary>
	private void UpdateJump()
	{
		switch (_step)
		{
			case 0:
			case 6:
				Wait();
				break;
			case 2:
				if (RunGroup(Crouch))
				{
					_step += 2;
					_timer = 0x80;
					Body.Velocity = new Vector2(0, -0x200);
				}
				break;
			case 4:
				if (--_timer >= 0)
				{
					MoveBody();
					PositionChildren();
					break;
				}
				_step += 2;
				Body.Velocity = default;
				_targetX = null;
				_sensor = Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(DeathEggRobotType.SensorResourceKey),
					Level.Map.Layers.IndexOf(Layer), Level.Player.Protagonist.Position, new { })) as DeathEggRobotSensorInstance;
				break;
			case 8:
				if (_sensor == null)
				{
					// Without the sensor, the robot comes down where it went up
					_targetX = Body.Position.X;
				}
				else if (_sensor.TargetX is int targetX)
				{
					_targetX = (double)(targetX - OriginX) / Scale;
				}
				if (_targetX is not double x)
				{
					break;
				}
				_step += 2;
				_sensor = null;
				Body.Position.X = x;
				_facingRight = x < MiddleX;
				Body.Velocity = new Vector2(0, 0x800);
				_timer = 0x20;
				break;
			case 0xA:
				if (--_timer >= 0)
				{
					MoveBody();
					PositionChildren();
					break;
				}
				// The landing shakes the screen
				_step += 2;
				Body.Velocity = default;
				_shakeTimer = 0x40;
				_jetState = 6;
				break;
			case 0xC:
				if (!RunGroup(Land))
				{
					break;
				}
				PositionChildren();
				if (IsPlayerInFront())
				{
					_state = BodyState.ChooseAction;
					break;
				}
				_step += 2;
				_timer = 0x60;
				CreateBombs();
				break;
			case 0xE:
				if (--_timer < 0)
				{
					_state = BodyState.ChooseAction;
				}
				break;
		}
	}

	/// <summary>loc_3D83C: wait, lean forward, fire the front and then the back forearm, and lean back.</summary>
	private void UpdatePunch()
	{
		switch (_step)
		{
			case 0:
			case 4:
			case 8:
				Wait();
				break;
			case 2:
				if (!RunGroup(LeanForward))
				{
					break;
				}
				if (IsPlayerInFront())
				{
					_step += 2;
					_timer = 0x40;
					_launchFront = true;
				}
				else
				{
					_step = 8;
					_timer = 0x20;
					CreateBombs();
				}
				break;
			case 6:
				Wait();
				if (_step == 8)
				{
					_launchBack = true;
					_timer = 0x40;
				}
				break;
			case 0xA:
				if (RunGroup(LeanBack))
				{
					_state = BodyState.ChooseAction;
				}
				break;
		}
	}

	/// <summary>loc_3D6C0: wait anim_frame_duration frames, then go to the next step.</summary>
	private void Wait()
	{
		if (--_timer < 0)
		{
			_step += 2;
		}
	}

	/// <summary>Routine $E: the body falls to y $15C and bounces, explodes, and then the player runs right.</summary>
	private void UpdateDefeated()
	{
		switch (_step)
		{
			case 0:
				LoadExplosion(0x20);
				Body.Position += Body.Velocity / 256.0;
				Body.Velocity.Y += Gravity;
				if (Body.Position.Y < WreckFloorY)
				{
					break;
				}
				Body.Position.Y = WreckFloorY;
				if (Body.Velocity.Y >= 0 && (int)Body.Velocity.Y >> 2 >= 0x100)
				{
					Body.Velocity.Y = -((int)Body.Velocity.Y >> 2);
					break;
				}
				_step = 2;
				_timer = 0x40;
				break;
			case 2:
				if (--_timer >= 0)
				{
					LoadExplosion(0x20);
					break;
				}
				_step = 4;
				Escaping = true;
				break;
			case 4:
				ForceRight();
				if (GetCameraX() < EndingCameraX)
				{
					break;
				}
				_state = BodyState.Ending;
				_step = 0;
				_endingOffset = 0x20;
				_shakeTimer = int.MaxValue;
				_parts[(int)Part.Head].Visible = false;
				break;
		}
	}

	/// <summary>
	/// ObjC7_SetupEnding: explosions follow the player. When the player reaches x $EC0, the screen fades to white in
	/// $16 frames, and the stage completes. The floor ends at x $F00, so a longer fade lets the player run off the
	/// floor and fall out of the level.
	/// </summary>
	private void UpdateEnding()
	{
		ForceRight();
		if ((Level.Ticks & 0x1F) == 0)
		{
			_endingOffset--;
		}
		ICharacter player = Level.Player.Protagonist;
		if (player == null)
		{
			return;
		}
		// loc_3DFBA: an explosion on each frame, up to $30 pixels away
		Vector2i centre = player.Position - new Vector2i(_endingOffset * Scale, 0);
		ExplodeAt(centre + new Vector2i((Level.Random.Next(0x40) - 0x30) * Scale, (Level.Random.Next(0x40) - 0x30) * Scale));
		if (!_fadingOut && (player.Position.X - OriginX) / Scale >= EndingPlayerX)
		{
			_fadingOut = true;
			Level.FadeOutToWhite(LevelState.StageCompleted, EndingFadeFrames);
		}
	}

	/// <summary>Control_Locked: the player runs right (Ctrl_1_Logical = right).</summary>
	private void ForceRight()
	{
		Level.StateFlags &= ~LevelStateFlags.AllowCharacterControl;
		ICharacter player = Level.Player.Protagonist;
		if (player != null)
		{
			player.Input = new CharacterInputState { HorizontalDirection = 1 };
		}
	}

	/// <summary>The head waits for Eggman, shows him getting in, and starts the robot $40 frames later.</summary>
	private void UpdateHead()
	{
		PartState head = _parts[(int)Part.Head];
		switch (_headState)
		{
			case HeadState.WaitForEggman:
				if (IsEggmanBoarded())
				{
					_headState = HeadState.ShowEggman;
					_headTimer = 0x40;
				}
				break;
			case HeadState.ShowEggman:
				if (_headAnimator.Step() == AnimateSpriteScript.NextSecondaryRoutine)
				{
					_headState = HeadState.Wait;
				}
				head.Frame = _headAnimator.Frame;
				break;
			case HeadState.Wait:
				if (--_headTimer < 0)
				{
					_headState = HeadState.Done;
					_started = true;
				}
				break;
		}
	}

	/// <summary>DEZ_Eggman, status.npc.p1_standing: Eggman jumped into the robot.</summary>
	private bool IsEggmanBoarded() => Level.ObjectManager.ActiveObjects.OfType<DEZEggmanInstance>().Any(eggman => eggman.Boarded);

	/// <summary>ObjC7_Jet: the flame flickers, burns while the robot flies, and flickers again after a landing.</summary>
	private void UpdateJet()
	{
		switch (_jetState)
		{
			case 2:
			case 8:
				_jetAnimator.Animation = 3;
				break;
			case 6:
				_jetAnimator.Animation = 1;
				break;
		}
		if (_jetState == 0)
		{
			// The jet shows from the next frame
			_jetState = 2;
			_jetVisible = false;
			return;
		}
		_jetVisible = true;
		int flag = _jetAnimator.Step();
		if (flag == AnimateSpriteScript.NextSecondaryRoutine)
		{
			_jetState += 2;
		}
		_parts[(int)Part.Jet].Frame = _jetAnimator.Frame;
	}

	/// <summary>loc_3DAAC to loc_3DB5A: a forearm drops, flies out at $800 toward the player, and comes back.</summary>
	private void UpdateForearm(PartState forearm, ref bool launch)
	{
		if (forearm.Falling)
		{
			return;
		}
		switch (forearm.ForearmState)
		{
			case 0:
				if (launch)
				{
					launch = false;
					forearm.ForearmState = 4;
					forearm.Timer = 0x10;
					forearm.SavedY = forearm.Position.Y;
				}
				break;
			case 4:
				if (--forearm.Timer >= 0)
				{
					forearm.Velocity.Y += 0x20;
					forearm.Position += forearm.Velocity / 256.0;
					break;
				}
				forearm.ForearmState = 6;
				forearm.Timer = 0x20;
				// The vertical speed comes from the x distance to the player: $200 when it is near, 0 from $C0 away. The
				// forearm goes up when the player is above it.
				GetOrientationToPlayer(forearm.Position, out double dx, out _, out bool playerAbove);
				int distance = Math.Min((int)Math.Abs(dx), 0xFF);
				int[] speeds = { 0x200, 0x100, 0x80, 0 };
				int speed = speeds[(distance & 0xC0) >> 6];
				forearm.Velocity = new Vector2(_facingRight ? 0x800 : -0x800, playerAbove ? -speed : speed);
				PlaySound(DeathEggRobotType.SpindashSoundResourceKey);
				break;
			case 6:
				if (--forearm.Timer >= 0)
				{
					forearm.Position += forearm.Velocity / 256.0;
					break;
				}
				forearm.ForearmState = 8;
				forearm.Timer = 0x20;
				forearm.Velocity = new Vector2(-forearm.Velocity.X, (forearm.SavedY - forearm.Position.Y) * 8);
				break;
			case 8:
				if (--forearm.Timer >= 0)
				{
					forearm.Position += forearm.Velocity / 256.0;
					break;
				}
				forearm.ForearmState = 0;
				forearm.Velocity = default;
				break;
		}
	}

	/// <summary>Routine $1E: a part that flies off falls for 128 frames, then goes.</summary>
	private void UpdateFallingParts()
	{
		foreach (PartState part in _parts)
		{
			if (!part.Falling || !part.Visible)
			{
				continue;
			}
			if (--part.Timer < 0)
			{
				part.Visible = false;
				continue;
			}
			part.Position += part.Velocity / 256.0;
			part.Velocity.Y += Gravity;
		}
	}

	/// <summary>loc_3E282: the shoulder, the head and the jet follow the body.</summary>
	private void UpdateAttachedParts()
	{
		Attach(Part.Shoulder, 0xC, -0x14);
		Attach(Part.Head, 0, -0x34);
		Attach(Part.Jet, 0x38, 0x18);
	}

	private void Attach(Part part, int x, int y)
	{
		PartState state = _parts[(int)part];
		if (!state.Falling)
		{
			state.Position = Body.Position + new Vector2(_facingRight ? -x : x, y);
		}
	}

	/// <summary>ObjC7_PositionChildren with ObjC7_ChildDeltas.</summary>
	private void PositionChildren()
	{
		foreach ((Part part, int x, int y) in ChildDeltas)
		{
			_parts[(int)part].Position = Body.Position + new Vector2(_facingRight ? -x : x, y);
		}
	}

	/// <summary>
	/// loc_3E1AA: one tick of a group animation. Returns true at the end of the script, and the script starts again
	/// from the first frame the next time.
	/// </summary>
	private bool RunGroup(GroupScript script)
	{
		int value = script.Sequence[_groupIndex];
		bool backwards = false;
		while ((value & 0xC0) != 0)
		{
			if ((value & 0xC0) == 0x40)
			{
				// A sound (SndID_Hammer), then the next frame
				_groupIndex += 2;
				value = script.Sequence[_groupIndex];
				continue;
			}
			if ((value & 0xC0) == 0x80)
			{
				backwards = true;
				value &= 0x3F;
				break;
			}
			_groupIndex = 0;
			return true;
		}
		GroupFrame frame = script.Frames[value];
		int tick = _groupTick + 1;
		if (tick >= frame.Ticks)
		{
			_groupIndex++;
			tick = 0;
		}
		_groupTick = tick;
		double sign = backwards ? -1 : 1;
		foreach (Delta delta in frame.Deltas)
		{
			PartState part = _parts[(int)delta.Part];
			part.Position += new Vector2((_facingRight ? -delta.X : delta.X) * sign / 16.0, delta.Y * sign / 16.0);
		}
		return false;
	}

	/// <summary>CreateEggmanBombs: two bombs fly back over the robot.</summary>
	private void CreateBombs()
	{
		foreach (Vector2 speed in BombSpeeds)
		{
			_bombs.Add(new Bomb
			{
				Position = Body.Position + new Vector2(_facingRight ? -0x38 : 0x38, -0x14),
				Velocity = new Vector2(_facingRight ? -speed.X : speed.X, speed.Y)
			});
		}
	}

	/// <summary>ObjC7_EggmanBomb: a bomb falls to the floor and explodes $40 frames later, or when the robot is defeated.</summary>
	private void UpdateBombs()
	{
		foreach (Bomb bomb in _bombs)
		{
			switch (bomb.State)
			{
				case 0:
					// The bomb moves from the next frame
					bomb.State = 2;
					break;
				case 2:
					if (Defeated)
					{
						Explode(bomb);
						break;
					}
					bomb.Position += bomb.Velocity / 256.0;
					bomb.Velocity.Y += Gravity;
					if (bomb.Position.Y >= BombFloorY)
					{
						bomb.State = 4;
						bomb.Position.Y = BombFloorY;
						bomb.Timer = 0x40;
					}
					break;
				case 4:
					if (Defeated || --bomb.Timer < 0)
					{
						Explode(bomb);
					}
					break;
				case 6:
					bomb.ExplosionTicks++;
					break;
			}
		}
		_bombs.RemoveAll(bomb => bomb.ExplosionTicks >= 56);
	}

	private void Explode(Bomb bomb)
	{
		bomb.State = 6;
		bomb.ExplosionTicks = 0;
		ExplodeAt((Vector2i)ToLevel(bomb.Position));
	}

	/// <summary>Boss_LoadExplosion: an explosion every 8 frames, up to $20 pixels from the body.</summary>
	private void LoadExplosion(int range)
	{
		if ((Level.Ticks & 7) == 0)
		{
			ExplodeAt(Position + new Vector2i((Level.Random.Next(range * 2) - range) * Scale, (Level.Random.Next(range * 2) - range) * Scale));
		}
	}

	/// <summary>DEZ_Shake_Timer: the screen shakes after a landing, and in the ending.</summary>
	private void UpdateShake()
	{
		if (_shakeTimer > 0)
		{
			_shakeTimer--;
			Level.EarthquakeActive = true;
		}
		else if (Level.EarthquakeActive)
		{
			Level.EarthquakeActive = false;
		}
	}

	private void MoveBody() => Body.Position += Body.Velocity / 256.0;

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		// The body and the head flash for 60 frames, not 32
		InvincibilityTimer = FlashTicks;
	}

	/// <summary>ObjC7_Beaten: the parts fly off, the jet goes, and the body falls.</summary>
	protected override void Defeat()
	{
		Level.Player.GainScore(DefeatScore);
		Defeated = true;
		_state = BodyState.Defeated;
		_step = 0;
		_touching = false;
		Body.Velocity = default;
		foreach ((Part part, Vector2 velocity) in BreakSpeeds)
		{
			PartState state = _parts[(int)part];
			state.Falling = true;
			state.Timer = 0x80;
			state.Velocity = velocity;
		}
		_parts[(int)Part.Jet].Visible = false;
		_sensor?.Finish();
		_sensor = null;
	}

	/// <summary>
	/// The touch areas. The body and the head can be hit, but not while they flash after a hit. The other parts hurt,
	/// and so do the bombs until 40 ticks after they explode.
	/// </summary>
	private void UpdateTouchAreas()
	{
		var rectangles = new List<CollisionRectangle>();
		if (_touching)
		{
			foreach ((Part part, int halfWidth, int halfHeight, bool hurts) in PartTouch)
			{
				PartState state = _parts[(int)part];
				if (!state.Visible || (!hurts && InvincibilityTimer > 0))
				{
					continue;
				}
				rectangles.Add(Box((int)part, state.Position, halfWidth, halfHeight));
			}
		}
		for (int i = 0; i < _bombs.Count; i++)
		{
			if (_bombs[i].ExplosionTicks < 40)
			{
				rectangles.Add(Box(FirstBombId + i, _bombs[i].Position, 0xC, 0x10));
			}
		}
		CollisionRectangles = rectangles.ToArray();
	}

	/// <summary>A touch area from its centre in original level pixels and its half size.</summary>
	private CollisionRectangle Box(int id, Vector2 centre, int halfWidth, int halfHeight)
	{
		Vector2 offset = (centre - Body.Position) * Scale;
		return new CollisionRectangle(this, id, (int)offset.X - halfWidth * Scale, (int)offset.Y - halfHeight * Scale, halfWidth * 2 * Scale, halfHeight * 2 * Scale);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		if (e.Id == (int)Part.Body || e.Id == (int)Part.Head)
		{
			if (Health > 0)
			{
				base.OnCollision(e);
			}
			return;
		}
		var character = (ICharacter)e.ActiveObject;
		if (character.CanBeHurt)
		{
			character.Hurt(Math.Sign(character.Position.X - Position.X));
		}
	}

	/// <summary>Obj_GetOrientationToPlayer: the player is in front of the robot.</summary>
	private bool IsPlayerInFront()
	{
		GetOrientationToPlayer(Body.Position, out double dx, out _, out _);
		bool playerRight = dx < 0;
		return playerRight == _facingRight;
	}

	/// <summary>
	/// Obj_GetOrientationToPlayer: the distance from the nearer character (in x) to a point, in original pixels, and
	/// whether the character is above the point.
	/// </summary>
	private void GetOrientationToPlayer(Vector2 point, out double dx, out double dy, out bool above)
	{
		ICharacter nearest = null;
		dx = 0;
		foreach (ICharacter character in new[] { Level.Player.Protagonist, Level.Player.Sidekick })
		{
			if (character == null)
			{
				continue;
			}
			double distance = point.X - (double)(character.Position.X - OriginX) / Scale;
			if (nearest == null || Math.Abs(distance) < Math.Abs(dx))
			{
				nearest = character;
				dx = distance;
			}
		}
		dy = nearest == null ? 0 : point.Y - (double)(nearest.Position.Y - OriginY) / Scale;
		above = dy >= 0;
	}

	/// <summary>The original camera X: the left edge of a 320 pixel screen at the centre of the HD camera.</summary>
	private double GetCameraX() => (Level.Camera.Bounds.X + Level.Camera.Bounds.Width / 2 - OriginX) / Scale - OriginalScreenWidth / 2;

	private void PlaySound(string key) => Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(key));

	private void PlayMusic(string key)
	{
		if (ResourceTree[key]?.Resource != null)
		{
			Level.SoundManager.PlayMusic(key);
		}
	}

	private Vector2 ToLevel(Vector2 position) => new Vector2(OriginX + position.X * Scale, OriginY + position.Y * Scale);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The bombs have priority 5 and come after the body
		foreach (Bomb bomb in _bombs)
		{
			if (bomb.State != 6)
			{
				_animation.Index = 0xE;
				objectRenderer.RenderOriginal(_animation, (bomb.Position - Body.Position) * Scale);
			}
		}
		objectRenderer.AdditiveColour = IsInvincibleFlashing ? FlashAdditiveColour : Colours.Black;
		foreach (Part part in DrawOrder)
		{
			PartState state = _parts[(int)part];
			if (!state.Visible || (part == Part.Jet && !_jetVisible))
			{
				continue;
			}
			_animation.Index = state.Frame;
			objectRenderer.RenderOriginal(_animation, (state.Position - Body.Position) * Scale, _facingRight);
		}
		objectRenderer.AdditiveColour = Colours.Black;
	}

	private static GroupFrame Lean(int ticks, int bodyX, int bodyY, int thighX, int thighY) => new GroupFrame(ticks, new[]
	{
		new Delta(Part.Body, bodyX, bodyY),
		new Delta(Part.FrontForearm, bodyX, bodyY),
		new Delta(Part.Arm, bodyX, bodyY),
		new Delta(Part.BackForearm, bodyX, bodyY),
		new Delta(Part.FrontThigh, thighX, thighY),
		new Delta(Part.BackThigh, thighX, thighY),
	});

	private static GroupFrame Walk(int ticks, params (Part Part, int X, int Y)[] deltas) =>
		new GroupFrame(ticks, deltas.Select(d => new Delta(d.Part, d.X, d.Y)).ToArray());
}
