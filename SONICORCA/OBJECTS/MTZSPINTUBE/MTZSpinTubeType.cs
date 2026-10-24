using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZSPINTUBE;

/// <summary>
/// The object type for the spin tubes of Metropolis Zone (Obj67 in s2disasm). "s2hd-data import-level mtz" adds the
/// art of the original: the flash at the entrance.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Spin tube")]
[Description("Spin tube entrance from Metropolis Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(MTZSpinTubeInstance))]
public class MTZSpinTubeType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The sound when a character enters (SndID_Roll).</summary>
	[Dependency]
	public const string EnterSoundResourceKey = "SONICORCA/SOUND/SPINBALL";

	/// <summary>The sound when the tube sends the character along the path (SndID_SpindashRelease).</summary>
	[Dependency]
	public const string LaunchSoundResourceKey = "SONICORCA/SOUND/SPINDASH/RELEASE";
}
