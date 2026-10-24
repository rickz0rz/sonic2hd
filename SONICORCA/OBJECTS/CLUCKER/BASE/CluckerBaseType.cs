using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CLUCKER.BASE;

/// <summary>
/// The object type for the base of a Clucker in Wing Fortress Zone (ObjAD in s2disasm). It uses the art of the
/// Clucker. See <see cref="CluckerBaseInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Clucker base")]
[Description("Base of the Clucker badnik from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(CluckerBaseInstance))]
public class CluckerBaseType : ObjectType
{
	/// <summary>The animation group of the Clucker.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "//ANIGROUP";
}
