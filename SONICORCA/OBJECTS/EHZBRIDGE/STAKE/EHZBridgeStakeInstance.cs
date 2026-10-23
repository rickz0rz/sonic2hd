using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.EHZBRIDGE.STAKE;

/// <summary>The post at each end of a bridge in Emerald Hill Zone. It is decoration only.</summary>
public class EHZBridgeStakeInstance : Scenery
{
	public EHZBridgeStakeInstance()
		: base("//ANIGROUP")
	{
		DesignBounds = new Rectanglei(-32, -32, 64, 64);
	}

	protected override void OnStart()
	{
		base.OnStart();
		Priority = 1280;
	}
}
