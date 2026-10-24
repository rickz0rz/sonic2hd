using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZFLIPPER;

/// <summary>
/// The object type for the flippers of Casino Night Zone (Obj86 in s2disasm). "s2hd-data import-level cnz" adds the
/// art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Flipper")]
[Description("Flipper from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZFlipperInstance))]
public class CNZFlipperType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The game data has no flipper sound. The spring sound is the nearest.</summary>
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
