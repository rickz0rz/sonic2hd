using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.REXON;

/// <summary>The object type for Rexon, a badnik that lifts a long neck out of the lava and fires projectiles.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Rexon")]
[Description("Rexon from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(RexonInstance))]
public class RexonType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
