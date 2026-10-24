using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CHOPCHOP;

/// <summary>
/// The object type for Chop Chop, the piranha badnik of Aquatic Ruin Zone (Obj91 in s2disasm). "s2hd-data
/// import-level arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Chop Chop")]
[Description("Chop Chop from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(ChopChopInstance))]
public class ChopChopType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
