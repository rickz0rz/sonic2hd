using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZLAUNCHER;

/// <summary>
/// The object type for the speed launcher (the catapult) of Wing Fortress Zone (ObjC0 in s2disasm).
/// "s2hd-data import-level wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Speed launcher")]
[Description("Catapult from Wing Fortress Zone that throws a character to the side")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(WFZLauncherInstance))]
public class WFZLauncherType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
