using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZPLATING;

/// <summary>
/// The object type for the breakable plating of Wing Fortress Zone (ObjC1 in s2disasm). "s2hd-data import-level wfz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Breakable plating")]
[Description("Plating from Wing Fortress Zone that a character hangs on until it breaks")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(WFZPlatingInstance))]
public class WFZPlatingType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The sound of the plating that breaks (SndID_SlowSmash in the original).</summary>
	[Dependency]
	public const string BreakSoundResourceKey = "SONICORCA/SOUND/BREAKABLE";
}
