using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SPINY;

/// <summary>The object type for Spiny, a badnik that crawls on a floor or wall and fires plasma spheres.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Spiny")]
[Description("Spiny from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(SpinyInstance))]
public class SpinyType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
