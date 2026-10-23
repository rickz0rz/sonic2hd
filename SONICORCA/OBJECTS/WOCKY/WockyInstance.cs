using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.WOCKY;

/// <summary>Wocky, an animal that jumps away after a destroyed badnik sets it free.</summary>
public class WockyInstance : Animal
{
	public WockyInstance()
		: base(WockyType.AnimationGroupResourceKey)
	{
		JumpVelocity = new Vector2(-10.0, -5.0);
	}
}
