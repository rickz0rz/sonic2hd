// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SUBMARINEEGGMAN.SubmarineEggmanInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;
using SONICORCA.OBJECTS.DUST;

#nullable disable
namespace SONICORCA.OBJECTS.SUBMARINEEGGMAN;

/// <summary>
/// The Hill Top Zone boss. Eggman rises out of one of two lava pits in a submarine. He fires a flame thrower,
/// sinks back into the lava and throws two fireballs that set fire to the ground. Then he comes up again
/// in the pit on the side of the protagonist.
/// </summary>
public class SubmarineEggmanInstance : BossObject
{
	public const int AnimationMobileInside = 0;
	public const int AnimationMobileBody = 1;
	public const int AnimationMobileBodyExploding = 2;
	public const int AnimationMobileBodyDefeated = 3;
	public const int AnimationMobileLights = 4;
	public const int AnimationFlameThrower = 5;
	public const int AnimationFlameThrowerShot = 6;
	public const int AnimationFireball = 7;
	public const int AnimationHighlightsSpawnIntro = 8;
	public const int AnimationHighlightsIntro = 9;
	public const int AnimationHighlightsEnd = 10;
	public const int AnimationHighlightsLava = 11;
	public const int AnimationBrokenPartsLavaA = 12;
	public const int AnimationBrokenPartsLavaB = 13;
	public const int AnimationBrokenPartsLavaC = 14;
	public const int AnimationBrokenPartsLavaD = 15;
	public const int AnimationBrokenPartsLavaE = 16;
	public const int AnimationFireballTransition = 17;
	public const int AnimationFireballIntro = 18;
	public const int AnimationFireballEnd = 19;
	private const int AnimationRobotnikNormal = 0;
	private const int AnimationRobotnikSmiling = 1;
	private const int AnimationRobotnikDefeated = 2;
	private const int AnimationDustSmoke = 1;
	private const double AltitudeSpeed = 3.5;
	private const int AltitudeSpeedDefeated = 8;

	private const int InitialHealth = 8;
	private const int DefeatScore = 1000;
	private const int BrokenPartCount = 5;

	/// <summary>
	/// The largest upward velocity that a character keeps after it hits the boss.
	/// </summary>
	private const double MaxHitBounceVelocityY = -15.125;

	/// <summary>
	/// The size of the area around the boss in which explosions appear after the defeat.
	/// </summary>
	private const int ExplosionAreaSize = 128;

	private AnimationInstance _animationInside;
	private AnimationInstance _animationRobotnik;
	private AnimationInstance _animationBody;
	private AnimationInstance _animationLights;
	private AnimationInstance _animationHighlightsSpawnIntro;
	private AnimationInstance _animationHighlightsIntro;
	private AnimationInstance _animationHighlightsEnd;
	private AnimationInstance _animationHighlightsLava;

	/// <summary>
	/// The draw offset of each broken part, by its animation index.
	/// </summary>
	private Dictionary<int, Vector2i> _brokenPartsPositions = new Dictionary<int, Vector2i>();

	/// <summary>
	/// The animation index of each broken part, in the order that the parts come off.
	/// </summary>
	private Dictionary<int, int> _brokenPartsIndexOrder = new Dictionary<int, int>();
	private AnimationInstance _brokenPartsA;
	private AnimationInstance _brokenPartsB;
	private AnimationInstance _brokenPartsC;
	private AnimationInstance _brokenPartsD;
	private AnimationInstance _brokenPartsE;

	// The pit positions and heights are offsets from the start position until OnStart adds the start position.
	// The right pit is at the start position.
	private int _leftX = -1024;
	private int _rightX;
	private int _leftPeekY = -416;
	private int _leftFireballY = -288;
	private int _leftSpawnY = 128;
	private int _rightPeekY = -528;
	private int _rightFireballY = -224;
	private int _rightSpawnY;
	private Vector2i _startPosition;
	private Vector2 _nonHoverPosition;
	private State _state;
	private Vector2 _velocity;
	private bool _flipX;
	private bool _isRightPit;
	private int _stateTimer;
	private int _brokenPartsCounter;
	private double _floatOffsetAngle;
	private bool _firedFireball;
	private AnimationGroup _animationGroup;
	private AnimationInstance _currentHighlight;

	protected override void OnStart()
	{
		ResourceTree resourceTree = Level.GameContext.ResourceTree;
		AnimationGroup animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, SubmarineEggmanType.AnimationGroupResourceKey);
		AnimationGroup robotnikAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, SubmarineEggmanType.RobotnikAnimationGroupResourceKey);
		_animationInside = new AnimationInstance(animationGroup);
		_animationRobotnik = new AnimationInstance(robotnikAnimationGroup);
		_animationBody = new AnimationInstance(animationGroup, AnimationMobileBody);
		_animationLights = new AnimationInstance(animationGroup, AnimationMobileLights);
		_animationHighlightsSpawnIntro = new AnimationInstance(animationGroup, AnimationHighlightsSpawnIntro);
		_animationHighlightsIntro = new AnimationInstance(animationGroup, AnimationHighlightsIntro);
		_animationHighlightsEnd = new AnimationInstance(animationGroup, AnimationHighlightsEnd);
		_animationHighlightsLava = new AnimationInstance(animationGroup, AnimationHighlightsLava);
		_brokenPartsPositions[AnimationBrokenPartsLavaA] = new Vector2i(-20, -64);
		_brokenPartsPositions[AnimationBrokenPartsLavaC] = new Vector2i(-8, -88);
		_brokenPartsPositions[AnimationBrokenPartsLavaD] = new Vector2i(-16, -112);
		_brokenPartsPositions[AnimationBrokenPartsLavaB] = new Vector2i(-44, -112);
		_brokenPartsPositions[AnimationBrokenPartsLavaE] = new Vector2i(-80, -112);
		_brokenPartsIndexOrder[0] = AnimationBrokenPartsLavaE;
		_brokenPartsIndexOrder[1] = AnimationBrokenPartsLavaB;
		_brokenPartsIndexOrder[2] = AnimationBrokenPartsLavaD;
		_brokenPartsIndexOrder[3] = AnimationBrokenPartsLavaC;
		_brokenPartsIndexOrder[4] = AnimationBrokenPartsLavaA;
		_animationGroup = animationGroup;
		_brokenPartsA = new AnimationInstance(_animationGroup, AnimationBrokenPartsLavaA);
		_brokenPartsB = new AnimationInstance(_animationGroup, AnimationBrokenPartsLavaB);
		_brokenPartsC = new AnimationInstance(_animationGroup, AnimationBrokenPartsLavaC);
		_brokenPartsD = new AnimationInstance(_animationGroup, AnimationBrokenPartsLavaD);
		_brokenPartsE = new AnimationInstance(_animationGroup, AnimationBrokenPartsLavaE);
		ExplosionResourceKey = Type.GetAbsolutePath(SubmarineEggmanType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(SubmarineEggmanType.BossHitSoundResourceKey);
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -100, -100, 200, 200)
		};
		_startPosition = Position;
		_leftX += _startPosition.X;
		_rightX += _startPosition.X;
		_leftPeekY += _startPosition.Y;
		_leftFireballY += _startPosition.Y;
		_leftSpawnY += _startPosition.Y;
		_rightPeekY += _startPosition.Y;
		_rightFireballY += _startPosition.Y;
		_rightSpawnY += _startPosition.Y;
		LockLifetime = true;
		_velocity = new Vector2(0.0, -AltitudeSpeed);
		_isRightPit = true;
		_state = State.Raise;
		Health = InitialHealth;
	}

	private void ResetHighlights()
	{
		_animationHighlightsSpawnIntro.ResetFrame();
		_animationHighlightsSpawnIntro.Cycles = 0;
		_animationHighlightsIntro.ResetFrame();
		_animationHighlightsIntro.Cycles = 0;
		_animationHighlightsEnd.ResetFrame();
		_animationHighlightsEnd.Cycles = 0;
		_currentHighlight = _animationHighlightsSpawnIntro;
	}

	private void CleanHighlights()
	{
		ResetHighlights();
		_currentHighlight = null;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (Health == 1 && Level.Random.Next(15) == 0)
			EmitSmoke((Vector2)new Vector2i(Level.Random.Next(-128, 128), Level.Random.Next(-64, -32)), new Vector2(0.0, -4.0));
		switch (_state)
		{
			case State.Raise:
				// Rise out of the lava to the peek height of the current pit.
				PositionPrecise += _velocity;
				if (Position.Y > (_isRightPit ? _rightPeekY : _leftPeekY))
					break;
				_velocity.Y = 0.0;
				_state = State.PrepareFlameThrower;
				_stateTimer = 60;
				_nonHoverPosition = PositionPrecise;
				_floatOffsetAngle = 0.0;
				break;
			case State.PrepareFlameThrower:
				// Hover, then show the flame thrower flame, and then fire the flame thrower shot.
				--_stateTimer;
				if (_stateTimer > 0)
				{
					Hover();
					break;
				}
				if (_stateTimer == 0)
				{
					CreateFlameThrowerFlame();
					ResetHighlights();
					break;
				}
				if (_stateTimer > -120)
					break;
				CreateFlameThrowerShot();
				_currentHighlight = _animationHighlightsIntro;
				_stateTimer = 47;
				_state = State.Hover;
				break;
			case State.Hover:
				--_stateTimer;
				if (_stateTimer > 0)
				{
					Hover();
					break;
				}
				_currentHighlight = _animationHighlightsEnd;
				_velocity = new Vector2(0.0, AltitudeSpeed);
				_state = State.Lower;
				break;
			case State.Lower:
				UpdateLower();
				break;
			case State.Defeated:
				UpdateDefeated();
				break;
		}
	}

	/// <summary>
	/// Sinks into the lava and throws two fireballs on the way down. At the bottom, moves to the pit
	/// on the side of the protagonist and starts to rise again.
	/// </summary>
	private void UpdateLower()
	{
		PositionPrecise += _velocity;
		if (Position.Y > (_isRightPit ? _rightFireballY : _leftFireballY) && !_firedFireball)
		{
			CreateFireball(-1);
			CreateFireball(1);
			Level.SoundManager.PlaySound(Position, SubmarineEggmanType.FireBallSoundResourceKey);
			_firedFireball = true;
		}
		if (_isRightPit)
		{
			if (Position.Y < _rightSpawnY)
				return;
			Position = new Vector2i(Position.X, _rightSpawnY);
		}
		else
		{
			if (Position.Y < _leftSpawnY)
				return;
			Position = new Vector2i(Position.X, _leftSpawnY);
		}
		_velocity = new Vector2(0.0, -AltitudeSpeed);
		_state = State.Raise;
		_firedFireball = false;
		ICharacter protagonist = Level.Player.Protagonist;
		if (protagonist.Position.X < _startPosition.X - 512)
		{
			Position = new Vector2i(_leftX, _leftSpawnY);
			_isRightPit = false;
		}
		else
		{
			Position = new Vector2i(_rightX, _rightSpawnY);
			_isRightPit = true;
		}
		_flipX = protagonist.Position.X >= Position.X;
	}

	/// <summary>
	/// Releases the broken parts one by one with explosions, then sinks into the lava.
	/// </summary>
	private void UpdateDefeated()
	{
		CleanHighlights();
		--_stateTimer;
		if (_stateTimer > 0)
		{
			if (_stateTimer % 16 == 0 && _brokenPartsCounter < BrokenPartCount)
			{
				int partIndex = _brokenPartsIndexOrder[_brokenPartsCounter];
				ReleaseFragmentMetal(Position + _brokenPartsPositions[partIndex], 6.0, partIndex);
				// Stop drawing the part on the boss.
				switch (partIndex)
				{
					case AnimationBrokenPartsLavaA:
						_brokenPartsA = null;
						break;
					case AnimationBrokenPartsLavaB:
						_brokenPartsB = null;
						break;
					case AnimationBrokenPartsLavaC:
						_brokenPartsC = null;
						break;
					case AnimationBrokenPartsLavaD:
						_brokenPartsD = null;
						break;
					case AnimationBrokenPartsLavaE:
						_brokenPartsE = null;
						break;
				}
				++_brokenPartsCounter;
			}
			UpdateExplosions(ExplosionAreaSize);
			if (_stateTimer >= 30)
				return;
			UpdateSmoke();
			return;
		}
		_animationBody.Index = AnimationMobileBodyDefeated;
		_animationRobotnik.Index = AnimationRobotnikNormal;
		UpdateSmoke();
		if (_stateTimer > -60)
			return;
		Defeated = true;
		Fleeing = true;
		LockLifetime = false;
		Position += new Vector2i(0, AltitudeSpeedDefeated);
	}

	private void EmitSmoke(Vector2 offset, Vector2 velocity)
	{
		Matrix4 matrix = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
		if (_flipX)
			matrix *= Matrix4.CreateScale(-1.0, 1.0);
		Vector2i position = (Vector2i)((matrix * Matrix4.CreateTranslation(offset)).RotateZ(0.0) * new Vector2(0.0, 100.0));
		DustInstance dustInstance = Level.ObjectManager.AddObject(new ObjectPlacement(SubmarineEggmanType.DefaultBrokenSmokeResourceKey, Level.Map.Layers.IndexOf(Layer), position)) as DustInstance;
		dustInstance.SetDustAnimationIndex(AnimationDustSmoke);
		dustInstance.Velocity = velocity;
	}

	/// <summary>
	/// Moves the boss up and down around the hover position.
	/// </summary>
	private void Hover()
	{
		PositionPrecise = _nonHoverPosition + new Vector2(0.0, Math.Sin(_floatOffsetAngle) / Math.PI * 16.0);
		_floatOffsetAngle += Math.PI / 64.0;
	}

	private void CreateFlameThrowerFlame()
	{
		FlameThrower flameThrower = Level.ObjectManager.AddSubObject<FlameThrower>(this);
		flameThrower.FlipX = _flipX;
		flameThrower.Position = Position + new Vector2i(_flipX ? 132 : -132, -121);
	}

	private void CreateFlameThrowerShot()
	{
		FlameThrowerShot flameThrowerShot = Level.ObjectManager.AddSubObject<FlameThrowerShot>(this);
		flameThrowerShot.FlipX = _flipX;
		flameThrowerShot.Position = Position + new Vector2i(_flipX ? 448 : -448, -112);
		flameThrowerShot.Velocity = new Vector2i(_flipX ? 16 : -16, 0);
	}

	/// <summary>
	/// Throws a fireball to the left (-1) or to the right (1). The velocity depends on the pit, so that the
	/// fireball lands on the ground next to the pit.
	/// </summary>
	private void CreateFireball(int direction)
	{
		Fireball fireball = Level.ObjectManager.AddSubObject<Fireball>(this);
		fireball.IsMainFireball = true;
		fireball.SpawnDirection = (Fireball.FireSpawn)direction;
		fireball.Position = Position;
		if (_isRightPit)
		{
			if (direction == 1)
				fireball.Velocity = new Vector2(7.0, -25.0);
			else if (direction == -1)
				fireball.Velocity = new Vector2(-6.85, -25.0);
		}
		else if (direction == 1)
			fireball.Velocity = new Vector2(6.7, -21.0);
		else if (direction == -1)
			fireball.Velocity = new Vector2(-7.0, -21.0);
	}

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (character.Velocity.Y < MaxHitBounceVelocityY)
			character.Velocity = new Vector2(character.Velocity.X, MaxHitBounceVelocityY);
		if (Health <= 0)
			return;
		_animationRobotnik.Index = AnimationRobotnikSmiling;
	}

	protected override void Defeat()
	{
		base.Defeat();
		_state = State.Defeated;
		_stateTimer = 179;
		_animationBody.Index = AnimationMobileBodyExploding;
		_animationRobotnik.Index = AnimationRobotnikDefeated;
		CollisionRectangles = new CollisionRectangle[0];
		Level.Player.GainScore(DefeatScore);
		Level.CreateScoreObject(DefeatScore, Position + new Vector2i(0, -128));
	}

	private void UpdateSmoke()
	{
		if (Level.Random.Next(15) != 0)
			return;
		EmitSmoke((Vector2)new Vector2i(Level.Random.Next(-128, 128), Level.Random.Next(-64, -32)), new Vector2(0.0, -4.0));
	}

	/// <summary>
	/// Releases a broken part in a random direction.
	/// </summary>
	private void ReleaseFragmentMetal(Vector2i position, double speed, int index)
	{
		double angle = Level.Random.NextRadians();
		Vector2 velocity = new Vector2(speed * Math.Cos(angle), speed * Math.Sin(angle));
		ReleaseFragmentMetal(position, velocity, index);
	}

	private void ReleaseFragmentMetal(Vector2i position, Vector2 velocity, int index)
	{
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = index;
		fragment.Position = position;
		fragment.Velocity = velocity;
		fragment.AngularVelocity = 2.0 * Math.PI / 15.0;
		fragment.FlipX = Level.Random.NextBoolean();
		fragment.Initialise();
	}

	protected override void OnAnimate()
	{
		_animationInside.Animate();
		_animationRobotnik.Animate();
		_animationBody.Animate();
		_animationLights.Animate();
		if (_currentHighlight == null)
			return;
		if (_currentHighlight == _animationHighlightsEnd)
		{
			_currentHighlight.Animate();
			if (_currentHighlight.Cycles <= 0)
				return;
			_currentHighlight = null;
			CleanHighlights();
		}
		else
			_currentHighlight.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.FilterAmount *= 0.5;
		objectRenderer.Render(_animationInside, _flipX);
		objectRenderer.Render(_animationRobotnik, (Vector2)new Vector2i(_flipX ? 32 : -32, -42), _flipX);
		if (IsInvincibleFlashing)
			objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
		objectRenderer.Render(_animationBody, _flipX);
		objectRenderer.EmitsLight = true;
		objectRenderer.Render(_animationLights, _flipX);
		if (_currentHighlight != null)
			objectRenderer.Render(_currentHighlight, (Vector2)new Vector2i(0, 0), _flipX);
		objectRenderer.Render(_animationHighlightsLava, (Vector2)new Vector2i(0, 0), _flipX);
		if (_state != State.Defeated)
			return;
		// Draw the broken parts that are still on the boss.
		DrawBrokenPart(objectRenderer, _brokenPartsA, AnimationBrokenPartsLavaA);
		DrawBrokenPart(objectRenderer, _brokenPartsC, AnimationBrokenPartsLavaC);
		DrawBrokenPart(objectRenderer, _brokenPartsD, AnimationBrokenPartsLavaD);
		DrawBrokenPart(objectRenderer, _brokenPartsB, AnimationBrokenPartsLavaB);
		DrawBrokenPart(objectRenderer, _brokenPartsE, AnimationBrokenPartsLavaE);
	}

	private void DrawBrokenPart(IObjectRenderer objectRenderer, AnimationInstance brokenPart, int animationIndex)
	{
		if (brokenPart == null)
			return;
		objectRenderer.Render(brokenPart, (Vector2)_brokenPartsPositions[animationIndex], _flipX);
	}

	private enum State
	{
		Raise,
		PrepareFlameThrower,
		Hover,
		Lower,
		Defeated,
	}

	/// <summary>
	/// The flame at the front of the flame thrower. It hurts a character that touches it while the flame is out.
	/// </summary>
	private class FlameThrower : ParticleObject
	{
		/// <summary>
		/// The animation frame at which the flame comes out, and the frame at which it goes away.
		/// </summary>
		private const int FlameOutFrame = 35;
		private const int FlameEndFrame = 50;

		public FlameThrower()
			: base(SubmarineEggmanType.AnimationGroupResourceKey, AnimationFlameThrower)
		{
			FilterMultiplier = 0.0;
		}

		protected override void OnCollision(CollisionEvent e)
		{
			if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
				return;
			((ICharacter)e.ActiveObject).Hurt(-1);
		}

		protected override void OnAnimate() => base.OnAnimate();

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (_animationInstance.CurrentFrameIndex == FlameOutFrame)
			{
				if (CollisionRectangles != null && CollisionRectangles.Length != 0)
					return;
				// The collision rectangle covers the middle half of the flame frame.
				Animation.Frame currentFrame = _animationInstance.CurrentFrame;
				int x = (int)((FlipX ? -currentFrame.Offset.X : currentFrame.Offset.X) - currentFrame.Source.Width / 2.0);
				int y = (int)(currentFrame.Offset.Y - currentFrame.Source.Height / 4.0);
				int width = currentFrame.Source.Width;
				int height = currentFrame.Source.Height / 2;
				CollisionRectangles = new CollisionRectangle[]
				{
					new CollisionRectangle(this, 0, x, y, width, height)
				};
			}
			else
			{
				// The second part of this condition has "&&" where "||" was probably intended.
				// It throws when CollisionRectangles is null.
				if (_animationInstance.CurrentFrameIndex != FlameEndFrame || CollisionRectangles == null && CollisionRectangles.Length == 0)
					return;
				CollisionRectangles = new CollisionRectangle[0];
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (_animationInstance.CurrentFrameIndex >= FlameOutFrame)
			{
				if (!CanDraw())
					return;
				Vector2 destination = new Vector2(12.0, 9.0);
				IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
				using (objectRenderer.BeginMatixState())
				{
					if (FlipX)
						objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
					if (FlipY)
						objectRenderer.ModelMatrix *= Matrix4.CreateScale(1.0, -1.0);
					if (FilterMultiplier == 0.0)
						objectRenderer.Filter = 0;
					else
						objectRenderer.FilterAmount *= FilterMultiplier;
					objectRenderer.BlendMode = AdditiveBlending ? BlendMode.Additive : BlendMode.Alpha;
					objectRenderer.Render(_animationInstance, destination);
				}
			}
			else
				base.OnDraw(renderer, viewOptions);
		}
	}

	/// <summary>
	/// The shot of the flame thrower. It moves in a straight line and hurts characters.
	/// </summary>
	private class FlameThrowerShot : Enemy
	{
		private AnimationInstance _animation;

		public bool FlipX { get; set; }

		public Vector2i Velocity { get; set; }

		protected override void OnStart()
		{
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SubmarineEggmanType.AnimationGroupResourceKey), AnimationFlameThrowerShot);
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -16, -16, 32, 32)
			};
		}

		protected override void OnUpdate() => Position += Velocity;

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation, FlipX);
		}
	}

	/// <summary>
	/// A fireball that the boss throws. When it lands, it turns into flames. The main fireball also starts
	/// a row of fires on the ground.
	/// </summary>
	private class Fireball : Enemy
	{
		private const double Gravity = 0.875;
		private AnimationInstance _currentAnimation;
		private AnimationInstance _fireball;
		private AnimationInstance _fireballTransition;

		/// <summary>
		/// The ticks after landing. The value starts at 10, and a fire starts at 15, 20, 25 and 29.
		/// </summary>
		private int _ticks = 10;
		private int _fireInstances;

		public Vector2 Velocity { get; set; }

		public bool IsMainFireball { get; set; }

		public FireSpawn SpawnDirection { get; set; } = FireSpawn.Left;

		protected override void OnStart()
		{
			_fireball = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SubmarineEggmanType.AnimationGroupResourceKey), AnimationFireball);
			_fireballTransition = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SubmarineEggmanType.AnimationGroupResourceKey), AnimationFireballTransition);
			_currentAnimation = _fireball;
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -32, -32, 64, 64)
			};
		}

		protected override void OnUpdate()
		{
			PositionPrecise += Velocity;
			Velocity += new Vector2(0.0, Gravity);
			if (!IsTouchingGround())
				return;
			Velocity = new Vector2(0.0, 0.0);
			if (_currentAnimation != _fireballTransition)
				_currentAnimation = _fireballTransition;
			if (!IsMainFireball)
				return;
			++_ticks;
			if (_ticks == 15 || _ticks == 20 || _ticks == 25 || _ticks == 29)
				CreateFire();
		}

		/// <summary>
		/// Starts a fire on the ground. Each fire is one fire width further in the spawn direction.
		/// </summary>
		private void CreateFire()
		{
			Fire fire = Level.ObjectManager.AddSubObject<Fire>(ParentObject);
			fire.Position = Position + new Vector2i(_fireInstances * fire.DesignBounds.Width * (int)SpawnDirection, 0);
			fire.BubbleLeftCount = 4;
			fire.BubbleRightCount = 4;
			fire.Priority = 1024;
			fire.Layer = Level.Map.Layers[Level.Map.Layers.Count - 1];
			Level.SoundManager.PlaySound(Position, Type.GetAbsolutePath(SubmarineEggmanType.FireBurnSoundResourceKey));
			++_fireInstances;
		}

		/// <summary>
		/// Moves the fireball up onto the ground. Returns true when the fireball touches the ground.
		/// </summary>
		private bool IsTouchingGround()
		{
			bool touching = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 32))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					touching = true;
				}
			}
			return touching;
		}

		protected override void OnAnimate()
		{
			if (_currentAnimation != null)
				_currentAnimation.Animate();
			if (_currentAnimation != _fireballTransition || _currentAnimation.Cycles <= 0)
				return;
			_currentAnimation = null;
			Finish();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			if (_currentAnimation == null)
				return;
			if (_currentAnimation == _fireballTransition)
			{
				bool flipX = SpawnDirection == FireSpawn.Right;
				objectRenderer.Render(_currentAnimation, new Vector2(flipX ? 80.0 : -80.0, -8.0), flipX);
			}
			else
			{
				// Turn the fireball so that it points in the direction of travel.
				double angle = Velocity.Angle + 6.0 * Math.PI / 5.0;
				using (objectRenderer.BeginMatixState())
				{
					objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(angle);
					objectRenderer.Render(_currentAnimation);
				}
			}
		}

		public enum FireSpawn
		{
			Left = -1,
			Right = 1,
		}
	}

	/// <summary>
	/// A fire on the ground. It spreads one time to the left and to the right, and each new fire spreads
	/// one less time in the same direction.
	/// </summary>
	private class Fire : Enemy
	{
		private const int LifetimeTicks = 74;
		private const int SpreadTick = 66;
		private const int SpreadDistance = 64;

		private AnimationInstance _currentAnimation;
		private AnimationInstance _fireballIntro;
		private AnimationInstance _fireballEnd;
		private int _timer;

		public int BubbleLeftCount { get; set; }

		public int BubbleRightCount { get; set; }

		protected override void OnStart()
		{
			_fireballIntro = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SubmarineEggmanType.AnimationGroupResourceKey), AnimationFireballIntro);
			_fireballEnd = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SubmarineEggmanType.AnimationGroupResourceKey), AnimationFireballEnd);
			_currentAnimation = _fireballIntro;
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -32, -32, 64, 64)
			};
			_timer = LifetimeTicks;
		}

		protected override void OnUpdate()
		{
			--_timer;
			if (_timer == SpreadTick)
			{
				if (BubbleLeftCount > 0)
					BubbleFire(Position + new Vector2i(-SpreadDistance, 0), BubbleLeftCount--, 0);
				if (BubbleRightCount <= 0)
					return;
				BubbleFire(Position + new Vector2i(SpreadDistance, 0), 0, BubbleRightCount--);
			}
			else
			{
				if (_timer != 0)
					return;
				_currentAnimation = _fireballEnd;
			}
		}

		private void BubbleFire(Vector2i bubblePosition, int leftCount, int rightCount)
		{
			if (!IsGroundBelow(ref bubblePosition))
				return;
			Fire fire = Level.ObjectManager.AddSubObject<Fire>(ParentObject);
			fire.Position = bubblePosition;
			fire.BubbleLeftCount = leftCount;
			fire.BubbleRightCount = rightCount;
		}

		/// <summary>
		/// Moves the position by the collision shift. The collisions are for this fire, not for the new position.
		/// </summary>
		private bool IsGroundBelow(ref Vector2i position)
		{
			bool groundBelow = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 32))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					position.Y += (int)collision.Shift;
					groundBelow = true;
				}
			}
			return groundBelow;
		}

		protected override void OnAnimate()
		{
			if (_currentAnimation != null)
				_currentAnimation.Animate();
			if (_currentAnimation != _fireballEnd || _currentAnimation.Cycles <= 0)
				return;
			_currentAnimation = null;
			Finish();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			if (_currentAnimation == null)
				return;
			objectRenderer.Render(_currentAnimation);
		}
	}
}
