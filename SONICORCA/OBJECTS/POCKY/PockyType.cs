using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.POCKY;

/// <summary>The object type for Pocky, an animal that a destroyed badnik sets free.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Pocky")]
[Description("Pocky from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Animal)]
[ObjectInstance(typeof(PockyInstance))]
public class PockyType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
