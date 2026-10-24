using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZHEXBUMPER;

/// <summary>
/// The object type for the hexagonal bumper of Casino Night Zone (ObjD7 in s2disasm). "s2hd-data import-level cnz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Hexagonal bumper")]
[Description("Hexagonal bumper from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZHexBumperInstance))]
public class CNZHexBumperType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The game data has no bumper sound. The spring sound is the nearest.</summary>
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
