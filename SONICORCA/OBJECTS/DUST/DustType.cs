using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.DUST;

/// <summary>The object type for <see cref="DustInstance"/>, a small cloud of dust.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Dust")]
[Description("Dust from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Particle)]
[ObjectInstance(typeof(DustInstance))]
public class DustType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
