using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZCYLINDER;

/// <summary>
/// The object type for the rotating cylinder of Metropolis Zone (Obj06 with subtype bit 7 in s2disasm). The cylinder
/// is level art. The object has no art.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Rotating cylinder")]
[Description("Rotating mesh cylinder from Metropolis Zone that a character runs around")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZCylinderInstance))]
public class MTZCylinderType : ObjectType
{
}
