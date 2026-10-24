using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.INVISIBLEBLOCK;

/// <summary>The object type for an invisible solid block (Obj74 in s2disasm).</summary>
[SonicOrca.Core.Objects.Metadata.Name("Invisible block")]
[Description("Invisible solid block from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(InvisibleBlockInstance))]
public class InvisibleBlockType : ObjectType
{
}
