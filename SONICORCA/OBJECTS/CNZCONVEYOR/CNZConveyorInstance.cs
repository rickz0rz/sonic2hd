using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZCONVEYOR;

/// <summary>
/// An invisible conveyor belt, as in Sonic 2 (Obj72). It moves each character on the ground in its area 2 pixels
/// each frame. The area is above the object position: the half width from the subtype, and $30 or $70 pixels high.
/// </summary>
public class CNZConveyorInstance : ActiveObject
{
	/// <summary>2 pixels each frame at four times the resolution.</summary>
	private const int Speed = 8;

	private int _halfWidth = 256;

	private int _height = 0x30 * 4;

	private bool _left;

	[StateVariable]
	private int HalfWidth
	{
		get => _halfWidth;
		set => _halfWidth = value;
	}

	[StateVariable]
	private int Height
	{
		get => _height;
		set => _height = value;
	}

	/// <summary>The belt moves characters to the left instead of the right.</summary>
	[StateVariable]
	private bool Left
	{
		get => _left;
		set => _left = value;
	}

	protected override void OnStart()
	{
		DesignBounds = new Rectanglei(-_halfWidth, -_height, _halfWidth * 2, _height);
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			int dx = character.Position.X - Position.X;
			int dy = character.Position.Y - Position.Y;
			if (dx >= -_halfWidth && dx < _halfWidth && dy >= -_height && dy < 0 && !character.IsAirborne && !character.IsObjectControlled)
			{
				character.Position += new Vector2i(_left ? -Speed : Speed, 0);
			}
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
		{
			return;
		}
		renderer.Get2dRenderer().RenderRectangle(Colours.White, new Rectangle(-_halfWidth, -_height, _halfWidth * 2, _height), 1.0);
	}
}
