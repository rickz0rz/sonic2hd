using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZBLOCK;

/// <summary>
/// The object type for the platforms of Metropolis Zone that stay, move, sink or descend (Obj6B in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Platform")]
[Description("Platform from Metropolis Zone that stays, moves, sinks or descends")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZBlockInstance))]
public class MTZBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
