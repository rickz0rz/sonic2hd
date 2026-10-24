using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZPLATFORM;

/// <summary>
/// The object type for the moving platform of Wing Fortress Zone (Obj19 with the WFZ art in s2disasm).
/// "s2hd-data import-level wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Moving platform")]
[Description("Moving platform from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(WFZPlatformInstance))]
public class WFZPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
