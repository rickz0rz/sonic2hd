using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.HTZDOOR;

/// <summary>The object type of the one-way door from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("One-way door")]
[Description("One-way door from from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(HTZDoorInstance))]
public class HTZDoorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
