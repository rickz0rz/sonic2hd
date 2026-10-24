using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZFLOORSPIKE;

/// <summary>
/// The object type for the floor spike of Metropolis Zone (Obj6D in s2disasm). "s2hd-data import-level mtz" adds the
/// art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Floor spike")]
[Description("Spike that comes up out of the floor from Metropolis Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(MTZFloorSpikeInstance))]
public class MTZFloorSpikeType : ObjectType
{
	/// <summary>Obj68_Obj6D_MapUnc_27750. Frame 0 is the spike that points up.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
