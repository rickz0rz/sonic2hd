using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.HTZRISINGBLOCK;

/// <summary>The object type of the large block that the Hill Top Zone earthquake moves up and down.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Rising block")]
[Description("Rising block from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(HTZRisingBlockInstance))]
public class HTZRisingBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
