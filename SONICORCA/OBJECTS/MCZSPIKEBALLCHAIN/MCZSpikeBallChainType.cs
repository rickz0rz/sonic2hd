using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZSPIKEBALLCHAIN;

/// <summary>
/// The object type for the spike ball on a chain and the brick of Mystic Cave Zone (Obj75 in s2disasm). "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Spike ball on a chain")]
[Description("Spike ball that turns on a chain, or a solid brick, from Mystic Cave Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(MCZSpikeBallChainInstance))]
public class MCZSpikeBallChainType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
