using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZBURNER;

/// <summary>
/// The object type for the burner platforms of Oil Ocean Zone (Obj33 in s2disasm). "s2hd-data import-level ooz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Burner platform")]
[Description("Green platform with a flame from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(OOZBurnerInstance))]
public class OOZBurnerType : ObjectType
{
	/// <summary>The lid.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The green flame under the lid.</summary>
	[Dependency]
	public const string FlameAnimationGroupResourceKey = "/FLAME/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
