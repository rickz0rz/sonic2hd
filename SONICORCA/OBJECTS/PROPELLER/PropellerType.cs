using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.PROPELLER;

/// <summary>
/// The object type for the propellers of Sky Chase Zone and Wing Fortress Zone: the vertical propeller (ObjB4 in
/// s2disasm) and the horizontal propeller (ObjB5). "s2hd-data import-level scz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Propeller")]
[Description("Propeller from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(PropellerInstance))]
public class PropellerType : ObjectType
{
	/// <summary>The vertical propeller.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string HorizontalAnimationGroupResourceKey = "/HORIZONTAL/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BOSSHELICOPTER";
}
