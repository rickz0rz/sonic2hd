using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SUBMARINEEGGMAN;

/// <summary>
/// The object type for the Hill Top Zone boss. It declares the resources that the boss needs.
/// See <see cref="SubmarineEggmanInstance"/> for the behavior.
/// </summary>

[SonicOrca.Core.Objects.Metadata.Name("Submarine Eggman")]
[Description("Submarine Eggman from Hill Top Zone, Sonic 2")]
[ObjectInstance(typeof(SubmarineEggmanInstance))]
public class SubmarineEggmanType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string RobotnikAnimationGroupResourceKey = "SONICORCA/OBJECTS/ROBOTNIK/ANIGROUP";
	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";
	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";
	[Dependency]
	public const string FireBallSoundResourceKey = "SONICORCA/SOUND/FIREBALL";
	[Dependency]
	public const string FireBurnSoundResourceKey = "SONICORCA/SOUND/FIREBURN";
	[Dependency]
	public const string DefaultBrokenSmokeResourceKey = "SONICORCA/OBJECTS/DUST";
}
