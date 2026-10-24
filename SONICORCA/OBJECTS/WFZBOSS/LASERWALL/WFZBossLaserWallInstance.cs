using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZBOSS.LASERWALL;

/// <summary>
/// A laser wall of the Wing Fortress Zone boss (ObjC5, routine 4). It is a solid block 8 pixels to each side and $40
/// pixels up and down. It shows in every second frame. When the boss is defeated, it flickers with longer and longer
/// gaps, and then it goes (ObjC5_LaserWallDelete).
/// </summary>
public class WFZBossLaserWallInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 8;

	private const int HalfHeight = 0x40;

	private const int Frame = 0xC;

	private const int VanishSteps = 5;

	private const int VanishRepeats = 0x10;

	private AnimationInstance _animation;

	private bool _vanishing;

	private bool _visible;

	/// <summary>The counters of ObjC5_LaserWallDelete: anim_frame_duration, anim_frame and objoff_30.</summary>
	private int _duration;

	private int _step;

	private int _repeats;

	public WFZBossLaserWallInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZBossLaserWallType.AnimationGroupResourceKey));
		LockLifetime = true;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
	}

	/// <summary>The boss is defeated: the wall starts to go (ObjC5_LaserWallTimerSet).</summary>
	public void Vanish()
	{
		if (!_vanishing)
		{
			_vanishing = true;
			_repeats = 4;
		}
	}

	protected override void OnUpdate()
	{
		if (!_vanishing)
		{
			_visible = !_visible;
			return;
		}
		_visible = false;
		if (--_duration >= 0)
		{
			return;
		}
		if (_duration + 2 >= 0)
		{
			_visible = true;
			return;
		}
		_duration = _step;
		if (--_repeats >= 0)
		{
			_visible = true;
			return;
		}
		_repeats = VanishRepeats;
		if (++_step >= VanishSteps)
		{
			LockLifetime = false;
			FinishForever();
			return;
		}
		_duration = _step;
		_visible = true;
	}

	protected override void OnStop() => FinishForever();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_visible)
		{
			_animation.Index = Frame;
			renderer.GetObjectRenderer().Render(_animation);
		}
	}
}
