using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZBRIDGE;

/// <summary>
/// The object type for the log bridge of Mystic Cave Zone that opens with a switch (Obj77 in s2disasm). "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Switch bridge")]
[Description("Log bridge from Mystic Cave Zone that opens when a switch is pulled")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZBridgeInstance))]
public class MCZBridgeType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
