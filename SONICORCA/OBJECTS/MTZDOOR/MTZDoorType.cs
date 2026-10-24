using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZDOOR;

/// <summary>
/// The object type for the one-way barrier of Metropolis Zone (Obj2D in s2disasm). "s2hd-data import-level mtz" adds
/// the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Metropolis barrier")]
[Description("One-way barrier from Metropolis Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(MTZDoorInstance))]
public class MTZDoorType : ObjectType
{
	/// <summary>Obj2D_MapUnc_11822 with the level art. Frame 1 is the Metropolis barrier.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
