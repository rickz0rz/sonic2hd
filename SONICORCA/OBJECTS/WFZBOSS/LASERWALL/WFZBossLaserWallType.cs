using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZBOSS.LASERWALL;

/// <summary>
/// The object type for a laser wall of the Wing Fortress Zone boss (ObjC5, routine 4). The boss creates two of them.
/// The art is a frame of the boss. See <see cref="WFZBossLaserWallInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("WFZ boss laser wall")]
[Description("Laser wall of the Wing Fortress Zone boss, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(WFZBossLaserWallInstance))]
public class WFZBossLaserWallType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "SONICORCA/OBJECTS/WFZBOSS/ANIGROUP";
}
