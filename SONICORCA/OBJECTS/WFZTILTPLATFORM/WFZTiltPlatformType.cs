using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZTILTPLATFORM;

/// <summary>
/// The object type for the tilting platform of Wing Fortress Zone (ObjB6 in s2disasm). "s2hd-data import-level wfz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Tilting platform")]
[Description("Tilting platform from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(WFZTiltPlatformInstance))]
public class WFZTiltPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
