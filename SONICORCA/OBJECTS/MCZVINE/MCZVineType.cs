using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZVINE;

/// <summary>
/// The object type for the vine pulley of Mystic Cave Zone (Obj80 in s2disasm). "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Vine pulley")]
[Description("Vine from Mystic Cave Zone that lowers a character that hangs on it")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(MCZVineInstance))]
public class MCZVineType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The switch sound (SndID_Blip in the original).</summary>
	[Dependency]
	public const string SwitchSoundResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";
}
