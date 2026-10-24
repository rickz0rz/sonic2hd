using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.ARZARROWSHOOTER;

/// <summary>
/// The object type for the arrow shooter of Aquatic Ruin Zone (Obj22 in s2disasm). "s2hd-data import-level arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Arrow shooter")]
[Description("Arrow shooter from Aquatic Ruin Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(ARZArrowShooterInstance))]
public class ARZArrowShooterType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
