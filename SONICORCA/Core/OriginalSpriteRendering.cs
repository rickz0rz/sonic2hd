using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// Draws the sprites that "s2hd-data import-level" makes from the original art. A flipped frame of the original
/// mirrors its offset from the object position too, so a sprite that is not centred stays on the correct side.
/// <see cref="IObjectRenderer.Render(AnimationInstance, Vector2, bool, bool)"/> flips only the texture.
/// </summary>
public static class OriginalSpriteRendering
{
	public static void RenderOriginal(this IObjectRenderer objectRenderer, AnimationInstance animation, bool flipX = false, bool flipY = false)
	{
		objectRenderer.RenderOriginal(animation, default, flipX, flipY);
	}

	public static void RenderOriginal(this IObjectRenderer objectRenderer, AnimationInstance animation, Vector2 destination, bool flipX = false, bool flipY = false)
	{
		Animation.Frame frame = animation.CurrentFrame;
		Vector2 offset = new Vector2(flipX ? -frame.Offset.X : frame.Offset.X, flipY ? -frame.Offset.Y : frame.Offset.Y);
		objectRenderer.Texture = animation.CurrentTexture;
		objectRenderer.Render(frame.Source, destination + offset, flipX, flipY);
	}
}
