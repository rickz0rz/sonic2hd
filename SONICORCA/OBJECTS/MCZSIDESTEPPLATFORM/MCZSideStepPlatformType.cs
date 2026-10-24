using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZSIDESTEPPLATFORM;

/// <summary>
/// The object type for the platform of Mystic Cave Zone that moves to the left and the right (Obj7A in s2disasm).
/// "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Sideways platform")]
[Description("Platform that moves to the left and the right from Mystic Cave Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZSideStepPlatformInstance))]
public class MCZSideStepPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
