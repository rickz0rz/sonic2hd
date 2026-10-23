using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.HTZBREAKABLEPLATFORM;

/// <summary>The object type of the column of breakable blocks from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Breakable platform")]
[Description("Breakable platform from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(HTZBreakablePlatformInstance))]
public class HTZBreakablePlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string ParticleObjectResourceKey = "SONICORCA/OBJECTS/HTZROCK/PARTICLE";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BREAKABLE";
}
