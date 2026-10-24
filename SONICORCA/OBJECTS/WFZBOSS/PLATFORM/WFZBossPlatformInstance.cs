using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZBOSS.PLATFORM;

/// <summary>
/// A platform that the Wing Fortress Zone boss releases (ObjC5, routines 8 and $A). It is $10 pixels to each side,
/// and the spikes under it hurt (a box of 4 by 4 pixels $C pixels below its centre). It goes down at $100 for $60
/// frames. Then it moves to the left at $100 and turns round every $C0 frames ($60 frames for the first turn). Its
/// vertical speed changes by 4 each frame towards the height where it started to move to the side, so it bobs up and
/// down. When the boss is defeated, it explodes.
/// </summary>
public class WFZBossPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int HalfWidth = 0x10;

	private const int TopHeight = 8;

	private const int SpikeOffset = 0xC;

	private const int SpikeHalfSize = 4;

	private const int Speed = 0x100;

	private const int FirstTicks = 0x60;

	private const int TurnTicks = 0xC0;

	private const int Bob = 4;

	private static readonly int[] Frames = { 7, 8, 9, 0xA, 0xB };

	private const int FrameTicks = 4;

	private AnimationInstance _animation;

	private bool _started;

	private bool _sideways;

	private int _timer = FirstTicks;

	/// <summary>The position in original pixels, the speeds in 1/256 pixels per frame.</summary>
	private Vector2 _position;

	private int _velocityX;

	private int _velocityY = Speed;

	/// <summary>The height where the platform started to move to the side (objoff_34).</summary>
	private double _middleY;

	private int _ticks;

	public WFZBossPlatformInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -8 * Scale, HalfWidth * 2 * Scale, 0x18 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZBossPlatformType.AnimationGroupResourceKey));
		LockLifetime = true;
		_position = PositionPrecise / Scale;
		CollisionVectors = new[]
		{
			new CollisionVector(this, new Vector2i(-HalfWidth * Scale, -TopHeight * Scale), new Vector2i(HalfWidth * Scale, -TopHeight * Scale), flags: CollisionFlags.Conveyor)
		};
		CollisionRectangles = new[]
		{
			new CollisionRectangle(this, 0, -SpikeHalfSize * Scale, (SpikeOffset - SpikeHalfSize) * Scale, SpikeHalfSize * 2 * Scale, SpikeHalfSize * 2 * Scale)
		};
	}

	protected override void UpdateMovement()
	{
		_ticks++;
		// ObjC5_PlatformInit: the first frame does not move
		if (!_started)
		{
			_started = true;
			return;
		}
		if (!_sideways)
		{
			if (--_timer == 0)
			{
				_sideways = true;
				_timer = FirstTicks;
				_velocityX = -Speed;
				_middleY = _position.Y;
			}
		}
		else
		{
			if (--_timer == 0)
			{
				_timer = TurnTicks;
				_velocityX = -_velocityX;
			}
			_velocityY += _position.Y < _middleY ? Bob : -Bob;
		}
		_position += new Vector2(_velocityX / 256.0, _velocityY / 256.0);
		PositionPrecise = _position * Scale;
	}

	/// <summary>The boss is defeated: the platform turns into an explosion (ObjC5_PlatformExplode).</summary>
	public void Explode()
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(WFZBossPlatformType.ExplosionResourceKey), Level.Map.Layers.IndexOf(Layer), Position));
		LockLifetime = false;
		FinishForever();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject is ICharacter character && character.CanBeHurt)
		{
			character.Hurt(System.Math.Sign(character.Position.X - Position.X));
		}
	}

	protected override void OnStop() => FinishForever();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = Frames[(_ticks / FrameTicks) % Frames.Length];
		renderer.GetObjectRenderer().Render(_animation);
	}
}
