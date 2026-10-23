using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MASHER;

/// <summary>The object type for Masher, a piranha badnik that jumps up out of the water.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Masher")]
[Description("Masher from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(MasherInstance))]
public class MasherType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
