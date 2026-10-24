using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZSWINGPLATFORM;

/// <summary>
/// The object type for the swinging platform of Oil Ocean Zone (Obj15 in s2disasm). "s2hd-data import-level ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Swinging platform")]
[Description("Swinging platform from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(OOZSwingPlatformInstance))]
public class OOZSwingPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
