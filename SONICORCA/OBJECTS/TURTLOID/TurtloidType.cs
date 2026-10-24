using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.TURTLOID;

/// <summary>
/// The object type for Turtloid, the turtle badnik of Sky Chase Zone, with its rider (Obj9A, Obj9B and the jet
/// Obj9C in s2disasm). "s2hd-data import-level scz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Turtloid")]
[Description("Turtloid from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(TurtloidInstance))]
public class TurtloidType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
