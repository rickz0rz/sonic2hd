using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZSTEAMSPRING;

/// <summary>
/// The object type for the steam piston of Metropolis Zone (Obj42 in s2disasm). "s2hd-data import-level mtz" adds
/// the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Steam spring")]
[Description("Steam piston from Metropolis Zone that sends characters up")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(MTZSteamSpringInstance))]
public class MTZSteamSpringType : ObjectType
{
	/// <summary>Obj42_MapUnc_2686C with the level art. Frame 7 is the piston.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Obj42_MapUnc_2686C with the steam art. Frames 0 to 6 are the puff of steam.</summary>
	[Dependency]
	public const string SteamAnimationGroupResourceKey = "/STEAM/ANIGROUP";

	/// <summary>The spring sound (SndID_Spring in the original).</summary>
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
