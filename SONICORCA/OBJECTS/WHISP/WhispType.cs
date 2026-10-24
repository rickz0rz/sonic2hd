using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WHISP;

/// <summary>
/// The object type for Whisp, the blowfly badnik of Aquatic Ruin Zone (Obj8C in s2disasm). "s2hd-data import-level
/// arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Whisp")]
[Description("Whisp from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(WhispInstance))]
public class WhispType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
