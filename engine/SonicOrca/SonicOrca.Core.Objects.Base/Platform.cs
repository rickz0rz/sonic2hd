using System;
using System.Linq;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The base class for a platform. It can move on an oscillating path, sag when a character stands on it, or fall.
/// </summary>
public class Platform : ActiveObject, IPlatform, IActiveObject
{
	/// <summary>
	/// The number of ticks that a platform waits after a character stands on it before it falls.
	/// </summary>
	private const int FallingDelay = 30;

	private const double FallingGravity = 0.875;

	/// <summary>
	/// The maximum number of pixels that a platform sags when a character stands on it.
	/// </summary>
	private const int MaximumSagOffset = 16;

	private const int SagSpeed = 2;

	private Vector2i _initialPosition;

	private int _sagOffset;

	private bool _fallWhenStoodOn;

	private bool _falling;

	private int _fallingDelay;

	private Vector2 _fallingVelocity;

	protected Vector2 _velocityBasedOnNextPosition;

	protected Vector2 _nextPositionPrecise { get; set; }

	[StateVariable]
	public Vector2 MovementRadius { get; protected set; }

	[StateVariable]
	protected int TimePeriod { get; set; }

	[StateVariable]
	protected int TimeOffset { get; set; }

	[StateVariable]
	protected bool FallWhenStoodOn
	{
		get
		{
			return _fallWhenStoodOn;
		}
		set
		{
			_fallWhenStoodOn = value;
		}
	}

	protected bool SagWhenStoodOn { get; set; }

	protected bool Linear { get; set; }

	public int CurrentTime { get; set; }

	public double CurrentT { get; set; }

	public Vector2 Velocity => _velocityBasedOnNextPosition;

	public Ellipse Area { get; set; }

	protected override void OnStart()
	{
		_initialPosition = Position;
		_fallingDelay = FallingDelay;
	}

	protected override void OnUpdatePrepare()
	{
		// Find the next position now, so that the velocity is known before the characters update.
		// The platform moves to the next position in OnUpdate.
		Vector2 currentPosition = PositionPrecise;
		UpdateMovement();
		_nextPositionPrecise = PositionPrecise;
		_velocityBasedOnNextPosition = _nextPositionPrecise - currentPosition;
		PositionPrecise = currentPosition;
	}

	protected override void OnUpdate()
	{
		PositionPrecise = _nextPositionPrecise;
	}

	protected virtual void UpdateMovement()
	{
		UpdateTime();
		if (_falling)
		{
			if (_fallingDelay > 0)
			{
				_fallingDelay--;
				return;
			}
			_fallingVelocity.Y += FallingGravity;
			PositionPrecise += _fallingVelocity;
			return;
		}
		UpdateOscillationPosition();
		if (!IsCharacterInteractingWithPlatform())
		{
			if (MovementRadius.Y == 0.0 && !_fallWhenStoodOn && SagWhenStoodOn)
			{
				_sagOffset = Math.Max(0, _sagOffset - SagSpeed);
			}
		}
		else
		{
			if (MovementRadius.Y == 0.0 && !_fallWhenStoodOn && SagWhenStoodOn)
			{
				_sagOffset = Math.Min(MaximumSagOffset, _sagOffset + SagSpeed);
			}
			if (_fallWhenStoodOn)
			{
				_falling = true;
			}
		}
		PositionPrecise += new Vector2(0.0, _sagOffset);
	}

	private void UpdateTime()
	{
		if (TimePeriod > 0)
		{
			CurrentTime = (Level.Ticks + TimeOffset) % TimePeriod;
			CurrentT = CurrentTime / (double)TimePeriod;
		}
		else
		{
			CurrentTime = 0;
			CurrentT = 0.0;
		}
	}

	private void UpdateOscillationPosition()
	{
		if (TimePeriod == 0)
		{
			return;
		}
		double t = CurrentT;
		double factorX;
		double factorY;
		if (Linear)
		{
			// Triangle wave: 0 to 1, back to -1, and then back to 0.
			if (t <= 0.25)
			{
				t /= 0.25;
			}
			else if (t <= 0.5)
			{
				t = 1.0 - (t - 0.25) / 0.25;
			}
			else if (t <= 0.75)
			{
				t = 0.0 - (t - 0.5) / 0.25;
			}
			else
			{
				t = -1.0 + (t - 0.75) / 0.25;
			}
			factorX = t;
			factorY = t;
		}
		else
		{
			double angle = t * (Math.PI * 2.0);
			if (MovementRadius.X == 0.0 || MovementRadius.Y == 0.0)
			{
				// Movement on one axis only.
				factorX = Math.Sin(angle);
				factorY = Math.Sin(angle);
			}
			else
			{
				// Movement on a circle or an ellipse.
				factorX = Math.Cos(angle);
				factorY = Math.Sin(angle);
			}
		}
		PositionPrecise = new Vector2(_initialPosition.X + factorX * MovementRadius.X, _initialPosition.Y + factorY * MovementRadius.Y + _sagOffset);
	}

	protected bool IsCharacterInteractingWithPlatform()
	{
		return Level.ObjectManager.Characters.Any(x => x.ObjectLink == this);
	}
}
