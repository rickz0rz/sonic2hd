using SONICORCA.OBJECTS.CNZSLOTCAGE;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZSLOTREELS;

/// <summary>
/// The three reels of a slot machine. Each reel is a 32 x 32 window on a strip of 8 faces. The object position is
/// the top left of the left reel. All machines of a level show the same reels, as in the original.
/// </summary>
public class CNZSlotReelsInstance : ActiveObject
{
	private const int Scale = 4;

	private const int ReelSize = 32 * Scale;

	private ITexture _texture;

	public CNZSlotReelsInstance() => DesignBounds = new Rectanglei(0, 0, ReelSize * 3, ReelSize);

	protected override void OnStart()
	{
		_texture = ResourceTree.GetLoadedResource<ITexture>(Type.GetAbsolutePath(CNZSlotReelsType.TextureResourceKey));
		// In front of the landscape, behind the characters
		Priority = 1;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		CNZSlotMachine machine = CNZSlotMachine.For(Level);
		objectRenderer.Texture = _texture;
		for (int reel = 0; reel < 3; reel++)
		{
			var source = new Rectangle(reel * ReelSize, machine.GetShownRow(reel) * Scale, ReelSize, ReelSize);
			objectRenderer.Render(source, new Vector2(reel * ReelSize + ReelSize / 2, ReelSize / 2));
		}
	}
}
