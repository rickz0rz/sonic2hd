using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZLONGPLATFORM;

/// <summary>
/// The object type for the long moving platforms and the cogs of Metropolis Zone (Obj65 in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Long moving platform")]
[Description("Long platform from Metropolis Zone that slides out, with its cog")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZLongPlatformInstance))]
public class MTZLongPlatformType : ObjectType
{
	/// <summary>The platforms (level art).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The small cog (ArtNem_MtzCog).</summary>
	[Dependency]
	public const string CogAnimationGroupResourceKey = "/COG/ANIGROUP";
}
