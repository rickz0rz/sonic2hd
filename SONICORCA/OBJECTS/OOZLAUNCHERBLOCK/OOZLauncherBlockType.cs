using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZLAUNCHERBLOCK;

/// <summary>
/// The object type for the striped blocks of Oil Ocean Zone that break and launch a character into the launcher
/// balls (Obj3D in s2disasm). "s2hd-data import-level ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Launcher block")]
[Description("Block from Oil Ocean Zone that launches you into the launcher balls")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(OOZLauncherBlockInstance))]
public class OOZLauncherBlockType : ObjectType
{
	/// <summary>The block with vertical stripes (subtype 0).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The block with horizontal stripes (subtype not 0).</summary>
	[Dependency]
	public const string HorizontalAnimationGroupResourceKey = "/HORIZONTAL/ANIGROUP";

	[Dependency]
	public const string BreakSoundResourceKey = "SONICORCA/SOUND/BREAKABLE";

	[Dependency]
	public const string LaunchSoundResourceKey = "SONICORCA/SOUND/SPINBALL";
}
