using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZMAPBUMPER;

/// <summary>
/// The object type for a bumper that is part of the Casino Night Zone landscape (SpecialCNZBumpers in s2disasm). The
/// tiles show the bumper. This invisible object makes it bounce.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Landscape bumper")]
[Description("Bumper in the landscape of Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZMapBumperInstance))]
public class CNZMapBumperType : ObjectType
{
	/// <summary>The game data has no bumper sound. The spring sound is the nearest.</summary>
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
