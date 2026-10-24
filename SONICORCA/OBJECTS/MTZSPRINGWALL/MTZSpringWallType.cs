using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZSPRINGWALL;

/// <summary>
/// The object type for the invisible spring walls of Metropolis Zone (Obj66 in s2disasm). The object has no art.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Spring wall")]
[Description("Invisible spring wall from Metropolis Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(MTZSpringWallInstance))]
public class MTZSpringWallType : ObjectType
{
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
