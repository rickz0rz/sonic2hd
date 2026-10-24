using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.INVISIBLEBLOCK;

/// <summary>
/// A solid box that is not drawn. The level uses it where the landscape has no collision, for example at the edges
/// of the map. It shows only with the object collision view or in the editor.
/// </summary>
public class InvisibleBlockInstance : ActiveObject
{
	private int _width = 128;

	private int _height = 128;

	[StateVariable]
	private int Width
	{
		get => _width;
		set => _width = value;
	}

	[StateVariable]
	private int Height
	{
		get => _height;
		set => _height = value;
	}

	protected override void OnStart()
	{
		DesignBounds = new Rectanglei(-_width / 2, -_height / 2, _width, _height);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-_width / 2, -_height / 2, _width, _height));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
		{
			return;
		}
		renderer.Get2dRenderer().RenderRectangle(Colours.White, new Rectangle(-_width / 2, -_height / 2, _width, _height), 1.0);
	}
}
