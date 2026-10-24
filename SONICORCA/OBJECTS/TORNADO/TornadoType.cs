using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.TORNADO;

/// <summary>
/// The object type for the Tornado, the biplane of Tails (ObjB2 in s2disasm). "s2hd-data import-level scz" adds the
/// art of the original. The pilot is a separate sprite for each frame of the pilot animation, because the original
/// loads the pilot frames from the art of Tails into the art of the plane (Tails' DPLC).
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Tornado")]
[Description("The Tornado from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(TornadoInstance))]
public class TornadoType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Tails as the pilot: Tails frames $10, 1, 2, 3 and 4.</summary>
	[Dependency]
	public const string Pilot0AnimationGroupResourceKey = "/PILOT0/ANIGROUP";

	[Dependency]
	public const string Pilot1AnimationGroupResourceKey = "/PILOT1/ANIGROUP";

	[Dependency]
	public const string Pilot2AnimationGroupResourceKey = "/PILOT2/ANIGROUP";

	[Dependency]
	public const string Pilot3AnimationGroupResourceKey = "/PILOT3/ANIGROUP";

	[Dependency]
	public const string Pilot4AnimationGroupResourceKey = "/PILOT4/ANIGROUP";

	/// <summary>The rocket under the plane at the end of Wing Fortress Zone (ObjB2, subtype $5C).</summary>
	[Dependency]
	public const string ThrusterAnimationGroupResourceKey = "/THRUSTER/ANIGROUP";

	/// <summary>
	/// The fire at Eggman's ship at the end of Wing Fortress Zone (ObjB2, subtype $58). It is not a dependency: only
	/// a mod of Wing Fortress Zone has it.
	/// </summary>
	public const string FireResourceKey = "SONICORCA/OBJECTS/WFZTHRUST";
}
