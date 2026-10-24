using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.DEZEGGMAN.DOOR;

/// <summary>
/// The object type for the barrier in front of Eggman in Death Egg Zone (ObjC6 in s2disasm, subtype $A8). Eggman
/// adds it. See <see cref="DEZEggmanDoorInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("DEZ Eggman barrier")]
[Description("Barrier that opens when Eggman runs in Death Egg Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(DEZEggmanDoorInstance))]
public class DEZEggmanDoorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
