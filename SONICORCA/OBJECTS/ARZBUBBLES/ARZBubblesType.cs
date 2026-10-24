using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.ARZBUBBLES;

/// <summary>
/// The object type for the bubble generator of Aquatic Ruin Zone (Obj24 in s2disasm). "s2hd-data import-level arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Bubble generator")]
[Description("Bubble generator from Aquatic Ruin Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(ARZBubblesInstance))]
public class ARZBubblesType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
