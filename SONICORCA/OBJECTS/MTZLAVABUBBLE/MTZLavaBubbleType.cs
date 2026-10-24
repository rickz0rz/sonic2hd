using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZLAVABUBBLE;

/// <summary>
/// The object type for the lava bubble of Metropolis Zone (Obj71, subtype $22, in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Lava bubble")]
[Description("Bubble in the lava of Metropolis Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(MTZLavaBubbleInstance))]
public class MTZLavaBubbleType : ObjectType
{
	/// <summary>Obj71_MapUnc_11576: frames 0 to 5 of the bubble. Frame 6 is empty.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
