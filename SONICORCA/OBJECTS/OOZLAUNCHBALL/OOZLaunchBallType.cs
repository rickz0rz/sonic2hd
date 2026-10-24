using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZLAUNCHBALL;

/// <summary>
/// The object type for the launcher balls of Oil Ocean Zone (Obj48 in s2disasm). "s2hd-data import-level ooz" adds
/// the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Launcher ball")]
[Description("Round ball from Oil Ocean Zone that fires you off in a different direction")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(OOZLaunchBallInstance))]
public class OOZLaunchBallType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPINBALL";
}
