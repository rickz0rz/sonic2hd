using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.ARZFALLINGPILLAR;

/// <summary>
/// The object type for the pillar of Aquatic Ruin Zone that drops its lower part (Obj23 in s2disasm). "s2hd-data import-level arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Falling pillar")]
[Description("Falling pillar from Aquatic Ruin Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(ARZFallingPillarInstance))]
public class ARZFallingPillarType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
