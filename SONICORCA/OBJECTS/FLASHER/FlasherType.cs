using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.FLASHER;

/// <summary>
/// The object type for Flasher, the firefly badnik of Mystic Cave Zone (ObjA3 in s2disasm). "s2hd-data import-level
/// mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Flasher")]
[Description("Flasher from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(FlasherInstance))]
public class FlasherType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
