// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZTRAPDOOR.CPZTrapDoorInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZTRAPDOOR;

public class CPZTrapDoorInstance : ActiveObject
{
	private const int AnimationClosed = 0;
	private const int AnimationOpen = 1;
	private const int AnimationClosedToOpen = 2;
	private const int AnimationOpenToClosed = 3;
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
		DesignBounds = new Rectanglei(-64, -64, 128 /*0x80*/, 128 /*0x80*/);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_closeAnimationLength = _animation.AnimationGroup[3].Duration;
		int num = (Level.Ticks + _timeOffset) % 256 /*0x0100*/;
		_open = num >= 128 /*0x80*/;
		if (num >= 256 /*0x0100*/ - _closeAnimationLength)
		{
			_animation.Index = 3;
			_animation.Seek(num - (256 /*0x0100*/ - _closeAnimationLength));
		}
		else if (num >= 128 /*0x80*/)
		{
			_animation.Index = 2;
			_animation.Seek(num - 128 /*0x80*/);
		}
		CollisionVectors = new CollisionVector[1]
		{
	  new CollisionVector( this, new Vector2i(-64, -64), new Vector2i(64 /*0x40*/, -64))
		};
	}

	protected override void OnUpdate()
	{
		int num = (Level.Ticks + _timeOffset) % 256 /*0x0100*/;
		_open = num >= 128 /*0x80*/;
		if (num == 128 /*0x80*/)
			_animation.Index = 2;
		else if (num == 256 /*0x0100*/ - _closeAnimationLength)
			_animation.Index = 3;
		if (_open)
		{
			foreach (CollisionVector collisionVector in CollisionVectors)
			{
				if (!collisionVector.Flags.HasFlag(CollisionFlags.Ignore))
					collisionVector.Flags |= CollisionFlags.Ignore;
			}
		}
		else
		{
			foreach (CollisionVector collisionVector in CollisionVectors)
			{
				if (collisionVector.Flags.HasFlag(CollisionFlags.Ignore))
					collisionVector.Flags &= ~CollisionFlags.Ignore;
			}
		}
	}

	protected override void OnCollision(CollisionEvent e) => base.OnCollision(e);

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
