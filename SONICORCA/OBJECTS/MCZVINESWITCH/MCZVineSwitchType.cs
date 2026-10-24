using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZVINESWITCH;

/// <summary>
/// The object type for the vine switch of Mystic Cave Zone (Obj7F in s2disasm). "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Vine switch")]
[Description("Vine switch from Mystic Cave Zone that a character pulls when it hangs on it")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(MCZVineSwitchInstance))]
public class MCZVineSwitchType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The switch sound (SndID_Blip in the original).</summary>
	[Dependency]
	public const string SwitchSoundResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";
}
