using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZFLASHINGBLOCK;

/// <summary>
/// The object type for the flashing blocks that move in a U shape of Casino Night Zone (ObjD2 in s2disasm). "s2hd-data import-level cnz" adds the art of
/// the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Flashing block")]
[Description("Flashing block from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(CNZFlashingBlockInstance))]
public class CNZFlashingBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
