using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.EXPLOSION.BOSS;

/// <summary>The object type of the explosion particle that a boss makes when it is hit or destroyed.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Boss explosion")]
[Description("Boss explosion from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Particle)]
[ObjectInstance(typeof(BossExplosionInstance))]
public class BossExplosionType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BOSSEXPLOSION";
}
