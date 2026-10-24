using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.NEBULA;

/// <summary>
/// The object type for Nebula, the bomber badnik of Sky Chase Zone (Obj99 in s2disasm). "s2hd-data import-level scz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Nebula")]
[Description("Nebula from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(NebulaInstance))]
public class NebulaType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
