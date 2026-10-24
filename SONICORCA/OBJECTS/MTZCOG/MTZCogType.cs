using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZCOG;

/// <summary>
/// The object type for the giant cog of Metropolis Zone (Obj70 in s2disasm). "s2hd-data import-level mtz" adds the
/// art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Giant cog")]
[Description("Giant rotating cog from Metropolis Zone with teeth that characters stand on")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZCogInstance))]
public class MTZCogType : ObjectType
{
	/// <summary>Obj70_MapUnc_28786: the 32 rotation frames of a tooth.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
