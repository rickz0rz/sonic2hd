using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZLASER;

/// <summary>
/// The object type for the large horizontal laser of Wing Fortress Zone (ObjB9 in s2disasm). "s2hd-data import-level
/// wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Horizontal laser")]
[Description("Large horizontal laser from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(WFZLaserInstance))]
public class WFZLaserType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
