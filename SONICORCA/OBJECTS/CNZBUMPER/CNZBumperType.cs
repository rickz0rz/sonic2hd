using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZBUMPER;

/// <summary>
/// The object type for the round bumper of Casino Night Zone (Obj44 in s2disasm). The game data has no art for it.
/// "s2hd-data import-level cnz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Round bumper")]
[Description("Round bumper from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZBumperInstance))]
public class CNZBumperType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The game data has no bumper sound. The spring sound is the nearest.</summary>
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
