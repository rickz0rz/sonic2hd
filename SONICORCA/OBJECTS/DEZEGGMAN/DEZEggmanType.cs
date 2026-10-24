using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.DEZEGGMAN;

/// <summary>
/// The object type for Eggman in Death Egg Zone, who runs away to the Death Egg Robot (ObjC6 in s2disasm, subtype
/// $A6). "s2hd-data import-level dez" adds the art of the original. See <see cref="DEZEggmanInstance"/> for the
/// behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("DEZ Eggman")]
[Description("Eggman who runs to the Death Egg Robot, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(DEZEggmanInstance))]
public class DEZEggmanType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The barrier that opens when Eggman runs.</summary>
	[Dependency]
	public const string DoorResourceKey = "/DOOR";
}
