using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZHOOK;

/// <summary>
/// The object type for the hook on a chain of Wing Fortress Zone (Obj80 in WFZ in s2disasm). "s2hd-data import-level
/// wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Hook on a chain")]
[Description("Hook from Wing Fortress Zone that lowers a character that hangs on it")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(WFZHookInstance))]
public class WFZHookType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
