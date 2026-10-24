using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZNUT;

/// <summary>
/// The object type for the nuts of Metropolis Zone (Obj69 in s2disasm). "s2hd-data import-level mtz" adds the art
/// of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Nut")]
[Description("Nut from Metropolis Zone that turns down a screw when a character runs on it")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZNutInstance))]
public class MTZNutType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
