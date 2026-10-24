using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.GROUNDER;

/// <summary>
/// The object type for Grounder, the drill badnik of Aquatic Ruin Zone (Obj8D and Obj8E in s2disasm). "s2hd-data
/// import-level arz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Grounder")]
[Description("Grounder from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(GrounderInstance))]
public class GrounderType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The bricks of the wall that a hidden Grounder breaks (level art).</summary>
	[Dependency]
	public const string BrickAnimationGroupResourceKey = "/BRICK/ANIGROUP";

	/// <summary>The rocks that fly out of the wall.</summary>
	[Dependency]
	public const string RockAnimationGroupResourceKey = "/ROCK/ANIGROUP";
}
