using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZBOSS.ROCK;

/// <summary>
/// The object type for a stone or a spike that falls from the ceiling while the Mystic Cave Zone boss digs (Obj57,
/// subtype 4). See <see cref="MCZBossRockInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("MCZ boss rock")]
[Description("Falling stone or spike from the Mystic Cave Zone boss, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(MCZBossRockInstance))]
public class MCZBossRockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
