using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZBUTTON;

/// <summary>
/// The object type for the button (Obj47 in s2disasm). "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Button")]
[Description("Button that a character presses when it stands on it, from Metropolis Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(MTZButtonInstance))]
public class MTZButtonType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The button sound (SndID_Blip in the original).</summary>
	[Dependency]
	public const string SwitchSoundResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";
}
