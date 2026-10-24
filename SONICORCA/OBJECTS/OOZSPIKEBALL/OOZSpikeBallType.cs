using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZSPIKEBALL;

/// <summary>
/// The object type for the sliding spike balls of Oil Ocean Zone (Obj43 in s2disasm). "s2hd-data import-level ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Sliding spike ball")]
[Description("Sliding spike ball from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(OOZSpikeBallInstance))]
public class OOZSpikeBallType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
