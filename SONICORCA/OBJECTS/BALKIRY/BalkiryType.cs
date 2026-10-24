using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.BALKIRY;

/// <summary>
/// The object type for Balkiry, the jet badnik of Sky Chase Zone (ObjAC, and its jet Obj9C, in s2disasm).
/// "s2hd-data import-level scz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Balkiry")]
[Description("Balkiry from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(BalkiryInstance))]
public class BalkiryType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The flame of the jet, from the art of Turtloid.</summary>
	[Dependency]
	public const string JetAnimationGroupResourceKey = "/JET/ANIGROUP";
}
