using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZPLATFORM;

/// <summary>
/// The object type for the moving crates of Mystic Cave Zone (Obj6A in s2disasm). "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Moving crate")]
[Description("Large wooden crate from Mystic Cave Zone that moves around a rectangle")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZPlatformInstance))]
public class MCZPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
