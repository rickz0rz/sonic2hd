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
	public const int AnimationBrokenPartsLavaE = 16 /*0x10*/;
	public const int AnimationFireballTransition = 17;
	public const int AnimationFireballIntro = 18;
	public const int AnimationFireballEnd = 19;
	private const int AnimationRobotnikNormal = 0;
	private const int AnimationRobotnikSmiling = 1;
	private const int AnimationRobotnikDefeated = 2;
	private const int AnimationDustSmoke = 1;
	private const double AltitudeSpeed = 3.5;
	private const int AltitudeSpeedDefeated = 8;
	private AnimationInstance _animationInside;
	private AnimationInstance _animationRobotnik;
	private AnimationInstance _animationBody;
	private AnimationInstance _animationLights;
	private AnimationInstance _animationHighlightsSpawnIntro;
	private AnimationInstance _animationHighlightsIntro;
	private AnimationInstance _animationHighlightsEnd;
	private AnimationInstance _animationHighlightsLava;
	private Dictionary<int, Vector2i> _brokenPartsPositions = new Dictionary<int, Vector2i>();
	private Dictionary<int, int> _brokenPartsIndexOrder = new Dictionary<int, int>();
	private AnimationInstance _brokenPartsA;
	private AnimationInstance _brokenPartsB;
	private AnimationInstance _brokenPartsC;
	private AnimationInstance _brokenPartsD;
	private AnimationInstance _brokenPartsE;
	private int _leftX = -1024;
	private int _rightX;
	private int _leftPeekY = -416;
	private int _leftFireballY = -288;
	private int _leftSpawnY = 128 /*0x80*/;
	private int _rightPeekY = -528;
	private int _rightFireballY = -224;
	private int _rightSpawnY;
	private Vector2i _startPosition;
	private Vector2 _nonHoverPosition;
	private SubmarineEggmanInstance.State _state;
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
		AnimationGroup loadedResource1 = resourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		AnimationGroup loadedResource2 = resourceTree.GetLoadedResource<AnimationGroup>(Type, "SONICORCA/OBJECTS/ROBOTNIK/ANIGROUP");
		_animationInside = new AnimationInstance(loadedResource1);
		_animationRobotnik = new AnimationInstance(loadedResource2);
		_animationBody = new AnimationInstance(loadedResource1, 1);
		_animationLights = new AnimationInstance(loadedResource1, 4);
		_animationHighlightsSpawnIntro = new AnimationInstance(loadedResource1, 8);
		_animationHighlightsIntro = new AnimationInstance(loadedResource1, 9);
		_animationHighlightsEnd = new AnimationInstance(loadedResource1, 10);
		_animationHighlightsLava = new AnimationInstance(loadedResource1, 11);
		_brokenPartsPositions[12] = new Vector2i(-20, -64);
		_brokenPartsPositions[14] = new Vector2i(-8, -88);
		_brokenPartsPositions[15] = new Vector2i(-16, -112);
		_brokenPartsPositions[13] = new Vector2i(-44, -112);
		_brokenPartsPositions[16 /*0x10*/] = new Vector2i(-80, -112);
		_brokenPartsIndexOrder[0] = 16 /*0x10*/;
		_brokenPartsIndexOrder[1] = 13;
		_brokenPartsIndexOrder[2] = 15;
		_brokenPartsIndexOrder[3] = 14;
		_brokenPartsIndexOrder[4] = 12;
		_animationGroup = loadedResource1;
		_brokenPartsA = new AnimationInstance(_animationGroup, 12);
		_brokenPartsB = new AnimationInstance(_animationGroup, 13);
		_brokenPartsC = new AnimationInstance(_animationGroup, 14);
		_brokenPartsD = new AnimationInstance(_animationGroup, 15);
		_brokenPartsE = new AnimationInstance(_animationGroup, 16 /*0x10*/);
		ExplosionResourceKey = Type.GetAbsolutePath("SONICORCA/OBJECTS/EXPLOSION/BOSS");
		HitSoundResourceKey = Type.GetAbsolutePath("SONICORCA/SOUND/BOSSHIT");
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -100, -100, 200, 200)
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
		_velocity = new Vector2(0.0, -3.5);
		_isRightPit = true;
		_state = SubmarineEggmanInstance.State.Raise;
		Health = 8;
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
			EmitSmoke((Vector2)new Vector2i(Level.Random.Next(sbyte.MinValue, 128 /*0x80*/), Level.Random.Next(-64, -32)), new Vector2(0.0, -4.0));
		switch (_state)
		{
			case SubmarineEggmanInstance.State.Raise:
				PositionPrecise = PositionPrecise + _velocity;
				if (Position.Y > (_isRightPit ? _rightPeekY : _leftPeekY))
					break;
				_velocity.Y = 0.0;
				_state = SubmarineEggmanInstance.State.PrepareFlameThrower;
				_stateTimer = 60;
				_nonHoverPosition = PositionPrecise;
				_floatOffsetAngle = 0.0;
				break;
			case SubmarineEggmanInstance.State.PrepareFlameThrower:
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
				_state = SubmarineEggmanInstance.State.Hover;
				break;
			case SubmarineEggmanInstance.State.Hover:
				--_stateTimer;
				if (_stateTimer > 0)
				{
					Hover();
					break;
				}
				_currentHighlight = _animationHighlightsEnd;
				_velocity = new Vector2(0.0, 3.5);
				_state = SubmarineEggmanInstance.State.Lower;
				break;
			case SubmarineEggmanInstance.State.Lower:
				PositionPrecise = PositionPrecise + _velocity;
				if (Position.Y > (_isRightPit ? _rightFireballY : _leftFireballY) && !_firedFireball)
				{
					CreateFireball(-1);
					CreateFireball(1);
					Level.SoundManager.PlaySound(Position, "SONICORCA/SOUND/FIREBALL");
					_firedFireball = true;
				}
				if (_isRightPit)
				{
					if (Position.Y < _rightSpawnY)
						break;
					Position = new Vector2i(Position.X, _rightSpawnY);
				}
				else
				{
					if (Position.Y < _leftSpawnY)
						break;
					Position = new Vector2i(Position.X, _leftSpawnY);
				}
				_velocity = new Vector2(0.0, -3.5);
				_state = SubmarineEggmanInstance.State.Raise;
				_firedFireball = false;
				ICharacter protagonist = Level.Player.Protagonist;
				if (protagonist.Position.X < _startPosition.X - 512 /*0x0200*/)
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
				break;
			case SubmarineEggmanInstance.State.Defeated:
				CleanHighlights();
				--_stateTimer;
				if (_stateTimer > 0)
				{
					if (_stateTimer % 16 /*0x10*/ == 0 && _brokenPartsCounter < 5)
					{
						int num = _brokenPartsIndexOrder[_brokenPartsCounter];
						ReleaseFragmentMetal(Position + _brokenPartsPositions[num], 6.0, num);
						switch (num)
						{
							case 12:
								_brokenPartsA = null;
								break;
							case 13:
								_brokenPartsB = null;
								break;
							case 14:
								_brokenPartsC = null;
								break;
							case 15:
								_brokenPartsD = null;
								break;
							case 16 /*0x10*/:
								_brokenPartsE = null;
								break;
						}
						++_brokenPartsCounter;
					}
					UpdateExplosions(128 /*0x80*/);
					if (_stateTimer >= 30)
						break;
					UpdateSmoke();
					break;
				}
				_animationBody.Index = 3;
				_animationRobotnik.Index = 0;
				UpdateSmoke();
				if (_stateTimer > -60)
					break;
				Defeated = true;
				Fleeing = true;
				LockLifetime = false;
				Position = Position + new Vector2i(0, 8);
				break;
		}
	}

	private void EmitSmoke(Vector2 offset, Vector2 velocity)
	{
		Matrix4 matrix4 = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
		if (_flipX)
			matrix4 *= Matrix4.CreateScale(-1.0, 1.0);
		Vector2i position = (Vector2i)((matrix4 * Matrix4.CreateTranslation(offset)).RotateZ(0.0) * new Vector2(0.0, 100.0));
		DustInstance dustInstance = Level.ObjectManager.AddObject(new ObjectPlacement("SONICORCA/OBJECTS/DUST", Level.Map.Layers.IndexOf(Layer), position)) as DustInstance;
		dustInstance.SetDustAnimationIndex(1);
		dustInstance.Velocity = velocity;
	}

	private void Hover()
	{
		PositionPrecise = _nonHoverPosition + new Vector2(0.0, Math.Sin(_floatOffsetAngle) / Math.PI * 16.0);
		_floatOffsetAngle += Math.PI / 64.0;
	}

	private void CreateFlameThrowerFlame()
	{
		SubmarineEggmanInstance.FlameThrower flameThrower = Level.ObjectManager.AddSubObject<SubmarineEggmanInstance.FlameThrower>(this);
		flameThrower.FlipX = _flipX;
		flameThrower.Position = Position + new Vector2i(_flipX ? 132 : -132, -121);
	}

	private void CreateFlameThrowerShot()
	{
		SubmarineEggmanInstance.FlameThrowerShot flameThrowerShot = Level.ObjectManager.AddSubObject<SubmarineEggmanInstance.FlameThrowerShot>(this);
		flameThrowerShot.FlipX = _flipX;
		flameThrowerShot.Position = Position + new Vector2i(_flipX ? 448 : -448, -112);
		flameThrowerShot.Velocity = new Vector2i(_flipX ? 16 /*0x10*/ : -16, 0);
	}

	private void CreateFireball(int direction)
	{
		SubmarineEggmanInstance.Fireball fireball = Level.ObjectManager.AddSubObject<SubmarineEggmanInstance.Fireball>(this);
		fireball.IsMainFireball = true;
		fireball.SpawnDirection = (SubmarineEggmanInstance.Fireball.FireSpawn)direction;
		fireball.Position = Position;
		if (_isRightPit)
		{
			if (direction == 1)
			{
				fireball.Velocity = new Vector2(7.0, -25.0);
			}
			else
			{
				if (direction != -1)
					return;
				fireball.Velocity = new Vector2(-6.85, -25.0);
			}
		}
		else if (direction == 1)
		{
			fireball.Velocity = new Vector2(6.7, -21.0);
		}
		else
		{
			if (direction != -1)
				return;
			fireball.Velocity = new Vector2(-7.0, -21.0);
		}
	}

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (character.Velocity.Y < -15.125)
			character.Velocity = new Vector2(character.Velocity.X, -15.125);
		if (Health <= 0)
			return;
		_animationRobotnik.Index = 1;
	}

	protected override void Defeat()
	{
		base.Defeat();
		_state = SubmarineEggmanInstance.State.Defeated;
		_stateTimer = 179;
		_animationBody.Index = 2;
		_animationRobotnik.Index = 2;
		CollisionRectangles = new CollisionRectangle[0];
		Level.Player.GainScore(1000);
		Level.CreateScoreObject(1000, Position + new Vector2i(0, sbyte.MinValue));
	}

	private void UpdateSmoke()
	{
		if (Level.Random.Next(15) != 0)
			return;
		EmitSmoke((Vector2)new Vector2i(Level.Random.Next(sbyte.MinValue, 128 /*0x80*/), Level.Random.Next(-64, -32)), new Vector2(0.0, -4.0));
	}

	private void ReleaseFragmentMetal(Vector2i position, double speed, int index, bool explode = true)
	{
		double num1 = Level.Random.NextRadians();
		Vector2 velocity = new Vector2(speed * Math.Cos(num1), speed * Math.Sin(num1));
		ReleaseFragmentMetal(position, velocity, index);
		int num2 = explode ? 1 : 0;
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
		objectRenderer.Render(_animationRobotnik, (Vector2)new Vector2i(_flipX ? 32 /*0x20*/ : -32, -42), _flipX);
		if (IsInvincibleFlashing)
			objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
		objectRenderer.Render(_animationBody, _flipX);
		objectRenderer.EmitsLight = true;
		objectRenderer.Render(_animationLights, _flipX);
		if (_currentHighlight != null)
			objectRenderer.Render(_currentHighlight, (Vector2)new Vector2i(0, 0), _flipX);
		objectRenderer.Render(_animationHighlightsLava, (Vector2)new Vector2i(0, 0), _flipX);
		if (_state != SubmarineEggmanInstance.State.Defeated)
			return;
		if (_brokenPartsA != null)
		{
			Vector2i brokenPartsPosition = _brokenPartsPositions[12];
			objectRenderer.Render(_brokenPartsA, (Vector2)brokenPartsPosition, _flipX);
		}
		if (_brokenPartsC != null)
		{
			Vector2i brokenPartsPosition = _brokenPartsPositions[14];
			objectRenderer.Render(_brokenPartsC, (Vector2)brokenPartsPosition, _flipX);
		}
		if (_brokenPartsD != null)
		{
			Vector2i brokenPartsPosition = _brokenPartsPositions[15];
			objectRenderer.Render(_brokenPartsD, (Vector2)brokenPartsPosition, _flipX);
		}
		if (_brokenPartsB != null)
		{
			Vector2i brokenPartsPosition = _brokenPartsPositions[13];
			objectRenderer.Render(_brokenPartsB, (Vector2)brokenPartsPosition, _flipX);
		}
		if (_brokenPartsE == null)
			return;
		Vector2i brokenPartsPosition1 = _brokenPartsPositions[16 /*0x10*/];
		objectRenderer.Render(_brokenPartsE, (Vector2)brokenPartsPosition1, _flipX);
	}

	private enum State
	{
		Raise,
		PrepareFlameThrower,
		Hover,
		Lower,
		Defeated,
	}

	private class FlameThrower : ParticleObject
	{
		public FlameThrower()
		  : base("/ANIGROUP", 5)
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
			if (_animationInstance.CurrentFrameIndex == 35)
			{
				if (CollisionRectangles != null && CollisionRectangles.Length != 0)
					return;
				int x = (int)((FlipX ? -_animationInstance.CurrentFrame.Offset.X : _animationInstance.CurrentFrame.Offset.X) - _animationInstance.CurrentFrame.Source.Width / 2.0);
				Animation.Frame currentFrame = _animationInstance.CurrentFrame;
				double y1 = currentFrame.Offset.Y;
				currentFrame = _animationInstance.CurrentFrame;
				Rectanglei source = currentFrame.Source;
				double num = source.Height / 4.0;
				int y2 = (int)(y1 - num);
				currentFrame = _animationInstance.CurrentFrame;
				source = currentFrame.Source;
				int width = source.Width;
				currentFrame = _animationInstance.CurrentFrame;
				source = currentFrame.Source;
				int height = source.Height / 2;
				CollisionRectangles = new CollisionRectangle[1]
				{
		  new CollisionRectangle( this, 0, x, y2, width, height)
				};
			}
			else
			{
				if (_animationInstance.CurrentFrameIndex != 50 || CollisionRectangles == null && CollisionRectangles.Length == 0)
					return;
				CollisionRectangles = new CollisionRectangle[0];
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (_animationInstance.CurrentFrameIndex >= 35)
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

	private class FlameThrowerShot : Enemy
	{
		private AnimationInstance _animation;

		public bool FlipX { get; set; }

		public Vector2i Velocity { get; set; }

		protected override void OnStart()
		{
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 6);
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/)
			};
		}

		protected override void OnUpdate() => Position = Position + Velocity;

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation, FlipX);
		}
	}

	private class Fireball : Enemy
	{
		private const double Gravity = 0.875;
		private AnimationInstance _currentAnimation;
		private AnimationInstance _fireball;
		private AnimationInstance _fireballTransition;
		private int _ticks = 10;
		private int _fireInstances;
		private SubmarineEggmanInstance.Fireball.FireSpawn _spawnDirection = SubmarineEggmanInstance.Fireball.FireSpawn.Left;

		public Vector2 Velocity { get; set; }

		public bool IsMainFireball { get; set; }

		public SubmarineEggmanInstance.Fireball.FireSpawn SpawnDirection
		{
			get => _spawnDirection;
			set => _spawnDirection = value;
		}

		protected override void OnStart()
		{
			_fireball = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 7);
			_fireballTransition = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 17);
			_currentAnimation = _fireball;
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -32, -32, 64 /*0x40*/, 64 /*0x40*/)
			};
		}

		protected override void OnUpdate()
		{
			PositionPrecise = PositionPrecise + Velocity;
			Velocity += new Vector2(0.0, 0.875);
			if (!IsTouchingGround())
				return;
			Velocity = new Vector2(0.0, 0.0);
			if (_currentAnimation != _fireballTransition)
				_currentAnimation = _fireballTransition;
			if (!IsMainFireball)
				return;
			++_ticks;
			if (_ticks == 15)
				CreateFire();
			else if (_ticks == 20)
				CreateFire();
			else if (_ticks == 25)
			{
				CreateFire();
			}
			else
			{
				if (_ticks != 29)
					return;
				CreateFire();
			}
		}

		private void CreateFire()
		{
			SubmarineEggmanInstance.Fire fire = Level.ObjectManager.AddSubObject<SubmarineEggmanInstance.Fire>(ParentObject);
			fire.Position = Position + new Vector2i(_fireInstances * fire.DesignBounds.Width * (int)SpawnDirection, 0);
			fire.BubbleLeftCount = 4;
			fire.BubbleRightCount = 4;
			fire.Priority = 1024 /*0x0400*/;
			fire.Layer = Level.Map.Layers[Level.Map.Layers.Count - 1];
			Level.SoundManager.PlaySound(Position, Type.GetAbsolutePath("SONICORCA/SOUND/FIREBURN"));
			++_fireInstances;
		}

		private bool IsTouchingGround()
		{
			bool flag = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 32 /*0x20*/))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					flag = true;
				}
			}
			return flag;
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
				bool flipX = SpawnDirection == SubmarineEggmanInstance.Fireball.FireSpawn.Right;
				objectRenderer.Render(_currentAnimation, new Vector2(flipX ? 80.0 : -80.0, -8.0), flipX);
			}
			else
			{
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
			Left = -1, // 0xFFFFFFFF
			Right = 1,
		}
	}

	private class Fire : Enemy
	{
		private AnimationInstance _currentAnimation;
		private AnimationInstance _fireballIntro;
		private AnimationInstance _fireballEnd;
		private int _timer;

		public int BubbleLeftCount { get; set; }

		public int BubbleRightCount { get; set; }

		protected override void OnStart()
		{
			_fireballIntro = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 18);
			_fireballEnd = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 19);
			_currentAnimation = _fireballIntro;
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -32, -32, 64 /*0x40*/, 64 /*0x40*/)
			};
			_timer = 74;
		}

		protected override void OnUpdate()
		{
			--_timer;
			if (_timer == 66)
			{
				if (BubbleLeftCount > 0)
					BubbleFire(Position + new Vector2i(-64, 0), BubbleLeftCount--, 0);
				if (BubbleRightCount <= 0)
					return;
				BubbleFire(Position + new Vector2i(64 /*0x40*/, 0), 0, BubbleRightCount--);
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
			SubmarineEggmanInstance.Fire fire = Level.ObjectManager.AddSubObject<SubmarineEggmanInstance.Fire>(ParentObject);
			fire.Position = bubblePosition;
			fire.BubbleLeftCount = leftCount;
			fire.BubbleRightCount = rightCount;
		}

		private bool IsGroundBelow(ref Vector2i postion)
		{
			bool flag = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 32 /*0x20*/))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					postion.Y += (int)collision.Shift;
					flag = true;
				}
			}
			return flag;
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
