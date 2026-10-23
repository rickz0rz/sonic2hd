using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.RICKY;

/// <summary>Ricky, an animal that jumps away after a destroyed badnik sets it free.</summary>
public class RickyInstance : Animal
{
	public RickyInstance()
		: base(RickyType.AnimationGroupResourceKey)
	{
		JumpVelocity = new Vector2(-14.0, -14.0);
		JumpGravity = 0.875;
	}
}
