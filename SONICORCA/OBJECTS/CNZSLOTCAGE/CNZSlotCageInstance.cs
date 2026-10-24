using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZSLOTCAGE;

/// <summary>
/// A cage, as in Sonic 2 (ObjD6). It holds a player that lands on it (or hits it from below) in a ball.
/// <para>
/// A cage that gives points gives 100 points each 16 frames, 8 times, then lets the player go.
/// </para>
/// <para>
/// A slot machine cage starts a spin. When the spin ends, it sends one prize each 2 frames from a circle of 128
/// pixels toward the player. Each prize moves 1/16 of the way each frame. A ring prize gives a ring. A bomb prize
/// (any negative reward gives 100 of them) takes a ring. When no prize is left, the cage lets the player go.
/// </para>
/// </summary>
public class CNZSlotCageInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The cage is $18 pixels to each side, and $10 up and $11 down.</summary>
	private const int HalfWidth = 0x18 * Scale;

	private const int Top = 0x10 * Scale;

	private const int Bottom = 0x11 * Scale;

	private const int PointsTicks = 0x78;

	private const int Points = 100;

	private const int CooldownTicks = 0x1E;

	/// <summary>$400 at four times the resolution: the speed at which the player falls out.</summary>
	private const double ReleaseSpeed = 16.0;

	private const int MaxPrizes = 0x10;

	private const int BombCount = 0x64;

	private const int RingDelay = 0x1A;

	private const int BombDelay = 0x1E;

	private const int RingAngleStep = 0x89;

	private const int BombAngleStep = 0x90;

	private const int PrizeRadius = 128 * Scale;

	private readonly Dictionary<ICharacter, PlayerState> _players = new Dictionary<ICharacter, PlayerState>();

	private readonly List<Prize> _prizes = new List<Prize>();

	private AnimationInstance _animation;

	private AnimationInstance _bombAnimation;

	private AnimationInstance _ringAnimation;

	private bool _slotMachine;

	private int _bombsLeft = -1;

	private int _prizeAngle;

	private int _bombSoundCounter;

	private int _ticks;

	private sealed class PlayerState
	{
		public State State;

		public int Timer;
	}

	private enum State
	{
		Free,
		Held,
		Cooldown
	}

	private sealed class Prize
	{
		public bool Bomb;

		public double X;

		public double Y;

		public int Delay;

		public ICharacter Player;
	}

	/// <summary>The cage runs a slot machine (subtype not 0) instead of giving points.</summary>
	[StateVariable]
	private bool SlotMachine
	{
		get => _slotMachine;
		set => _slotMachine = value;
	}

	public CNZSlotCageInstance() => DesignBounds = new Rectanglei(-PrizeRadius, -PrizeRadius, PrizeRadius * 2, PrizeRadius * 2);

	private CNZSlotMachine Machine => CNZSlotMachine.For(Level);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZSlotCageType.AnimationGroupResourceKey));
		_bombAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZSlotCageType.BombAnimationGroupResourceKey));
		_ringAnimation = new AnimationInstance(ResourceTree, CNZSlotCageType.RingAnimationGroupResourceKey);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -Top, HalfWidth * 2, Top + Bottom));
		// A player that hits the bottom of the cage is held too
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -HalfWidth, Bottom - 8, HalfWidth * 2, 16)
		};
	}

	protected override void OnUpdate()
	{
		_ticks++;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			PlayerState player = GetPlayer(character);
			switch (player.State)
			{
				case State.Free:
					if (character.ObjectLink == this)
					{
						Take(character, player);
					}
					break;
				case State.Held:
					UpdateHeld(character, player);
					break;
				case State.Cooldown:
					if (--player.Timer < 0)
					{
						player.State = State.Free;
						if (_slotMachine)
						{
							Machine.InUse = false;
						}
					}
					break;
			}
		}
		UpdatePrizes();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject is ICharacter character && character.Velocity.Y < 0.0 && character.Position.Y > Position.Y)
		{
			PlayerState player = GetPlayer(character);
			if (player.State == State.Free)
			{
				Take(character, player);
			}
		}
	}

	private PlayerState GetPlayer(ICharacter character)
	{
		if (!_players.TryGetValue(character, out PlayerState player))
		{
			player = new PlayerState();
			_players[character] = player;
		}
		return player;
	}

	private void Take(ICharacter character, PlayerState player)
	{
		if (character.IsDebug || character.IsObjectControlled || (_slotMachine && Machine.InUse))
		{
			return;
		}
		player.State = State.Held;
		player.Timer = PointsTicks;
		Hold(character);
		if (_slotMachine && Machine.StartSpin())
		{
			_prizeAngle = 0;
			Machine.InUse = true;
			_bombsLeft = -1;
		}
	}

	private void Hold(ICharacter character)
	{
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.Position = Position;
		character.Velocity = new Vector2(0.0, 0.001);
	}

	private void UpdateHeld(ICharacter character, PlayerState player)
	{
		if (character.IsDebug || character.IsDying)
		{
			Release(character, player);
			return;
		}
		Hold(character);
		if (!_slotMachine)
		{
			if (--player.Timer < 0)
			{
				Release(character, player);
			}
			else if ((player.Timer & 0xF) == 0)
			{
				PlaySound(CNZSlotCageType.TickSoundResourceKey);
				Level.Player.GainScore(Points);
				Level.CreateScoreObject(Points, Position);
			}
			return;
		}
		if (!Machine.IsIdle)
		{
			if ((_ticks & 0xF) == 0)
			{
				PlaySound(CNZSlotCageType.TickSoundResourceKey);
			}
			return;
		}
		if (GivePrizes(character))
		{
			Release(character, player);
		}
	}

	/// <summary>Sends the prizes of the reward. Returns true when no prize is left and the player can go.</summary>
	private bool GivePrizes(ICharacter character)
	{
		int reward = Machine.Reward;
		if (reward < 0)
		{
			if (_bombsLeft < 0)
			{
				_bombsLeft = BombCount;
			}
			bool spawned = false;
			if (_bombsLeft != 0 && (Level.Ticks & 1) != 0 && _prizes.Count < MaxPrizes)
			{
				AddPrize(character, bomb: true);
				_bombsLeft--;
				spawned = true;
			}
			_bombSoundCounter++;
			return !spawned && _bombsLeft == 0 && _prizes.Count == 0;
		}
		if (reward > 0)
		{
			if ((Level.Ticks & 1) != 0 && _prizes.Count < MaxPrizes)
			{
				AddPrize(character, bomb: false);
				Machine.Reward = reward - 1;
			}
			return false;
		}
		return _prizes.Count == 0;
	}

	private void AddPrize(ICharacter character, bool bomb)
	{
		double angle = (_prizeAngle & 0xFF) * Math.PI / 128.0;
		_prizeAngle += bomb ? BombAngleStep : RingAngleStep;
		_prizes.Add(new Prize
		{
			Bomb = bomb,
			X = Position.X + Math.Cos(angle) * PrizeRadius,
			Y = Position.Y + Math.Sin(angle) * PrizeRadius,
			Delay = bomb ? BombDelay : RingDelay,
			Player = character
		});
	}

	private void UpdatePrizes()
	{
		for (int i = _prizes.Count - 1; i >= 0; i--)
		{
			Prize prize = _prizes[i];
			prize.X -= (prize.X - Position.X) / 16.0;
			prize.Y -= (prize.Y - Position.Y) / 16.0;
			if (--prize.Delay > 0)
			{
				continue;
			}
			_prizes.RemoveAt(i);
			if (!prize.Bomb)
			{
				Level.Player.GainRings();
				PlaySound(CNZSlotCageType.RingSoundResourceKey);
				continue;
			}
			// A bomb takes one ring. The sound plays at most each 5 frames.
			if (_bombSoundCounter >= 5)
			{
				_bombSoundCounter = 0;
				PlaySound(CNZSlotCageType.BombSoundResourceKey);
			}
			if (Level.Player.CurrentRings > 0)
			{
				Level.Player.CurrentRings--;
			}
		}
	}

	private void Release(ICharacter character, PlayerState player)
	{
		player.State = State.Cooldown;
		player.Timer = CooldownTicks;
		character.IsObjectControlled = false;
		character.LeaveGround();
		character.IsAirborne = true;
		character.Velocity = new Vector2(0.0, ReleaseSpeed);
	}

	private void PlaySound(string key)
	{
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(key));
	}

	protected override void OnStop()
	{
		foreach (KeyValuePair<ICharacter, PlayerState> pair in _players)
		{
			if (pair.Value.State == State.Held)
			{
				Release(pair.Key, pair.Value);
			}
		}
	}

	protected override void OnAnimate()
	{
		_ringAnimation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The cage flashes (frames 1 and 0, 2 frames each) while it holds a player
		bool held = false;
		foreach (PlayerState player in _players.Values)
		{
			held |= player.State == State.Held;
		}
		_animation.Index = held && (_ticks / 2) % 2 == 0 ? 1 : 0;
		objectRenderer.Render(_animation);
		foreach (Prize prize in _prizes)
		{
			objectRenderer.Render(prize.Bomb ? _bombAnimation : _ringAnimation, new Vector2(prize.X - Position.X, prize.Y - Position.Y));
		}
	}
}
