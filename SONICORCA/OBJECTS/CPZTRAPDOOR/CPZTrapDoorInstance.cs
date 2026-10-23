// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZTRAPDOOR.CPZTrapDoorInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZTRAPDOOR;

/// <summary>
/// A trap door in Chemical Plant Zone. It opens and closes on a fixed cycle of 256 frames.
/// It is shut for the first half of the cycle and open for the second half. Characters fall through it while it is open.
/// </summary>
public class CPZTrapDoorInstance : ActiveObject
{
	private const int AnimationClosed = 0;
	private const int AnimationOpen = 1;
	private const int AnimationClosedToOpen = 2;
	private const int AnimationOpenToClosed = 3;
	private const int CycleDuration = 256;
	private const int OpenTime = 128;
	private AnimationInstance _animation;
	private int _closeAnimationLength;
	private int _timeOffset;
	private bool _open;

	[StateVariable]
	private int TimeOffset
	{
		get => _timeOffset;
		set => _timeOffset = value;
	}

	public CPZTrapDoorInstance()
	{
		DesignBounds = new Rectanglei(-64, -64, 128, 128);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_closeAnimationLength = _animation.AnimationGroup[AnimationOpenToClosed].Duration;
		int cycleTime = (Level.Ticks + _timeOffset) % CycleDuration;
		_open = cycleTime >= OpenTime;
		// Start the open or close animation at the correct frame, if the door is in one of them now.
		if (cycleTime >= CycleDuration - _closeAnimationLength)
		{
			_animation.Index = AnimationOpenToClosed;
			_animation.Seek(cycleTime - (CycleDuration - _closeAnimationLength));
		}
		else if (cycleTime >= OpenTime)
		{
			_animation.Index = AnimationClosedToOpen;
			_animation.Seek(cycleTime - OpenTime);
		}
		CollisionVectors = new CollisionVector[1]
		{
			new CollisionVector(this, new Vector2i(-64, -64), new Vector2i(64, -64))
		};
	}

	protected override void OnUpdate()
	{
		int cycleTime = (Level.Ticks + _timeOffset) % CycleDuration;
		_open = cycleTime >= OpenTime;
		if (cycleTime == OpenTime)
		{
			_animation.Index = AnimationClosedToOpen;
		}
		else if (cycleTime == CycleDuration - _closeAnimationLength)
		{
			_animation.Index = AnimationOpenToClosed;
		}
		if (_open)
		{
			foreach (CollisionVector collisionVector in CollisionVectors)
			{
				if (!collisionVector.Flags.HasFlag(CollisionFlags.Ignore))
				{
					collisionVector.Flags |= CollisionFlags.Ignore;
				}
			}
		}
		else
		{
			foreach (CollisionVector collisionVector in CollisionVectors)
			{
				if (collisionVector.Flags.HasFlag(CollisionFlags.Ignore))
				{
					collisionVector.Flags &= ~CollisionFlags.Ignore;
				}
			}
		}
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
