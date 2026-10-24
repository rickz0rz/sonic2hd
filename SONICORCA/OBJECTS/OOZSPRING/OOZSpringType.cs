using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZSPRING;

/// <summary>
/// The object type for the pressure spring of Oil Ocean Zone (Obj45 in s2disasm). "s2hd-data import-level ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Pressure spring")]
[Description("Pressure spring from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(OOZSpringInstance))]
public class OOZSpringType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
