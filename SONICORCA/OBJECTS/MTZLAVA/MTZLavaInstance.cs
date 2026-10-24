using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZLAVA;

/// <summary>
/// The lava collision marker, as in Sonic 2 (Obj31). It is an invisible area that hurts a character that touches it.
/// The subtype selects the size (collision flags $96, $94 and $95): subtype 0 is $20 pixels to each side and $20 up
/// and down, subtype 1 is $40 to each side and subtype 2 is $80 to each side.
/// </summary>
public class MTZLavaInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfHeight = 0x20;

	private static readonly int[] HalfWidths = { 0x20, 0x40, 0x80 };

	private int _subtype;

	/// <summary>The subtype of the original: 0, 1 or 2.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	private int HalfWidth => HalfWidths[_subtype is >= 0 and <= 2 ? _subtype : 0];

	public MTZLavaInstance() => DesignBounds = new Rectanglei(-0x80 * Scale, -HalfHeight * Scale, 0x100 * Scale, HalfHeight * 2 * Scale);

	protected override void OnStart()
	{
		DesignBounds = new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale);
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 1, -HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale)
		};
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		// The character bounces to the right when it is at or right of the centre
		ICharacter character = (ICharacter)e.ActiveObject;
		character.Hurt(character.Position.X >= Position.X ? 1 : -1);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!Level.LayerViewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing) || viewOptions.Shadows)
		{
			return;
		}
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		Rectangle destination = new Rectangle(-HalfWidth * Scale * scale.X, -HalfHeight * Scale * scale.Y, HalfWidth * 2 * Scale * scale.X, HalfHeight * 2 * Scale * scale.Y);
		renderer2d.RenderRectangle(Colours.Red, destination, 2.0);
	}
}
