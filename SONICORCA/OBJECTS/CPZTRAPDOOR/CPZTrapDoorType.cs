using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZTRAPDOOR;

/// <summary>The object type for <see cref="CPZTrapDoorInstance"/>, a floor that opens and closes on a timer.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Trap door")]
[Description("Trap door from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(CPZTrapDoorInstance))]
public class CPZTrapDoorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
