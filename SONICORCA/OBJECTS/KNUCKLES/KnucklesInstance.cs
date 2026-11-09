using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.KNUCKLES;

/// <summary>
/// Knuckles, a playable character. The Character base class has the movement. Knuckles jumps lower than Sonic: $600,
/// and $300 under water (KiS2). The sprites are the original sprites of KiS2, at four times their size.
/// </summary>
public class KnucklesInstance : Character
{
	/// <summary>The jump speed of Knuckles, $600, in level units.</summary>
	private const double KnucklesJumpForce = 0x600 * 4.0 / 256.0;

	/// <summary>The jump speed of Knuckles under water, $300, in level units.</summary>
	private const double KnucklesUnderwaterJumpForce = 0x300 * 4.0 / 256.0;

	/// <summary>
	/// The height radius of the original Knuckles, at four times the size: y_radius $13, and $E in a roll. The
	/// original sprites line up with these.
	/// </summary>
	private const int OriginalCollisionRadiusY = 76;

	private const int OriginalSpinballCollisionRadiusY = 56;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath(KnucklesType.AnimationGroupResourceKey);
		NormalJumpForce = KnucklesJumpForce;
		UnderwaterJumpForce = KnucklesUnderwaterJumpForce;
		// Super Knuckles does not jump higher (KiS2)
		SuperJumpForce = KnucklesJumpForce;
		if (Level.OriginalPhysics)
		{
			// The collision box of the original Knuckles: x_radius 9 and y_radius $13, and 7 and $E in a roll
			NormalCollisionRadius = new Vector2i(36, 76);
			SpinballCollisionRadius = new Vector2i(28, 56);
		}
		base.OnStart();
	}

	/// <summary>
	/// Draws the original sprite. The original art faces right. The sprite lines up with the collision box of the
	/// original, so it moves by the difference to the box of the character.
	/// </summary>
	protected override void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (DrawBodyRotated)
				objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
			double offsetY = IsSpinball ? SpinballCollisionRadius.Y - OriginalSpinballCollisionRadiusY : NormalCollisionRadius.Y - OriginalCollisionRadiusY;
			objectRenderer.RenderOriginal(Animation, new Vector2(0.0, offsetY), IsFacingLeft);
		}
	}
}
