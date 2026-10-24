using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZSTOMPER;

/// <summary>
/// The object type for the stomper of Mystic Cave Zone (Obj2A in s2disasm). "s2hd-data import-level mcz" adds the
/// art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Stomper")]
[Description("Stomper from Mystic Cave Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZStomperInstance))]
public class MCZStomperType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
