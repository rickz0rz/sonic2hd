using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.AQUIS;

/// <summary>
/// The object type for Aquis, the seahorse badnik of Oil Ocean Zone (Obj50 in s2disasm). "s2hd-data import-level
/// ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Aquis")]
[Description("Aquis from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(AquisInstance))]
public class AquisType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
