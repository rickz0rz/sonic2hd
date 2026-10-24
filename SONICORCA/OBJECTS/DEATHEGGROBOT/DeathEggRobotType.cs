using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.DEATHEGGROBOT;

/// <summary>
/// The object type for the Death Egg Robot, the final boss of Sonic 2 (ObjC7 in s2disasm, Eggrobo). The object layout
/// of Death Egg Zone places it. "s2hd-data import-level dez" adds the art of the original. See
/// <see cref="DeathEggRobotInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Death Egg Robot")]
[Description("Death Egg Robot, the final boss of Sonic 2")]
[ObjectInstance(typeof(DeathEggRobotInstance))]
public class DeathEggRobotType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The targeting sensor that follows the player while the robot is in the air.</summary>
	[Dependency]
	public const string SensorResourceKey = "/SENSOR";

	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";

	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";

	[Dependency]
	public const string SpindashSoundResourceKey = "SONICORCA/SOUND/SPINDASH/RELEASE";

	/// <summary>The game data has no music for the final boss, so the robot plays the boss music.</summary>
	[Dependency]
	public const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
}
