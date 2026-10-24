using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZDRAWBRIDGE;

/// <summary>
/// The object type for the drawbridge of Mystic Cave Zone (Obj81 in s2disasm). "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Drawbridge")]
[Description("Drawbridge from Mystic Cave Zone that a switch lowers")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZDrawbridgeInstance))]
public class MCZDrawbridgeType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
