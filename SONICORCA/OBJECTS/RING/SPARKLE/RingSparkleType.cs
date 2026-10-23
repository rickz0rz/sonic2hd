using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.RING.SPARKLE;

/// <summary>The object type for the sparkle particle that shows when a ring is collected.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Ring sparkle")]
[Description("Ring sparkle from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Particle)]
[ObjectInstance(typeof(RingSparkleInstance))]
public class RingSparkleType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
