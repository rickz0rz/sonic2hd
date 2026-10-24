using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZBIGBLOCK;

/// <summary>
/// The object type for the big block that moves back and forth of Casino Night Zone (ObjD4 in s2disasm). "s2hd-data import-level cnz" adds the art of
/// the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Big moving block")]
[Description("Big moving block from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(CNZBigBlockInstance))]
public class CNZBigBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
