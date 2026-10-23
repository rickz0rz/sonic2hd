using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZDOOR;

/// <summary>The object type for <see cref="CPZDoorInstance"/>, a door that opens in one direction only.</summary>
[SonicOrca.Core.Objects.Metadata.Name("One-way door")]
[Description("One-way door from from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(CPZDoorInstance))]
public class CPZDoorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
