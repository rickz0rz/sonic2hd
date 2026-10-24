using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZMACHINE;

/// <summary>
/// The object type for the platforms of Metropolis Zone that move in a circle (Obj6E in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Circling platform")]
[Description("Machine part from Metropolis Zone that moves in a circle")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZMachineInstance))]
public class MTZMachineType : ObjectType
{
	/// <summary>Obj6E_MapUnc_2852C with the level art. Frames 0 to 2 are the platforms.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Obj6E_MapUnc_2852C with the indent art of the large wheel. Frame 3 is the indent.</summary>
	[Dependency]
	public const string IndentAnimationGroupResourceKey = "/INDENT/ANIGROUP";
}
