using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZBELTPLATFORM;

/// <summary>
/// The object type for the small metal platforms that ride up or down a belt in Wing Fortress Zone (ObjBD in
/// s2disasm). The placed object makes the platforms. "s2hd-data import-level wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Belt platform")]
[Description("Ascending or descending metal platforms from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(WFZBeltPlatformInstance))]
public class WFZBeltPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
