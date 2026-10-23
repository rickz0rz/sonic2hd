using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.HTZROCK.PARTICLE;

/// <summary>The object type of the rock fragment that a broken rock or breakable block throws out.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Rock particle")]
[Description("Rock particle from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Particle)]
[ObjectInstance(typeof(HTZRockParticleInstance))]
public class HTZRockParticleType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "//ANIGROUP";
}
