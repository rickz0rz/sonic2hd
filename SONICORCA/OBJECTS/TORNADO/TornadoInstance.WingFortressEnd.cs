using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SONICORCA.OBJECTS.WFZTHRUST;

namespace SONICORCA.OBJECTS.TORNADO;

/// <summary>
/// The end of Wing Fortress Zone (ObjB2, subtype $54, ObjB2_Main_WFZ_End). The plane is not there at first. When the
/// player is at y $5EC or lower for $40 frames, the plane takes control of the player:
/// <list type="number">
/// <item>The player runs right to x $2E30 and waits until Eggman's ship in the background is $380 pixels away.</item>
/// <item>The plane comes up from below at 1 pixel each frame to the right and up. After $30 frames, the player jumps
/// and holds right for $38 frames ($28 for Super Sonic) to land on the plane.</item>
/// <item>The plane carries the player after the ship on a fixed path. The rocket under the plane starts at frame
/// $100. At frame $437, the player jumps and holds jump for $10 frames to take the grip under the ship.</item>
/// <item>At frame $460, the ship flies away with the player (LevEvents_WFZ_Routine4), and the plane flies away. At
/// frame $9C0, the original goes to Death Egg Zone: <see cref="WFZEnding.Finished"/> tells the area to end the act.</item>
/// </list>
/// The physics of SonicOrca are not exactly the same as the physics of the original. So that the end always
/// completes, the plane puts a player who misses it on its top, and the grip takes a player who misses it at frame
/// $460.
/// </summary>
public partial class TornadoInstance
{
	private const int WingFortressEndSubtype = 0x54;

	/// <summary>The position of the object in the object layout of the original.</summary>
	private static readonly Vector2i EndLayoutPosition = new Vector2i(0x2C60, 0x5EC);

	/// <summary>ObjB2_Wait_Leader_position: the player must be at this y or lower for $40 frames.</summary>
	private const int EndPlayerY = 0x5EC;

	private const int EndWaitTicks = 0x40;

	/// <summary>The plane starts at this position, below the ledge of the player.</summary>
	private static readonly Vector2i EndPlaneStart = new Vector2i(0x2E58, 0x66C);

	/// <summary>ObjB2_Move_Leader_edge: the player runs right to this x.</summary>
	private const int EndEdgeX = 0x2E30;

	/// <summary>ObjB2_Wait_for_plane: the plane comes when the background offset of the ship is this value.</summary>
	private const int EndPlaneShipOffset = 0x380;

	private const int EndPrepareTicks = 0x30;

	private const int EndJumpTicks = 0x38;

	private const int EndSuperJumpTicks = 0x28;

	/// <summary>The plane puts a player on its top when the player has not landed this long after the jump.</summary>
	private const int EndBoardTimeout = 0x80;

	/// <summary>ObjB2_Landed_on_plane: the rocket under the plane starts at this frame.</summary>
	private const int EndThrusterFrame = 0x100;

	/// <summary>ObjB2_Approaching_ship and ObjB2_Jump_to_ship: the player holds jump between these frames.</summary>
	private const int EndShipJumpFrame = 0x437;

	private const int EndShipJumpEndFrame = 0x447;

	/// <summary>The ship flies away at this frame.</summary>
	private const int EndLeaveFrame = 0x460;

	/// <summary>ObjB2_Dock_on_DEZ: the original goes to Death Egg Zone at this frame.</summary>
	private const int EndFinishFrame = 0x9C0;

	/// <summary>The grip under the ship (ObjB2, subtype $56) and the touch size of the original ($C7: 6 by 6 pixels).</summary>
	private static readonly Vector2i EndGripPosition = new Vector2i(0x3118, 0x3F0);

	private const int EndGripTouchWidth = 6 + 8;

	private const int EndGripTouchHeight = 6 + 0xB;

	/// <summary>The player hangs $10 pixels to the left of the grip.</summary>
	private const int EndGripHangX = -0x10;

	/// <summary>The height of a player whom the grip takes after a missed jump, below the grip.</summary>
	private const int EndGripMissY = 0x12;

	/// <summary>The rocket under the plane (ObjB2, subtype $5C), from the centre of the plane.</summary>
	private static readonly Vector2i EndThrusterOffset = new Vector2i(-0xC, 0x28);

	/// <summary>The fire at the ship (ObjB2, subtype $58) when the plane takes control, and when the ship flies away.</summary>
	private static readonly Vector2i[] EndStartFires = { new Vector2i(0x3070, 0x3B0), new Vector2i(0x3070, 0x430) };

	private static readonly Vector2i[] EndLeaveFires = { new Vector2i(0x3090, 0x3D0), new Vector2i(0x30C0, 0x3F0), new Vector2i(0x3090, 0x410) };

	/// <summary>ObjB2_Dock_on_DEZ: at each of these frames, the plane takes the next velocity (word_3AC16).</summary>
	private static readonly int[] EndPathFrames = { 0x1E0, 0x260, 0x2A0, 0x2C0, 0x300, 0x3A0, 0x3F0, 0x460, 0x4A0, 0x580 };

	/// <summary>The velocities of the path, in original pixels each frame (byte_3AC2A). The first is not used.</summary>
	private static readonly Vector2i[] EndPathVelocities =
	{
		new Vector2i(-1, -1), new Vector2i(1, 0), new Vector2i(0, 1), new Vector2i(1, -1), new Vector2i(1, 1), new Vector2i(1, -1),
		new Vector2i(-1, 1), new Vector2i(-1, -1), new Vector2i(-1, 1), new Vector2i(-2, 0), new Vector2i(0, 0)
	};

	private AnimationInstance _thruster;

	private EndState _endState;

	private Vector2 _endOrigin;

	private int _endWaitTicks;

	/// <summary>The frame counter of the end sequence (objoff_2A).</summary>
	private int _endFrame;

	private int _endJumpTicks;

	private int _endPathIndex;

	private Vector2i _endVelocity;

	private bool _endVisible;

	private bool _endThrusterOn;

	private bool _endGripped;

	private double _endGripY;

	private bool _endJumpHeld;

	/// <summary>The layer of the placement, for the fire at the ship, which has low priority like the original.</summary>
	private LevelLayer _endLowLayer;

	private enum EndState
	{
		WaitPlayer,
		MoveToEdge,
		WaitForPlane,
		PrepareJump,
		JumpToPlane,
		OnPlane,
		Approach,
		JumpToShip,
		Leave,
		Done
	}

	private void StartWingFortressEnd()
	{
		_thruster = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(TornadoType.ThrusterAnimationGroupResourceKey));
		_endOrigin = PositionPrecise - (Vector2)EndLayoutPosition * Scale;
		_endLowLayer = Layer;
		_endState = EndState.WaitPlayer;
		SetSolid(false);
	}

	/// <summary>A position of the original, in level units.</summary>
	private Vector2 EndToLevel(Vector2i position) => _endOrigin + (Vector2)position * Scale;

	/// <summary>A position in level units, in pixels of the original.</summary>
	private Vector2 EndToOriginal(Vector2 position) => (position - _endOrigin) / Scale;

	private void UpdateWingFortressEnd(ICharacter player)
	{
		Vector2 playerPosition = EndToOriginal(player.PositionPrecise);
		switch (_endState)
		{
			case EndState.WaitPlayer:
				// ObjB2_Wait_Leader_position
				if (playerPosition.Y < EndPlayerY)
				{
					return;
				}
				SetEndInput(player, 0, false);
				if (++_endWaitTicks >= EndWaitTicks)
				{
					BeginEnd();
				}
				return;
			case EndState.MoveToEdge:
				// ObjB2_Move_Leader_edge: the original also sets the normal speeds of Sonic again here
				if (playerPosition.X < EndEdgeX)
				{
					SetEndInput(player, 1, false);
				}
				else
				{
					SetEndInput(player, 0, false);
					player.GroundVelocity = 0.0;
					player.Velocity = new Vector2();
					_endState = EndState.WaitForPlane;
				}
				break;
			case EndState.WaitForPlane:
				SetEndInput(player, 0, false);
				if (WFZShipOffset.Get(Level) >= EndPlaneShipOffset)
				{
					_endState = EndState.PrepareJump;
					_endVelocity = new Vector2i(1, -1);
					_endFrame = 0;
					_endVisible = true;
					// The plane has high priority (make_art_tile(ArtTile_ArtNem_Tornado,0,1)): it shows in front of the ship
					Layer = Level.Map.Layers.Last();
					SetSolid(true);
				}
				break;
			case EndState.PrepareJump:
				SetEndInput(player, 0, false);
				if (++_endFrame == EndPrepareTicks)
				{
					_endState = EndState.JumpToPlane;
					SetEndInput(player, 0, true);
					_endJumpTicks = player.StateFlags.HasFlag(CharacterState.Super) ? EndSuperJumpTicks : EndJumpTicks;
				}
				MoveEndPlane();
				break;
			case EndState.JumpToPlane:
				// ObjB2_Jump_to_plane: right and jump for $38 frames
				_endFrame++;
				_endJumpTicks--;
				SetEndInput(player, _endJumpTicks >= 0 ? 1 : 0, _endJumpTicks >= 0);
				MoveEndPlane();
				if (IsStandingOnPlane(player))
				{
					_endState = EndState.OnPlane;
				}
				else if (_endJumpTicks < 0 && (_endJumpTicks < -EndBoardTimeout || IsBelowPlane(player)))
				{
					PutPlayerOnPlane(player);
					_endJumpTicks = -1;
				}
				break;
			case EndState.OnPlane:
				// ObjB2_Landed_on_plane: the player stands still at the centre of the plane
				SetEndInput(player, 0, false);
				if (++_endFrame >= EndThrusterFrame)
				{
					_endState = EndState.Approach;
					_endThrusterOn = true;
				}
				KeepPlayerOnPlane(player);
				MoveEndPlane();
				break;
			case EndState.Approach:
			case EndState.JumpToShip:
			case EndState.Leave:
				UpdateEndFlight(player);
				break;
		}
		UpdateGrip(player);
	}

	/// <summary>The plane takes control: it makes the fire at the ship, and the ship starts to move if it has not.</summary>
	private void BeginEnd()
	{
		_endState = EndState.MoveToEdge;
		PositionPrecise = EndToLevel(EndPlaneStart);
		WFZEnding.For(Level).Started = true;
		// The camera of SonicOrca is not at the same place as the camera of the original, so the level events can
		// have not started the ship yet
		WFZShipOffset.Start(Level);
		foreach (Vector2i fire in EndStartFires)
		{
			AddEndFire(fire);
		}
	}

	/// <summary>ObjB2_Approaching_ship, ObjB2_Jump_to_ship and ObjB2_Dock_on_DEZ.</summary>
	private void UpdateEndFlight(ICharacter player)
	{
		if (_endState == EndState.Approach && _endFrame >= EndShipJumpFrame)
		{
			_endState = EndState.JumpToShip;
		}
		SetEndInput(player, 0, _endState == EndState.JumpToShip && _endFrame < EndShipJumpEndFrame);
		if (_endState == EndState.JumpToShip && _endFrame >= EndLeaveFrame)
		{
			_endState = EndState.Leave;
			WFZShipOffset.Leave(Level);
			WFZEnding.For(Level).Leaving = true;
			foreach (Vector2i fire in EndLeaveFires)
			{
				AddEndFire(fire);
			}
			if (!_endGripped)
			{
				// The player missed the grip
				Grip(player, EndToLevel(EndGripPosition).Y + EndGripMissY * Scale);
			}
		}
		if (_endFrame >= EndFinishFrame)
		{
			_endState = EndState.Done;
			WFZEnding.For(Level).Finished = true;
			return;
		}
		_endFrame++;
		if (_endPathIndex < EndPathFrames.Length && _endFrame >= EndPathFrames[_endPathIndex])
		{
			_endPathIndex++;
			_endVelocity = EndPathVelocities[_endPathIndex];
		}
		if (_endVelocity == new Vector2i(0, 0) && Position.X + DesignBounds.Right >= Level.Camera.Bounds.Left)
		{
			// The camera of SonicOrca is wider than the screen of the original: the plane flies on until it is off the
			// screen
			MoveEndPlane(new Vector2i(-2, 0));
			return;
		}
		MoveEndPlane();
	}

	private void MoveEndPlane() => MoveEndPlane(_endVelocity);

	private void MoveEndPlane(Vector2i velocity) => PositionPrecise += (Vector2)velocity * Scale;

	private bool IsStandingOnPlane(ICharacter player) => !player.IsAirborne && player.ObjectLink == this;

	/// <summary>The player falls and is lower than the top of the plane.</summary>
	private bool IsBelowPlane(ICharacter player) => player.IsAirborne && player.Velocity.Y > 0.0 && player.PositionPrecise.Y > PositionPrecise.Y + Top;

	/// <summary>Puts the player just above the top of the plane, so that the player lands on it.</summary>
	private void PutPlayerOnPlane(ICharacter player)
	{
		player.PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + Top - 0x14 * Scale);
		player.Velocity = new Vector2();
		player.GroundVelocity = 0.0;
		player.IsAirborne = true;
	}

	/// <summary>The player stands at the centre of the plane. The plane then carries the player with its movement.</summary>
	private void KeepPlayerOnPlane(ICharacter player)
	{
		if (IsStandingOnPlane(player))
		{
			player.PositionPrecise = new Vector2(PositionPrecise.X, player.PositionPrecise.Y);
			player.GroundVelocity = 0.0;
		}
		else if (IsBelowPlane(player))
		{
			PutPlayerOnPlane(player);
		}
	}

	/// <summary>The grip under the ship (ObjB2, subtype $56): it takes the player who touches it.</summary>
	private void UpdateGrip(ICharacter player)
	{
		if (_endState == EndState.WaitPlayer)
		{
			return;
		}
		if (_endGripped)
		{
			HoldOnGrip(player);
			return;
		}
		Vector2 offset = EndToOriginal(player.PositionPrecise) - (Vector2)EndGripPosition;
		if (Math.Abs(offset.X) >= EndGripTouchWidth || Math.Abs(offset.Y) >= EndGripTouchHeight)
		{
			return;
		}
		if (player.IsHurt || player.IsDying || player.IsDead)
		{
			return;
		}
		Grip(player, player.PositionPrecise.Y);
	}

	private void Grip(ICharacter player, double y)
	{
		_endGripped = true;
		_endGripY = y;
		if (!player.IsAirborne)
		{
			player.LeaveGround();
		}
		player.GroundVelocity = 0.0;
		player.IsSpinball = false;
		player.IsJumping = false;
		player.Facing = -1;
		player.SpecialState = CharacterSpecialState.Grabbed;
		HoldOnGrip(player);
	}

	private void HoldOnGrip(ICharacter player)
	{
		player.IsObjectControlled = true;
		player.IsAirborne = true;
		player.PositionPrecise = new Vector2(EndToLevel(EndGripPosition).X + EndGripHangX * Scale, _endGripY);
		player.Velocity = new Vector2();
	}

	/// <summary>The fire at the ship (ObjB2, subtype $58). It needs the fire of the ship (ObjBC) from the same mod.</summary>
	private void AddEndFire(Vector2i position)
	{
		string key = Type.GetAbsolutePath(TornadoType.FireResourceKey);
		if (ResourceTree[key]?.Resource == null)
		{
			return;
		}
		// The property name of the state object must match the state variable of the fire
		Level.ObjectManager.AddObject(new ObjectPlacement(key, Level.Map.Layers.IndexOf(_endLowLayer), (Vector2i)EndToLevel(position), new { Fixed = true }));
	}

	/// <summary>
	/// The original locks the controls (Control_Locked) and sets the buttons. The jump button is pressed on the
	/// first frame and held after it.
	/// </summary>
	private void SetEndInput(ICharacter player, int direction, bool jump)
	{
		CharacterInputState input = player.Input;
		input.Throttle = direction;
		input.VerticalDirection = 0;
		if (jump)
		{
			input.A = _endJumpHeld ? CharacterInputButtonState.Down : CharacterInputButtonState.Pressed;
		}
		else
		{
			input.A = _endJumpHeld ? CharacterInputButtonState.Released : CharacterInputButtonState.Up;
		}
		_endJumpHeld = jump;
		input.B = CharacterInputButtonState.Up;
		input.C = CharacterInputButtonState.Up;
	}

	private void DrawWingFortressEnd(IObjectRenderer objectRenderer)
	{
		if (_endVisible)
		{
			DrawPlane(objectRenderer);
		}
		if (_endState != EndState.WaitPlayer)
		{
			// The rocket shows frame 0 until it starts, and then frames 0, 1 and 2, one frame each (Ani_objB2_b)
			_thruster.Index = _endThrusterOn ? _ticks % 3 : 0;
			objectRenderer.RenderOriginal(_thruster, (Vector2)EndThrusterOffset * Scale);
		}
	}
}
