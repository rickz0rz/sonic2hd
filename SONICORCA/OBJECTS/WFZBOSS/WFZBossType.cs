using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZBOSS;

/// <summary>
/// The object type for the Wing Fortress Zone boss, the laser case (ObjC5 in s2disasm). The level area creates it
/// when the camera comes to the boss room. "s2hd-data import-level wfz" adds the art of the original. See
/// <see cref="WFZBossInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("WFZ boss")]
[Description("Laser case boss from Wing Fortress Zone, Sonic 2")]
[ObjectInstance(typeof(WFZBossInstance))]
public class WFZBossType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Robotnik, who watches from his stand (the mappings of ObjC6).</summary>
	[Dependency]
	public const string RobotnikAnimationGroupResourceKey = "/ROBOTNIK/ANIGROUP";

	/// <summary>The stand of Robotnik (ObjC5_MapUnc_3CEBC).</summary>
	[Dependency]
	public const string StandAnimationGroupResourceKey = "/STAND/ANIGROUP";

	[Dependency]
	public const string LaserWallResourceKey = "/LASERWALL";

	[Dependency]
	public const string PlatformResourceKey = "/PLATFORM";

	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";

	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";
}
