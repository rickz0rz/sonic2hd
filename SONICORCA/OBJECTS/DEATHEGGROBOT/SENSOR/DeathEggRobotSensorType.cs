using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.DEATHEGGROBOT.SENSOR;

/// <summary>
/// The object type for the targeting sensor of the Death Egg Robot (ObjC7 in s2disasm, routines $18 and $1A). The
/// robot adds it. See <see cref="DeathEggRobotSensorInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Death Egg Robot sensor")]
[Description("Targeting sensor of the Death Egg Robot, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(DeathEggRobotSensorInstance))]
public class DeathEggRobotSensorType : ObjectType
{
	/// <summary>The art of the robot, with palette lines 0 and 1, so that the lock can flash.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
