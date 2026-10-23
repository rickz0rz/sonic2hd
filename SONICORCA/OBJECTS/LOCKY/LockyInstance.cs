using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.LOCKY;

/// <summary>Locky, an animal that jumps away after a destroyed badnik sets it free.</summary>
public class LockyInstance : Animal
{
	public LockyInstance()
		: base(LockyType.AnimationGroupResourceKey)
	{
		JumpVelocity = new Vector2(-10.0, -12.0);
	}
}
