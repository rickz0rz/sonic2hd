using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.FLICKY;

/// <summary>The Flicky bird that a destroyed badnik releases. The base class <see cref="Animal"/> moves it.</summary>
public class FlickyInstance : Animal
{
	public FlickyInstance()
	  : base("/ANIGROUP")
	{
		JumpVelocity = new Vector2(-12.0, -16.0);
	}
}
