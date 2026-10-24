using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CLUCKER.SHOT;

/// <summary>
/// The object type for the shot of a Clucker (Obj98 with Obj98_CluckerShotMove in s2disasm). It uses the art of the
/// Clucker. See <see cref="CluckerShotInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Clucker shot")]
[Description("Shot of the Clucker badnik from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(CluckerShotInstance))]
public class CluckerShotType : ObjectType
{
	/// <summary>The animation group of the Clucker.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "//ANIGROUP";
}
