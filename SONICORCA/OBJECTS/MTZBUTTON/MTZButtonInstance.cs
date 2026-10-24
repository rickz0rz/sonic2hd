using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZBUTTON;

/// <summary>
/// A button, as in Sonic 2 (Obj47). The button is solid. While a character stands on it, it shows the pressed frame
/// and sets a bit of the trigger at its index (subtype bits 0-3): bit 0, or bit 7 when subtype bit 6 is set. When no
/// character stands on it, it clears the bit again. It plays a sound when it sets a trigger that was clear.
/// </summary>
public class MTZButtonInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The solid box: $1B pixels to each side, 4 up and 5 down.</summary>
	private const int HalfWidth = 0x1B;

	private const int Top = 4;

	private const int Bottom = 5;

	/// <summary>The button is 4 pixels below its placement. The box and the sprite move down by this distance.</summary>
	private const int PlacementOffset = 4;

	private const int UpFrame = 0;

	private const int PressedFrame = 1;

	private AnimationInstance _animation;

	private int _trigger;

	private bool _highBit;

	private bool _pressed;

	/// <summary>The index of the trigger that the button sets (subtype bits 0-3).</summary>
	[StateVariable]
	private int Trigger
	{
		get => _trigger;
		set => _trigger = value;
	}

	/// <summary>The button sets bit 7 of the trigger instead of bit 0 (subtype bit 6).</summary>
	[StateVariable]
	private bool HighBit
	{
		get => _highBit;
		set => _highBit = value;
	}

	public MTZButtonInstance() => DesignBounds = new Rectanglei(-0x10 * Scale, -0x8 * Scale, 0x20 * Scale, 0x10 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZButtonType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, (PlacementOffset - Top) * Scale, HalfWidth * 2 * Scale, (Top + Bottom) * Scale));
	}

	protected override void OnUpdate()
	{
		ButtonVineTriggers triggers = ButtonVineTriggers.For(Level);
		int bit = _highBit ? 7 : 0;
		_pressed = Level.ObjectManager.Characters.Any(c => c.ObjectLink == this);
		if (!_pressed)
		{
			triggers.Clear(_trigger, bit);
			return;
		}
		if (!triggers.IsAnySet(_trigger))
		{
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(MTZButtonType.SwitchSoundResourceKey));
		}
		triggers.Set(_trigger, bit);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _pressed ? PressedFrame : UpFrame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, new Vector2(0.0, PlacementOffset * Scale));
	}
}
