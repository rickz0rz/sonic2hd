using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WATEREGGMAN;

/// <summary>
/// The object type for the Chemical Plant Zone boss. It declares the resources that the boss needs.
/// See <see cref="WaterEggmanInstance"/> for the behavior.
/// </summary>

[SonicOrca.Core.Objects.Metadata.Name("Water Eggman")]
[Description("Water Eggman from Chemical Plant Zone, Sonic 2")]
[ObjectInstance(typeof(WaterEggmanInstance))]
public class WaterEggmanType : ObjectType
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
	public const string DefaultBrokenSmokeResourceKey = "SONICORCA/OBJECTS/DUST";
}
