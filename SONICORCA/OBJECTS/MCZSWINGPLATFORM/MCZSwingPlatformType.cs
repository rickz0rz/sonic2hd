using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZSWINGPLATFORM;

/// <summary>
/// The object type for the swinging platform of Mystic Cave Zone (Obj15 in s2disasm). "s2hd-data import-level mcz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Swinging platform")]
[Description("Swinging platform from Mystic Cave Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZSwingPlatformInstance))]
public class MCZSwingPlatformType : ObjectType
{
	/// <summary>The platform, the chain link and the pivot (frames 0, 1 and 2).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The spiked log of the trap kind (frame 0).</summary>
	[Dependency]
	public const string TrapAnimationGroupResourceKey = "/TRAP/ANIGROUP";
}
