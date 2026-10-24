using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZCONVEYOR;

/// <summary>The object type for the invisible conveyor belt of Casino Night Zone (Obj72 in s2disasm).</summary>
[SonicOrca.Core.Objects.Metadata.Name("Conveyor belt")]
[Description("Conveyor belt from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZConveyorInstance))]
public class CNZConveyorType : ObjectType
{
}
