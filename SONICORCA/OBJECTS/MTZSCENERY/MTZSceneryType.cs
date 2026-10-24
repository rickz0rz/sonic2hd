using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZSCENERY;

/// <summary>
/// The object type for the bolt ends and the rope of Metropolis Zone (Obj1C, subtypes 0, 1 and 3, in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Metropolis scenery")]
[Description("Bolt ends and rope from Metropolis Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(MTZSceneryInstance))]
public class MTZSceneryType : ObjectType
{
	/// <summary>Obj1C_MapUnc_11552 in palette line 2 (animations 0 to 2), then in palette line 1 (animations 3 to 5).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
