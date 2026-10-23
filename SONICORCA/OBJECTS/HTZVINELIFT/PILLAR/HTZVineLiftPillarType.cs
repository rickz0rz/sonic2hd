using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.HTZVINELIFT.PILLAR;

/// <summary>The object type of the pillar at each end of a vine lift path.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Vine lift pillar")]
[Description("Vine lift pillar from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(HTZVineLiftPillarInstance))]
public class HTZVineLiftPillarType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "//ANIGROUP";
}
