using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZWHEEL;

/// <summary>
/// The object type for the wheel of a conveyor belt in Wing Fortress Zone (ObjBA in s2disasm). "s2hd-data
/// import-level wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Conveyor belt wheel")]
[Description("Conveyor belt wheel from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(WFZWheelInstance))]
public class WFZWheelType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
