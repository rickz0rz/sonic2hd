using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZCOLLAPSEPLATFORM;

/// <summary>
/// The object type for the collapsing platform of Mystic Cave Zone (Obj1F in s2disasm). "s2hd-data import-level
/// mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Collapsing platform")]
[Description("Collapsing platform from Mystic Cave Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZCollapsePlatformInstance))]
public class MCZCollapsePlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BREAKABLE";
}
