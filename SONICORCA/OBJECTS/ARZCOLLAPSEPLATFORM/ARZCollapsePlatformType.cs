using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.ARZCOLLAPSEPLATFORM;

/// <summary>
/// The object type for the collapsing platform of Aquatic Ruin Zone (Obj1F in s2disasm). "s2hd-data import-level arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Collapsing platform")]
[Description("Collapsing platform from Aquatic Ruin Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(ARZCollapsePlatformInstance))]
public class ARZCollapsePlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BREAKABLE";
}
