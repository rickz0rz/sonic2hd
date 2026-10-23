using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MONITOR;

/// <summary>The object type for the item monitor.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Monitor")]
[Description("Monitor from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(MonitorInstance))]
public class MonitorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string ExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BADNIK";
}
