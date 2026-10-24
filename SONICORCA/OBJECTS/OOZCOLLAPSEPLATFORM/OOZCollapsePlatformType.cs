using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZCOLLAPSEPLATFORM;

/// <summary>
/// The object type for the collapsing platform of Oil Ocean Zone (Obj1F in s2disasm). "s2hd-data import-level ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Collapsing platform")]
[Description("Collapsing platform from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(OOZCollapsePlatformInstance))]
public class OOZCollapsePlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BREAKABLE";
}
