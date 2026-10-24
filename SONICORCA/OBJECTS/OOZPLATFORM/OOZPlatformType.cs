using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZPLATFORM;

/// <summary>
/// The object type for the moving platform of Oil Ocean Zone (Obj19 in s2disasm). "s2hd-data import-level ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Moving platform")]
[Description("Moving platform from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(OOZPlatformInstance))]
public class OOZPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
