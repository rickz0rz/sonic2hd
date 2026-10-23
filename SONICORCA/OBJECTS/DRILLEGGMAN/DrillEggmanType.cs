using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.DRILLEGGMAN;

/// <summary>
/// The object type for the Emerald Hill Zone boss. It declares the resources that the boss needs.
/// See <see cref="DrillEggmanInstance"/> for the behavior.
/// </summary>

[SonicOrca.Core.Objects.Metadata.Name("Drill Eggman")]
[Description("Drill Eggman from Emerald Hill Zone, Sonic 2")]
[ObjectInstance(typeof(DrillEggmanInstance))]
public class DrillEggmanType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string RobotnikAnimationGroupResourceKey = "SONICORCA/OBJECTS/ROBOTNIK/ANIGROUP";
	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";
	[Dependency]
	public const string BossHelicopterSoundResourceKey = "SONICORCA/SOUND/BOSSHELICOPTER";
	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";
	[Dependency]
	public const string DefaultBrokenSmokeResourceKey = "SONICORCA/OBJECTS/DUST";
}
