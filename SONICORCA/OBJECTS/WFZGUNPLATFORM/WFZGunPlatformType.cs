using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZGUNPLATFORM;

/// <summary>
/// The object type for the lateral cannon of Wing Fortress Zone, a platform that comes out of the wall and goes back
/// (ObjBE in s2disasm). "s2hd-data import-level wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Gun platform")]
[Description("Retracting gun platform from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(WFZGunPlatformInstance))]
public class WFZGunPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
