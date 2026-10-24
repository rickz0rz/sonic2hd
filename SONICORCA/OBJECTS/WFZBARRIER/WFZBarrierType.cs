using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZBARRIER;

/// <summary>
/// The object type for the one-way barrier of Wing Fortress Zone (Obj2D in s2disasm). "s2hd-data import-level wfz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("One-way barrier")]
[Description("One-way barrier from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(WFZBarrierInstance))]
public class WFZBarrierType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
