using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZPLATFORM;

/// <summary>
/// The object type for the platforms of Metropolis Zone that move when a character walks off them (Obj6A in
/// s2disasm). "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Step platform")]
[Description("Platform from Metropolis Zone that moves one step when a character walks off it")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZPlatformInstance))]
public class MTZPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
