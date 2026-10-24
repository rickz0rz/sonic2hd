using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZBUBBLES;

/// <summary>
/// A bubble generator, as in Sonic 2 (Obj24). While it is under water and on screen, it makes bursts of 1 to 6
/// bubbles, one each 0 to 31 frames, with a pause of 128 to 255 frames between the bursts. Each Nth burst (the
/// subtype + 1) has one big bubble that a character can breathe.
/// </summary>
public class ARZBubblesInstance : ActiveObject
{
	private const int Scale = 4;

	private const int SmallBubble = 0;

	private const int BigBubble = 2;

	/// <summary>The sizes of the bubbles of a burst (byte_1FAF0): 0 small, 1 medium.</summary>
	private static readonly int[] BurstSizes = { 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0 };

	/// <summary>The generator frames: $E and $F, 16 frames each, after the 14 bubble frames.</summary>
	private const int GeneratorFrame = 14;

	private AnimationInstance _animation;

	private int _cycle;

	private int _cyclesLeft;

	private bool _burst;

	private bool _burstHasBig;

	private bool _bigSpawned;

	private int _timer;

	private int _bubblesLeft;

	private int _tableStart;

	private int _ticks;

	/// <summary>Each how many bursts have a big bubble, minus one (subtype bits 0-6).</summary>
	[StateVariable]
	private int Cycle
	{
		get => _cycle;
		set => _cycle = value;
	}

	public ARZBubblesInstance() => DesignBounds = new Rectanglei(-32, -32, 64, 64);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZBubblesType.AnimationGroupResourceKey));
		_cyclesLeft = _cycle;
	}

	private bool Underwater => Level.WaterManager.Enabled && Level.WaterManager.IsUnderwater(Position);

	protected override void OnUpdate()
	{
		_ticks++;
		if (!_burst)
		{
			if (!Underwater || !Level.Camera.Bounds.Contains((Vector2)Position) || --_timer >= 0)
			{
				return;
			}
			StartBurst();
		}
		else if (--_timer >= 0)
		{
			return;
		}
		Spawn();
	}

	private void StartBurst()
	{
		_burst = true;
		_burstHasBig = false;
		_bigSpawned = false;
		int roll;
		do
		{
			roll = Level.Random.Next(0x10000);
		}
		while ((roll & 7) >= 6);
		_bubblesLeft = roll & 7;
		_tableStart = (roll >> 8) & 0xC;
		if (--_cyclesLeft < 0)
		{
			_cyclesLeft = _cycle;
			_burstHasBig = true;
		}
	}

	private void Spawn()
	{
		_timer = Level.Random.Next(0x20);
		int size = BurstSizes[(_tableStart + _bubblesLeft) % BurstSizes.Length];
		if (_burstHasBig && !_bigSpawned && (Level.Random.Next(4) == 0 || _bubblesLeft == 0))
		{
			_bigSpawned = true;
			size = BigBubble;
		}
		int x = Position.X + (Level.Random.Next(0x10) - 8) * Scale;
		Level.WaterManager.CreateBubble(Level.Map.Layers.IndexOf(Layer), new Vector2i(x, Position.Y), size);
		if (--_bubblesLeft < 0)
		{
			_timer += Level.Random.Next(0x80) + 0x80;
			_burst = false;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The generator shows only under water
		if (!Underwater)
		{
			return;
		}
		_animation.Index = GeneratorFrame + (_ticks / 16) % 2;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
