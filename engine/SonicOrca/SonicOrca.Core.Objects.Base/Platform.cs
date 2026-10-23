using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Objects.Base;

public class Platform : ActiveObject, IPlatform, IActiveObject
{
	private readonly List<ICharacter> _charactersOnPlatform = new List<ICharacter>();

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
		_fallingDelay = 30;
	}

	protected override void OnUpdatePrepare()
	{
		Vector2 positionPrecise = PositionPrecise;
		UpdateMovement();
		_nextPositionPrecise = PositionPrecise;
		_velocityBasedOnNextPosition = _nextPositionPrecise - positionPrecise;
		PositionPrecise = positionPrecise;
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
			_fallingVelocity.Y += 0.875;
			PositionPrecise += _fallingVelocity;
			return;
		}
		UpdateOscillationPosition();
		if (!IsCharacterInteractingWithPlatform())
		{
			if (MovementRadius.Y == 0.0 && !_fallWhenStoodOn && SagWhenStoodOn)
			{
				_sagOffset = Math.Max(0, _sagOffset - 2);
			}
		}
		else
		{
			if (MovementRadius.Y == 0.0 && !_fallWhenStoodOn && SagWhenStoodOn)
			{
				_sagOffset = Math.Min(16, _sagOffset + 2);
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
		double currentT = CurrentT;
		double num;
		double num2;
		if (Linear)
		{
			if (currentT <= 0.25)
			{
				currentT /= 0.25;
			}
			else if (currentT <= 0.5)
			{
				currentT = 1.0 - (currentT - 0.25) / 0.25;
			}
			else
			{
				currentT = ((!(currentT <= 0.75)) ? (-1.0 + (currentT - 0.75) / 0.25) : (0.0 - (currentT - 0.5) / 0.25));
			}
			num = currentT;
			num2 = currentT;
		}
		else
		{
			double num3 = currentT * (Math.PI * 2.0);
			if (MovementRadius.X == 0.0 || MovementRadius.Y == 0.0)
			{
				num = Math.Sin(num3);
				num2 = Math.Sin(num3);
			}
			else
			{
				num = Math.Cos(num3);
				num2 = Math.Sin(num3);
			}
		}
		PositionPrecise = new Vector2(_initialPosition.X + num * MovementRadius.X, _initialPosition.Y + num2 * MovementRadius.Y + _sagOffset);
	}

	protected bool IsCharacterInteractingWithPlatform()
	{
		return Level.ObjectManager.Characters.Any((ICharacter x) => x.ObjectLink == this);
	}
}
