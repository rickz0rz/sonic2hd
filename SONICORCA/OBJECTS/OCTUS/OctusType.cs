using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OCTUS;

/// <summary>
/// The object type for Octus, the octopus badnik of Oil Ocean Zone (Obj4A in s2disasm). "s2hd-data import-level
/// ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Octus")]
[Description("Octus from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(OctusInstance))]
public class OctusType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
