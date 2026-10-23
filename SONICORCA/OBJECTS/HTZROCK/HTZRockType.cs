using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.HTZROCK;

/// <summary>The object type of the breakable rock from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Rock")]
[Description("Rock from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(HTZRockInstance))]
public class HTZRockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string ParticleObjectResourceKey = "/PARTICLE";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BREAKABLE";
}
