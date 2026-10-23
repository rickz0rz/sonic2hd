// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZSEESAW.HTZSeeSawInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.OBJECTS.HTZSEESAW;

public class HTZSeeSawInstance : ActiveObject
{
	private readonly IReadOnlyList<double> VisualAngles = new double[3]
	{
	-1.0 * Math.PI / 8.0,
	0.0,
	Math.PI / 8.0
	};
	private const int AnimationStand = 0;
	private const int AnimationPlatform = 1;
	private AnimationInstance _animationStand;
	private AnimationInstance _animationPlatform;
	private readonly List<ICharacter> _charactersOnLeftSide = new List<ICharacter>();
	private readonly List<ICharacter> _charactersOnMiddleSide = new List<ICharacter>();
	private readonly List<ICharacter> _charactersOnRightSide = new List<ICharacter>();
	private int _bias;
	private double _visualAngle;
	private HTZSeeSawInstance.Sol _sol;
	private double _maxCharacterLandVelocity;
	private double _strengthMultiplier = 1.0;

	[StateVariable]
	private bool FlipX { get; set; }

	[StateVariable]
	private double StrengthMultiplier
	{
		get => _strengthMultiplier;
		set => _strengthMultiplier = value;
	}

	private IEnumerable<ICharacter> CharactersOnObject
	{
		get
		{
			return _charactersOnLeftSide.Concat<ICharacter>(_charactersOnMiddleSide).Concat<ICharacter>(_charactersOnRightSide);
		}
	}

	public HTZSeeSawInstance() => DesignBounds = new Rectanglei(-216, -132, 432, 264);

	protected override void OnStart()
	{
		AnimationGroup loadedResource = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		_animationStand = new AnimationInstance(loadedResource);
		_animationPlatform = new AnimationInstance(loadedResource, 1);
		CollisionVectors = new CollisionVector[1]
		{
	  new CollisionVector( this, new Vector2i(), new Vector2i(), flags: CollisionFlags.Conveyor | CollisionFlags.NoAngle | CollisionFlags.NoBalance | CollisionFlags.NoAutoAlignment)
		};
		_bias = FlipX ? -1 : 1;
		_visualAngle = VisualAngles[_bias + 1];
		_sol = Level.ObjectManager.AddSubObject<HTZSeeSawInstance.Sol>(this);
		_sol.Position = Position + new Vector2i(160 /*0xA0*/ * _bias, 32 /*0x20*/);
		_sol.Side = _bias;
		_sol.LockLifetime = true;
		Priority = -256;
		ShadowInfo.MaxShadowOffset = new Vector2i(16 /*0x10*/);
	}

	protected override void OnUpdateEditor()
	{
		base.OnUpdateEditor();
		_sol.Position = Position + new Vector2i(160 /*0xA0*/ * _bias, 32 /*0x20*/);
	}

	protected override void OnUpdate()
	{
		if (_charactersOnLeftSide.Count != 0 || _charactersOnMiddleSide.Count != 0 || _charactersOnRightSide.Count != 0)
		{
			int num = Math.Sign(_charactersOnRightSide.Count - _charactersOnLeftSide.Count);
			if (num != _bias)
			{
				_bias = num;
				OnCharacterTouch(_bias, _maxCharacterLandVelocity);
			}
		}
		_maxCharacterLandVelocity = 0.0;
	}

	protected override void OnStop()
	{
		base.OnStop();
		_sol.FinishForever();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		int classification = (int)e.ActiveObject.Type.Classification;
	}

	private List<ICharacter> GetStandingCharacters()
	{
		List<ICharacter> standingCharacters = new List<ICharacter>();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.GroundVector == CollisionVectors[0] && !character.IsAirborne)
				standingCharacters.Add(character);
		}
		return standingCharacters;
	}

	protected override void OnUpdateCollision()
	{
		_charactersOnLeftSide.Clear();
		_charactersOnRightSide.Clear();
		_charactersOnMiddleSide.Clear();
		foreach (ICharacter standingCharacter in GetStandingCharacters())
		{
			if (standingCharacter.Velocity.Y >= 0.0 || !standingCharacter.IsAirborne)
			{
				Vector2i position = standingCharacter.Position;
				int x1 = position.X;
				position = Position;
				int num1 = position.X - 32 /*0x20*/;
				int num2;
				if (x1 < num1)
				{
					num2 = -1;
					_charactersOnLeftSide.Add(standingCharacter);
				}
				else
				{
					position = standingCharacter.Position;
					int x2 = position.X;
					position = Position;
					int num3 = position.X + 32 /*0x20*/;
					if (x2 > num3)
					{
						num2 = 0;
						_charactersOnRightSide.Add(standingCharacter);
					}
					else
					{
						num2 = 1;
						_charactersOnMiddleSide.Add(standingCharacter);
					}
				}
				if (!_sol.Flying && num2 != _sol.Side)
					_maxCharacterLandVelocity = Math.Max(_maxCharacterLandVelocity, standingCharacter.Velocity.Y);
			}
		}
		if (_bias < -1 || _bias > 1)
			return;
		int[] numArray1 = new int[3] { 80 /*0x50*/, 4, -64 };
		int[] numArray2 = new int[3] { -64, 4, 80 /*0x50*/ };
		CollisionVector collisionVector = CollisionVectors[0];
		collisionVector.RelativeA = new Vector2i(-192, numArray1[_bias + 1]);
		collisionVector.RelativeB = new Vector2i(192 /*0xC0*/, numArray2[_bias + 1]);
		RegisterCollisionUpdate();
		foreach (ICharacter character in CharactersOnObject)
		{
			int y;
			if (GetYIntersectAt(character.Position.X, out y))
				character.Position = new Vector2i(character.Position.X, y - character.CollisionRadius.Y - 4);
		}
	}

	private bool GetYIntersectAt(int x, out int y)
	{
		Vector2 intersection;
		if (!CollisionVector.GetIntersection(CollisionVectors[0], new CollisionVector(new Vector2i(x, Position.Y - 512 /*0x0200*/), new Vector2i(x, Position.Y + 512 /*0x0200*/)), out intersection))
		{
			y = 0;
			return false;
		}
		y = (int)intersection.Y - 1;
		return true;
	}

	private void OnCharacterTouch(int side, double yVelocity)
	{
		if (_sol.Flying || side == _sol.Side)
			return;
		Vector2 vector2 = new Vector2(69.0 / 16.0, 32.375);
		if (side != 0)
		{
			vector2 = new Vector2(51.0 / 16.0, 43.75);
			if (yVelocity > 36.0)
				vector2 = new Vector2(2.5, 56.0);
		}
		if (_sol.Side == 1)
			vector2.X *= -1.0;
		vector2.Y *= -1.0;
		_sol.Velocity = vector2;
		_sol.Side = _sol.Side == 1 ? -1 : 1;
		_sol.Flying = true;
	}

	private void OnSolTouch(int side, double yVelocity)
	{
		IEnumerable<ICharacter> characters = side == -1 ? _charactersOnRightSide.Concat<ICharacter>(_charactersOnMiddleSide) : _charactersOnLeftSide.Concat<ICharacter>(_charactersOnMiddleSide);
		bool flag = false;
		foreach (ICharacter character in characters)
		{
			character.LeaveGround();
			character.IsAirborne = true;
			character.Velocity = new Vector2(character.Velocity.X, -yVelocity * _strengthMultiplier);
			character.TumbleAngle = 0.0;
			character.TumbleTurns = 1;
			character.GroundVelocity = 0.0;
			flag = true;
		}
		if (!flag)
			return;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/SPRING"));
	}

	private void AnimateVisualAngle()
	{
		_visualAngle = MathX.GoTowards(_visualAngle, VisualAngles[_bias + 1], Math.PI / 8.0);
	}

	protected override void OnAnimate()
	{
		AnimateVisualAngle();
		_animationStand.Animate();
		_animationPlatform.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (_visualAngle != 0.0)
				objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(_visualAngle);
			objectRenderer.Render(_animationPlatform);
		}
		objectRenderer.Render(_animationStand);
	}

	private class Sol : Enemy
	{
		private const int AnimationSol = 1;
		private const double Gravity = 0.875;
		private static readonly IReadOnlyList<int> SolRestYOffsets = new int[5]
		{
	  -32,
	  -112,
	  -188,
	  -112,
	  -32
		};
		private AnimationInstance _animation;
		private bool _facingRight;

		public Vector2 Velocity { get; set; }

		public int Side { get; set; }

		public bool Flying { get; set; }

		public HTZSeeSawInstance SeeSaw => (HTZSeeSawInstance)ParentObject;

		protected override void OnStart()
		{
			base.OnStart();
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -36, -36, 72, 72)
			};
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("SONICORCA/OBJECTS/SOL/ANIGROUP"), 1);
			ShadowInfo.MaxShadowOffset = new Vector2i(16 /*0x10*/, 16 /*0x10*/);
		}

		protected override void OnUpdate()
		{
			Vector2i position;
			if (Level.Player.Protagonist != null)
			{
				position = Level.Player.Protagonist.Position;
				int x1 = position.X;
				position = Position;
				int x2 = position.X;
				_facingRight = x1 >= x2;
			}
			int index = SeeSaw._bias + 1;
			if (Side == 1)
				index += 2;
			position = SeeSaw.Position;
			int y = position.Y + 64 /*0x40*/ + HTZSeeSawInstance.Sol.SolRestYOffsets[index];
			if (!Flying)
			{
				position = Position;
				Position = new Vector2i(position.X, y);
			}
			else
			{
				PositionPrecise = PositionPrecise + Velocity;
				Velocity += new Vector2(0.0, 0.875);
				if (Velocity.Y > 0.0)
				{
					position = Position;
					if (position.Y >= y)
					{
						position = Position;
						Position = new Vector2i(position.X, y);
						Flying = false;
						SeeSaw._bias = Side;
						SeeSaw.OnSolTouch(Side, Velocity.Y);
					}
				}
			}
			SeeSaw._charactersOnLeftSide.Clear();
			SeeSaw._charactersOnMiddleSide.Clear();
			SeeSaw._charactersOnRightSide.Clear();
		}

		protected override void OnAnimate()
		{
			base.OnAnimate();
			_animation.Animate();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			renderer.GetObjectRenderer().Render(_animation, _facingRight);
		}
	}
}
