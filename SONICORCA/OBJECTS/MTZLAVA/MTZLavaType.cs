using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZLAVA;

/// <summary>
/// The object type for the lava collision marker (Obj31 in s2disasm). The lava is level art, so the object has no art.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Lava marker")]
[Description("Invisible area that hurts characters, over the lava of Metropolis Zone and Hill Top Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(MTZLavaInstance))]
public class MTZLavaType : ObjectType
{
}
