using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZELEVATOR;

/// <summary>
/// The object type for the elevator of Casino Night Zone (ObjD5 in s2disasm). "s2hd-data import-level cnz" adds the art of
/// the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Elevator")]
[Description("Elevator from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(CNZElevatorInstance))]
public class CNZElevatorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
