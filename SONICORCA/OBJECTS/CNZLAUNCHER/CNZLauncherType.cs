using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZLAUNCHER;

/// <summary>
/// The object type for the spring launchers of Casino Night Zone (Obj85 in s2disasm). "s2hd-data import-level cnz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Spring launcher")]
[Description("Spring from Casino Night Zone that you hold jump on to pull back further")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZLauncherInstance))]
public class CNZLauncherType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string DiagonalAnimationGroupResourceKey = "/DIAGONAL/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
