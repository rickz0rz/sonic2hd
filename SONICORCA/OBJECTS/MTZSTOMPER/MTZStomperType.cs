using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZSTOMPER;

/// <summary>
/// The object type for the twin stompers of Metropolis Zone (Obj64 in s2disasm). "s2hd-data import-level mtz" adds
/// the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Twin stomper")]
[Description("Stomper from Metropolis Zone that drops and rises")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZStomperInstance))]
public class MTZStomperType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
