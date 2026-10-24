using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.ARZSWINGPLATFORM;

/// <summary>
/// The object type for the swinging platform of Aquatic Ruin Zone (Obj15 in s2disasm). "s2hd-data import-level arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Swinging platform")]
[Description("Swinging platform from Aquatic Ruin Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(ARZSwingPlatformInstance))]
public class ARZSwingPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
