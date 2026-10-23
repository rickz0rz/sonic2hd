using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.LOCKY;

/// <summary>The object type for Locky, an animal that a destroyed badnik sets free.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Locky")]
[Description("Locky from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Animal)]
[ObjectInstance(typeof(LockyInstance))]
public class LockyType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
