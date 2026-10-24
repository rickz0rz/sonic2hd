using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZTURRET;

/// <summary>
/// The object type for the wall turret of Wing Fortress Zone (ObjB8 in s2disasm). "s2hd-data import-level wfz" adds
/// the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Wall turret")]
[Description("Wall turret from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(WFZTurretInstance))]
public class WFZTurretType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
