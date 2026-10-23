using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.POCKY;

/// <summary>Pocky, an animal that jumps away after a destroyed badnik sets it free.</summary>
public class PockyInstance : Animal
{
	public PockyInstance()
		: base(PockyType.AnimationGroupResourceKey)
	{
		JumpVelocity = new Vector2(-6.0, -12.0);
		JumpGravity = 0.875;
	}
}
