using SONICORCA.OBJECTS.ARZCOLLAPSEPLATFORM;

namespace SONICORCA.OBJECTS.MCZCOLLAPSEPLATFORM;

/// <summary>
/// A platform that collapses, as in Sonic 2 (Obj1F in Mystic Cave Zone). It works as in Aquatic Ruin, but it breaks
/// into 6 pieces with the delays of Obj1F_MCZ_DelayData.
/// </summary>
public class MCZCollapsePlatformInstance : ARZCollapsePlatformInstance
{
	/// <summary>The delay of each piece (Obj1F_MCZ_DelayData).</summary>
	private static readonly int[] MczDelays = { 0x1A, 0x16, 0x12, 0x0E, 0x0A, 0x02 };

	protected override int[] Delays => MczDelays;
}
