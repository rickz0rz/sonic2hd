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

namespace SONICORCA.OBJECTS.HTZSEESAW;

/// <summary>
/// The see-saw from Hill Top Zone, with the Sol fireball badnik on one end. A character that tilts the see-saw throws Sol
/// into the air. When Sol lands on the other end, it launches the characters on the opposite side.
/// </summary>
public class HTZSeeSawInstance : ActiveObject
{
	/// <summary>The rotation of the platform for each bias (-1, 0 and 1).</summary>
	private readonly IReadOnlyList<double> VisualAngles = new double[3]
	{
		-1.0 * Math.PI / 8.0,
		0.0,
		Math.PI / 8.0
	};

	/// <summary>The Y position of the left end of the collision vector for each bias (-1, 0 and 1).</summary>
	private static readonly int[] LeftEndYOffsets = new int[3] { 80, 4, -64 };

	/// <summary>The Y position of the right end of the collision vector for each bias (-1, 0 and 1).</summary>
	private static readonly int[] RightEndYOffsets = new int[3] { -64, 4, 80 };

	private const int AnimationStand = 0;
	private const int AnimationPlatform = 1;

	/// <summary>The half width of the middle part of the platform.</summary>
	private const int MiddleHalfWidth = 32;

	private const int SolOffsetX = 160;
	private const int SolOffsetY = 32;

	private AnimationInstance _animationStand;
	private AnimationInstance _animationPlatform;
	private readonly List<ICharacter> _charactersOnLeftSide = new List<ICharacter>();
	private readonly List<ICharacter> _charactersOnMiddleSide = new List<ICharacter>();
	private readonly List<ICharacter> _charactersOnRightSide = new List<ICharacter>();

	/// <summary>The side that is down: -1 is left, 0 is level, 1 is right.</summary>
	private int _bias;

	private double _visualAngle;
	private Sol _sol;
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
			return _charactersOnLeftSide.Concat(_charactersOnMiddleSide).Concat(_charactersOnRightSide);
		}
	}

	public HTZSeeSawInstance() => DesignBounds = new Rectanglei(-216, -132, 432, 264);

	protected override void OnStart()
	{
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		_animationStand = new AnimationInstance(animationGroup, AnimationStand);
		_animationPlatform = new AnimationInstance(animationGroup, AnimationPlatform);
		CollisionVectors = new CollisionVector[1]
		{
			new CollisionVector(this, new Vector2i(), new Vector2i(), flags: CollisionFlags.Conveyor | CollisionFlags.NoAngle | CollisionFlags.NoBalance | CollisionFlags.NoAutoAlignment)
		};
		_bias = FlipX ? -1 : 1;
		_visualAngle = VisualAngles[_bias + 1];
		_sol = Level.ObjectManager.AddSubObject<Sol>(this);
		_sol.Position = Position + new Vector2i(SolOffsetX * _bias, SolOffsetY);
		_sol.Side = _bias;
		_sol.LockLifetime = true;
		Priority = -256;
		ShadowInfo.MaxShadowOffset = new Vector2i(16);
	}

	protected override void OnUpdateEditor()
	{
		base.OnUpdateEditor();
		_sol.Position = Position + new Vector2i(SolOffsetX * _bias, SolOffsetY);
	}

	protected override void OnUpdate()
	{
		if (_charactersOnLeftSide.Count != 0 || _charactersOnMiddleSide.Count != 0 || _charactersOnRightSide.Count != 0)
		{
			int newBias = Math.Sign(_charactersOnRightSide.Count - _charactersOnLeftSide.Count);
			if (newBias != _bias)
			{
				_bias = newBias;
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
				// These side codes (left -1, right 0, middle 1) do not match the codes of Sol.Side (-1 and 1).
				int side;
				if (standingCharacter.Position.X < Position.X - MiddleHalfWidth)
				{
					side = -1;
					_charactersOnLeftSide.Add(standingCharacter);
				}
				else if (standingCharacter.Position.X > Position.X + MiddleHalfWidth)
				{
					side = 0;
					_charactersOnRightSide.Add(standingCharacter);
				}
				else
				{
					side = 1;
					_charactersOnMiddleSide.Add(standingCharacter);
				}
				if (!_sol.Flying && side != _sol.Side)
					_maxCharacterLandVelocity = Math.Max(_maxCharacterLandVelocity, standingCharacter.Velocity.Y);
			}
		}
		if (_bias < -1 || _bias > 1)
			return;
		CollisionVector collisionVector = CollisionVectors[0];
		collisionVector.RelativeA = new Vector2i(-192, LeftEndYOffsets[_bias + 1]);
		collisionVector.RelativeB = new Vector2i(192, RightEndYOffsets[_bias + 1]);
		RegisterCollisionUpdate();
		// Keep the characters on the surface after the platform tilts.
		foreach (ICharacter character in CharactersOnObject)
		{
			if (GetYIntersectAt(character.Position.X, out int y))
				character.Position = new Vector2i(character.Position.X, y - character.CollisionRadius.Y - 4);
		}
	}

	/// <summary>Gets the Y position of the platform surface at the given X position.</summary>
	private bool GetYIntersectAt(int x, out int y)
	{
		if (!CollisionVector.GetIntersection(CollisionVectors[0], new CollisionVector(new Vector2i(x, Position.Y - 512), new Vector2i(x, Position.Y + 512)), out Vector2 intersection))
		{
			y = 0;
			return false;
		}
		y = (int)intersection.Y - 1;
		return true;
	}

	/// <summary>Throws Sol into the air when the see-saw tilts to a side that Sol is not on.</summary>
	private void OnCharacterTouch(int side, double yVelocity)
	{
		if (_sol.Flying || side == _sol.Side)
			return;
		Vector2 launchVelocity = new Vector2(69.0 / 16.0, 32.375);
		if (side != 0)
		{
			launchVelocity = new Vector2(51.0 / 16.0, 43.75);
			// The original (Obj14) gives the big launch at a landing y_vel of $A00 (40 units) or more.
			if (Level.OriginalPhysics ? yVelocity >= 40.0 : yVelocity > 36.0)
				launchVelocity = new Vector2(2.5, 56.0);
		}
		if (_sol.Side == 1)
			launchVelocity.X *= -1.0;
		launchVelocity.Y *= -1.0;
		_sol.Velocity = launchVelocity;
		_sol.Side = _sol.Side == 1 ? -1 : 1;
		_sol.Flying = true;
	}

	/// <summary>Launches the characters on the side opposite to where Sol landed, and on the middle.</summary>
	private void OnSolTouch(int side, double yVelocity)
	{
		IEnumerable<ICharacter> characters = side == -1 ? _charactersOnRightSide.Concat(_charactersOnMiddleSide) : _charactersOnLeftSide.Concat(_charactersOnMiddleSide);
		bool launched = false;
		foreach (ICharacter character in characters)
		{
			character.LeaveGround();
			character.IsAirborne = true;
			character.Velocity = new Vector2(character.Velocity.X, -yVelocity * _strengthMultiplier);
			character.TumbleAngle = 0.0;
			character.TumbleTurns = 1;
			character.GroundVelocity = 0.0;
			launched = true;
		}
		if (!launched)
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

	/// <summary>
	/// The Sol fireball badnik that sits on one end of the see-saw. It flies to the other end when a character tilts
	/// the see-saw.
	/// </summary>
	private class Sol : Enemy
	{
		private const int AnimationSol = 1;
		private const double Gravity = 0.875;

		/// <summary>The Y offset of Sol at rest. The index is the bias plus 1, plus 2 more when Sol is on the right.</summary>
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

		/// <summary>The end of the see-saw that Sol is on: -1 is left, 1 is right.</summary>
		public int Side { get; set; }

		public bool Flying { get; set; }

		public HTZSeeSawInstance SeeSaw => (HTZSeeSawInstance)ParentObject;

		protected override void OnStart()
		{
			base.OnStart();
			CollisionRectangles = new CollisionRectangle[1]
			{
				new CollisionRectangle(this, 0, -36, -36, 72, 72)
			};
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("SONICORCA/OBJECTS/SOL/ANIGROUP"), AnimationSol);
			ShadowInfo.MaxShadowOffset = new Vector2i(16, 16);
		}

		protected override void OnUpdate()
		{
			if (Level.Player.Protagonist != null)
				_facingRight = Level.Player.Protagonist.Position.X >= Position.X;
			int index = SeeSaw._bias + 1;
			if (Side == 1)
				index += 2;
			int restY = SeeSaw.Position.Y + 64 + SolRestYOffsets[index];
			if (!Flying)
			{
				Position = new Vector2i(Position.X, restY);
			}
			else
			{
				PositionPrecise += Velocity;
				Velocity += new Vector2(0.0, Gravity);
				if (Velocity.Y > 0.0 && Position.Y >= restY)
				{
					Position = new Vector2i(Position.X, restY);
					Flying = false;
					SeeSaw._bias = Side;
					SeeSaw.OnSolTouch(Side, Velocity.Y);
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
