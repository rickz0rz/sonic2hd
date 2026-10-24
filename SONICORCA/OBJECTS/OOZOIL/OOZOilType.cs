using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZOIL;

/// <summary>
/// The object type for the oil surface of Oil Ocean Zone (Obj07 in s2disasm). The oil is level art, so the object has
/// no art. The area of the zone adds one instance at the start of the level.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Oil")]
[Description("Oil surface from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(OOZOilInstance))]
public class OOZOilType : ObjectType
{
}
